#!/usr/bin/env python3
"""
check_packages.py — report NuGet / npm / pnpm package compliance against
AGENTS.md → Dependencies. Read-only: it never edits project files
(.NET only runs `dotnet restore`, which writes nothing but obj/).

Usage:  python3 -I check_packages.py <project-root>
Exit:   0 no violations · 1 violations found · 2 could not check
"""
from __future__ import annotations

import argparse
import concurrent.futures as cf
import datetime as dt
import gzip
import json
import os
import re
import subprocess
import sys
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

# Thresholds mirror AGENTS.md → Dependencies; change them together.
MIN_RELEASE_AGE = dt.timedelta(days=7)
MAINTAINED_WITHIN = dt.timedelta(days=365)
PNPM_MIN_RELEASE_AGE_MINUTES = 7 * 24 * 60
NOW = dt.datetime.now(dt.timezone.utc)

# Released in lockstep with .NET: the major must equal the target framework's major.
# Same list as 0-dotnet-project-setup/scripts/scaffold.sh framework_versioned().
FRAMEWORK_VERSIONED = re.compile(
    r"^(Microsoft\.Extensions\..+|Microsoft\.AspNetCore\..+|Microsoft\.EntityFrameworkCore(\..+)?"
    r"|Npgsql\.EntityFrameworkCore\.PostgreSQL|dotnet-ef"
    r"|Serilog\.(AspNetCore|Extensions\.Hosting|Extensions\.Logging|Settings\.Configuration))$",
    re.I,
)

# ---------------------------------------------------------------------------
# License classification (SPDX). ok < weak < ask < banned
# ---------------------------------------------------------------------------
PERMISSIVE = {s.upper() for s in (
    "MIT", "MIT-0", "Apache-2.0", "BSD-2-Clause", "BSD-3-Clause", "0BSD", "ISC", "Zlib",
    "Unlicense", "CC0-1.0", "BSL-1.0", "PostgreSQL", "Python-2.0", "BlueOak-1.0.0",
    "CC-BY-3.0", "CC-BY-4.0", "X11",
)}
WEAK_COPYLEFT = ("LGPL-2.0", "LGPL-2.1", "LGPL-3.0", "MPL-2.0")
BANNED = ("GPL-", "AGPL-", "SSPL-", "EUPL-", "OSL-", "CPAL-", "RPL-", "CC-BY-NC", "CC-BY-SA")
RANK = {"ok": 0, "weak": 1, "ask": 2, "banned": 3}


def classify_id(term: str) -> str:
    t = term.strip().upper()
    if " WITH " in t:  # license exception: fine only on an already-permissive base
        return "ok" if t.split(" WITH ")[0].strip() in PERMISSIVE else "ask"
    t = t.rstrip("+")
    if t in PERMISSIVE:
        return "ok"
    if t.startswith(WEAK_COPYLEFT):
        return "weak"
    if t.startswith(BANNED):
        return "banned"
    return "ask"


def classify_license(expr: str | None) -> str:
    """SPDX expression → status. OR picks the best branch, AND the worst term."""
    if not expr or not expr.strip():
        return "ask"
    up = expr.strip().upper()
    if up.startswith("SEE LICENSE") or up in ("UNLICENSED", "NONE", "NOASSERTION"):
        return "ask"
    tokens = re.findall(r"\(|\)|[^\s()]+(?:\s+WITH\s+[^\s()]+)?", expr, flags=re.I)
    pos = 0

    def parse_or() -> str:
        nonlocal pos
        best = parse_and()
        while pos < len(tokens) and tokens[pos].upper() == "OR":
            pos += 1
            best = min(best, parse_and(), key=RANK.get)
        return best

    def parse_and() -> str:
        nonlocal pos
        worst = parse_atom()
        while pos < len(tokens) and tokens[pos].upper() == "AND":
            pos += 1
            worst = max(worst, parse_atom(), key=RANK.get)
        return worst

    def parse_atom() -> str:
        nonlocal pos
        if pos >= len(tokens):
            return "ask"
        tok = tokens[pos]
        pos += 1
        if tok == "(":
            r = parse_or()
            pos += 1  # ")"
            return r
        return classify_id(tok)

    try:
        result = parse_or()
        return result if pos >= len(tokens) else "ask"
    except Exception:
        return "ask"


# ---------------------------------------------------------------------------
# HTTP + version helpers
# ---------------------------------------------------------------------------
def get_json(url: str):
    req = urllib.request.Request(url, headers={
        "Accept-Encoding": "gzip", "Accept": "application/json", "User-Agent": "package-compliance-check"})
    for attempt in range(3):
        try:
            with urllib.request.urlopen(req, timeout=60) as r:
                data = r.read()
            if data[:2] == b"\x1f\x8b":
                data = gzip.decompress(data)
            return json.loads(data)
        except urllib.error.HTTPError as e:
            if e.code == 404:
                return None
            if attempt == 2:
                raise
        except (urllib.error.URLError, TimeoutError):
            if attempt == 2:
                raise
    return None


def parse_time(s: str | None) -> dt.datetime | None:
    if not s:
        return None
    try:
        t = dt.datetime.fromisoformat(s.replace("Z", "+00:00"))
        return t if t.tzinfo else t.replace(tzinfo=dt.timezone.utc)
    except ValueError:
        return None


def norm(v: str) -> str:
    return v.split("+")[0].strip().lower()


def stable(v: str) -> bool:
    return "-" not in norm(v)


def vtuple(v: str) -> tuple[int, ...]:
    core = norm(v).split("-")[0]
    parts = [int(p) if p.isdigit() else 0 for p in core.split(".")]
    return tuple((parts + [0, 0, 0, 0])[:4])


def major(v: str) -> int:
    return vtuple(v)[0]


def day(t: dt.datetime | None) -> str:
    return t.date().isoformat() if t else "?"


def age_days(t: dt.datetime | None) -> int | None:
    return (NOW - t).days if t else None


# ---------------------------------------------------------------------------
# Registry metadata
# ---------------------------------------------------------------------------
def license_from_url(url: str) -> tuple[str | None, bool]:
    """Legacy NuGet licenseUrl → (SPDX id, guessed). licenses.nuget.org is exact; others are inferred."""
    m = re.match(r"https?://licenses\.nuget\.org/(.+)$", url)
    if m:
        return urllib.parse.unquote(m.group(1)), False
    u = url.lower()
    for pat, spdx in ((r"gnu\.org/licenses/agpl", "AGPL-3.0"), (r"gnu\.org/licenses/lgpl", "LGPL-3.0"),
                      (r"gnu\.org/licenses/(old-licenses/)?gpl", "GPL-3.0"),
                      (r"apache\.org/licenses/license-2\.0", "Apache-2.0")):
        if re.search(pat, u):
            return spdx, True
    m = re.search(r"opensource\.org/licenses?/([a-z0-9.\-]+?)(\.php|\.html)?/?$", u)
    if m:
        return m.group(1), True
    return None, False


def nuget_meta(pkg: str) -> dict | None:
    index = get_json(f"https://api.nuget.org/v3/registration5-gz-semver2/{pkg.lower()}/index.json")
    if index is None:
        return None
    versions = {}
    for page in index.get("items", []):
        leaves = page.get("items")
        if leaves is None:
            leaves = (get_json(page["@id"]) or {}).get("items", [])
        for leaf in leaves:
            ce = leaf.get("catalogEntry", {})
            published = parse_time(ce.get("published"))
            listed = ce.get("listed", True) and bool(published and published.year > 1900)
            expr, url = ce.get("licenseExpression") or None, ce.get("licenseUrl") or None
            guessed = False
            if not expr and url:
                expr, guessed = license_from_url(url)
            dep = ce.get("deprecation")
            versions[norm(ce.get("version", ""))] = {
                "version": ce.get("version", ""),
                "published": published,
                "listed": listed,
                "license": expr,
                "license_url": url if (guessed or not expr) else None,
                "deprecated": None if not dep else ", ".join(dep.get("reasons", [])) + (
                    f" (alternative: {dep['alternatePackage'].get('id')})" if dep.get("alternatePackage") else ""),
                "tfms": [g.get("targetFramework", "") for g in ce.get("dependencyGroups", [])],
            }
    return {"versions": versions}


def npm_meta(pkg: str) -> dict | None:
    doc = get_json("https://registry.npmjs.org/" + pkg.replace("/", "%2F"))
    if doc is None or "versions" not in doc:
        return None
    times = doc.get("time", {})
    versions = {}
    for v, man in doc["versions"].items():
        lic = man.get("license")
        if isinstance(lic, dict):
            lic = lic.get("type")
        if not lic and isinstance(man.get("licenses"), list):
            lic = " OR ".join(x.get("type", "") for x in man["licenses"] if isinstance(x, dict))
        versions[norm(v)] = {
            "version": v,
            "published": parse_time(times.get(v)),
            "listed": True,
            "license": lic if isinstance(lic, str) else None,
            "license_url": None,
            "deprecated": man.get("deprecated") or None,
            "peer": man.get("peerDependencies") or {},
        }
    return {"versions": versions}


def tfm_ok(tfms: list[str], tfm_major: int) -> bool:
    if not tfms:
        return True
    for t in tfms:
        tl = t.lower()
        if tl == "" or tl.startswith((".netstandard", "netstandard", "any")):
            return True
        m = re.match(r"^net(\d+)\.\d", tl) or re.match(r"^\.netcoreapp(\d+)", tl)
        if m and (int(m.group(1)) <= tfm_major or tl.startswith(".netcoreapp")):
            return True
    return False


def candidate(meta: dict, eco: str, name: str, tfm_major: int | None) -> dict | None:
    """Highest version that satisfies every version rule."""
    best = None
    for e in meta["versions"].values():
        if not e["listed"] or not stable(e["version"]) or e["deprecated"]:
            continue
        if not e["published"] or NOW - e["published"] < MIN_RELEASE_AGE:
            continue
        if eco == "nuget" and tfm_major is not None:
            if FRAMEWORK_VERSIONED.match(name) and major(e["version"]) != tfm_major:
                continue
            if not tfm_ok(e["tfms"], tfm_major):
                continue
        if best is None or vtuple(e["version"]) > vtuple(best["version"]):
            best = e
    return best


def last_release(meta: dict) -> dt.datetime | None:
    times = [e["published"] for e in meta["versions"].values() if e["listed"] and e["published"]]
    return max(times) if times else None


# ---------------------------------------------------------------------------
# Collectors → items: {eco, name, version, direct, dev, where:set, tfm_major}
# ---------------------------------------------------------------------------
SKIP_DIRS = {"node_modules", "bin", "obj", ".git", "dist", "build", ".venv", "wwwroot", ".nuxt", ".next"}


def walk(root: Path, max_depth: int = 4):
    for dirpath, dirnames, filenames in os.walk(root):
        depth = len(Path(dirpath).relative_to(root).parts)
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS and not d.startswith(".")] if depth < max_depth else []
        for f in filenames:
            yield Path(dirpath) / f


def add_item(items: dict, eco, name, version, direct, dev, where, tfm_major=None):
    key = (eco, name.lower(), norm(version))
    it = items.setdefault(key, {"eco": eco, "name": name, "version": version, "direct": False,
                                "dev": True, "where": set(), "tfm_major": tfm_major})
    it["direct"] |= direct
    it["dev"] &= dev
    it["where"].add(where)
    if tfm_major is not None:
        it["tfm_major"] = tfm_major if it["tfm_major"] is None else min(it["tfm_major"], tfm_major)


def collect_dotnet(root: Path, items: dict, errors: list, tfms: set):
    files = list(walk(root))
    targets = [f for f in files if f.suffix in (".slnx", ".sln")] or [f for f in files if f.suffix == ".csproj"]
    for t in targets:
        rel = str(t.relative_to(root))
        r = subprocess.run(["dotnet", "restore", str(t)], capture_output=True, text=True)
        if r.returncode != 0:
            errors.append(f"`dotnet restore {rel}` failed: {(r.stdout + r.stderr).strip()[-400:]}")
            continue
        r = subprocess.run(["dotnet", "list", str(t), "package", "--include-transitive", "--format", "json"],
                           capture_output=True, text=True)
        try:
            data = json.loads(r.stdout)
        except json.JSONDecodeError:
            errors.append(f"`dotnet list {rel} package` output could not be parsed: {(r.stdout + r.stderr).strip()[-400:]}")
            continue
        for proj in data.get("projects", []):
            pname = Path(proj.get("path", rel)).stem
            for fw in proj.get("frameworks", []):
                m = re.match(r"^net(\d+)\.", fw.get("framework", ""))
                tmaj = int(m.group(1)) if m else None
                if tmaj:
                    tfms.add(tmaj)
                for p in fw.get("topLevelPackages", []):
                    if not p.get("autoReferenced"):
                        add_item(items, "nuget", p["id"], p["resolvedVersion"], True, False, pname, tmaj)
                for p in fw.get("transitivePackages", []):
                    add_item(items, "nuget", p["id"], p["resolvedVersion"], False, False, pname, tmaj)


def split_key(key: str):
    key = key.strip().strip("'\"").lstrip("/")
    key = re.sub(r"\(.*$", "", key)
    i = key.rfind("@")
    return (key[:i], key[i + 1:]) if i > 0 else (None, None)


def collect_pnpm(lock: Path, root: Path, items: dict, errors: list):
    where = str(lock.parent.relative_to(root)) or "."
    text = lock.read_text(encoding="utf-8")
    m = re.search(r"^lockfileVersion:\s*'?([\d.]+)", text, re.M)
    if not m or float(m.group(1)) < 6:
        errors.append(f"{lock.relative_to(root)}: unsupported lockfileVersion {m.group(1) if m else '?'} (needs 6+)")
        return
    section, importer, deptype, pkg = None, None, None, None
    direct: dict[str, tuple[str, bool]] = {}
    for line in text.splitlines():
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        ind = len(line) - len(line.lstrip(" "))
        s = line.strip()
        if ind == 0:
            section = s.rstrip(":")
            importer = "." if section in ("dependencies", "devDependencies", "optionalDependencies") else None
            deptype = section if importer else None
            continue
        off = 0 if section == "importers" else -4  # v6 single-project lock has no importer level
        if section == "importers" and ind == 2:
            importer = s.rstrip(":").strip("'\"")
        elif importer and ind == 4 + off and section == "importers":
            deptype = s.rstrip(":")
        elif importer and deptype and ind == 6 + off:
            pkg = s.rstrip(":").strip("'\"")
        elif importer and deptype and pkg and ind == 8 + off and s.startswith("version:"):
            ver = re.sub(r"\(.*$", "", s.split(":", 1)[1].strip().strip("'\""))
            if not ver.startswith(("link:", "file:", "workspace:")):
                direct[f"{pkg}@{norm(ver)}"] = (pkg, deptype == "devDependencies")
                add_item(items, "npm", pkg, ver, True, deptype == "devDependencies", where)
        elif section == "packages" and ind == 2 and s.endswith(":"):
            name, ver = split_key(s[:-1])
            if not name:
                continue
            if not re.match(r"^\d+\.\d+\.\d+", ver):
                errors.append(f"{where}: {name}@{ver} is not from a registry; review manually")
                continue
            if f"{name}@{norm(ver)}" not in direct:
                add_item(items, "npm", name, ver, False, False, where)
    # pnpm supply-chain gate
    ws = lock.parent / "pnpm-workspace.yaml"
    mra = re.search(r"^minimumReleaseAge:\s*(\d+)", ws.read_text(encoding="utf-8"), re.M) if ws.exists() else None
    if not mra or int(mra.group(1)) < PNPM_MIN_RELEASE_AGE_MINUTES:
        PNPM_GATES.append((where, mra.group(1) if mra else None))


def collect_npm(lock: Path, root: Path, items: dict, errors: list):
    where = str(lock.parent.relative_to(root)) or "."
    data = json.loads(lock.read_text(encoding="utf-8"))
    pkgs = data.get("packages")
    if not pkgs:
        errors.append(f"{lock.relative_to(root)}: unsupported lockfileVersion {data.get('lockfileVersion')} (needs 2+)")
        return
    rootpkg = pkgs.get("", {})
    devs = set(rootpkg.get("devDependencies", {}))
    names = set(rootpkg.get("dependencies", {})) | devs | set(rootpkg.get("optionalDependencies", {}))
    for path, info in pkgs.items():
        if not path or info.get("link") or "version" not in info:
            continue
        name = info.get("name") or path.rsplit("node_modules/", 1)[-1]
        is_direct = path == f"node_modules/{name}" and name in names
        add_item(items, "npm", name, info["version"], is_direct, bool(info.get("dev")), where)


PNPM_GATES: list[tuple[str, str | None]] = []


# ---------------------------------------------------------------------------
# Framework / runtime LTS
# ---------------------------------------------------------------------------
def framework_findings(root: Path, tfms: set, has_js: bool, findings: list):
    rows = []
    if tfms:
        idx = get_json("https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json") or {}
        rels = {int(r["channel-version"].split(".")[0]): r for r in idx.get("releases-index", [])
                if r.get("product") == ".NET"}
        lts = [m for m, r in rels.items() if r.get("release-type") == "lts" and r.get("support-phase") == "active"]
        latest = max(lts) if lts else None
        for t in sorted(tfms):
            r = rels.get(t, {})
            if r.get("support-phase") == "eol":
                res = "❌ EOL"
                findings.append(("❌", "framework", f".NET {t}", "", "End of life: no more security updates"))
            elif r.get("release-type") == "sts":
                res = "⚠️ not LTS"
                findings.append(("⚠️", "framework", f".NET {t}", "", f"STS release; latest LTS is .NET {latest}"))
            elif latest and t < latest:
                res = "ℹ️ not latest LTS"
                findings.append(("ℹ️", "framework", f".NET {t}", "", f"Latest LTS is .NET {latest} (existing projects are not upgraded unasked)"))
            else:
                res = "✅"
            rows.append((f".NET (net{t}.0)", str(t), str(latest or "?"), res))
    if has_js:
        dist = get_json("https://nodejs.org/dist/index.json") or []
        lts_majors = [int(d["version"].lstrip("v").split(".")[0]) for d in dist if d.get("lts")]
        latest = max(lts_majors) if lts_majors else None
        spec = None
        for f in (".nvmrc", ".node-version"):
            p = next((x for x in walk(root, 2) if x.name == f), None)
            if p:
                spec = p.read_text(encoding="utf-8").strip()
                break
        if spec is None:
            pj = next((x for x in walk(root, 2) if x.name == "package.json"), None)
            if pj:
                spec = (json.loads(pj.read_text(encoding="utf-8")).get("engines") or {}).get("node")
        if not spec:
            res = "ℹ️ unspecified"
            findings.append(("ℹ️", "framework", "Node.js", "", f"No version set in .nvmrc / .node-version / engines.node; latest LTS is {latest}"))
        elif spec.startswith("lts/"):
            res = "✅"
        else:
            mm = re.search(r"\d+", spec)
            cur = int(mm.group(0)) if mm else None
            if cur and latest and cur < latest:
                res = "ℹ️ not latest LTS"
                findings.append(("ℹ️", "framework", "Node.js", spec, f"Latest LTS is {latest} (existing projects are not upgraded unasked)"))
            elif cur and cur % 2 == 1:
                res = "⚠️ not LTS"
                findings.append(("⚠️", "framework", "Node.js", spec, f"Odd-numbered releases are not LTS; latest LTS is {latest}"))
            else:
                res = "✅"
        rows.append(("Node.js", spec or "—", str(latest or "?"), res))
    return rows


# ---------------------------------------------------------------------------
# Rules
# ---------------------------------------------------------------------------
LICENSE_TEXT = {"weak": "Weak copyleft: confirm dynamically referenced, unmodified, not merged into a single file",
                "ask": "License needs manual review (custom / dual / revenue threshold / missing)",
                "banned": "Copyleft license: prohibited"}


def license_label(e: dict | None) -> str:
    if not e:
        return "?"
    if e["license"] and e["license_url"]:
        return f"{e['license']} (inferred from license URL: {e['license_url']})"
    return e["license"] or (f"license file: {e['license_url']}" if e["license_url"] else "missing")


def license_key(e: dict) -> str:
    """Comparable license identity; per-version license-file URLs differ only by the version."""
    return e["license"] or re.sub(re.escape(e["version"]), "{v}", e["license_url"] or "", flags=re.I)


def check(item: dict, meta: dict | None, findings: list) -> dict:
    eco, name, ver = item["eco"], item["name"], item["version"]
    kind = "direct" if item["direct"] else "transitive"
    row = {"name": name, "eco": eco, "version": ver, "cand": "?", "license": "?", "last": "?", "flags": [],
           "dev": item["dev"]}

    def flag(level, msg):
        findings.append((level, f"{eco}·{kind}", name, ver, msg))
        row["flags"].append(level)

    if meta is None:
        flag("⚠️", "Package not found in the registry (private feed?); review manually")
        return row
    e = meta["versions"].get(norm(ver))
    row["license"] = license_label(e)
    lstatus = classify_license(e["license"]) if e else "ask"
    if e and lstatus != "ok":
        flag("❌" if lstatus == "banned" else "⚠️", f"{LICENSE_TEXT[lstatus]}: {row['license']}")
    if e is None:
        flag("⚠️", "Version not found in the registry; review manually")
    else:
        a = age_days(e["published"])
        if e["published"] and NOW - e["published"] < MIN_RELEASE_AGE:
            flag("❌", f"Published less than 7 days ago ({a} days, {day(e['published'])})")
        if e["deprecated"]:
            flag("❌", f"Deprecated: {str(e['deprecated'])[:160]}")
    if not item["direct"]:
        return row

    # Direct packages: every rule.
    if not stable(ver):
        flag("❌", "Prerelease version (preview/beta/RC)")
    tmaj = item["tfm_major"]
    if eco == "nuget" and tmaj and FRAMEWORK_VERSIONED.match(name) and major(ver) != tmaj:
        flag("❌", f"Major does not match the main framework: target is .NET {tmaj}, use {tmaj}.x")
    last = last_release(meta)
    row["last"] = day(last)
    if last and NOW - last > MAINTAINED_WITHIN:
        flag("❌", f"Not actively maintained: last release {day(last)}")
    c = candidate(meta, eco, name, tmaj)
    if c is None or vtuple(c["version"]) < vtuple(ver):
        row["cand"] = "—"
        if not (e and e["deprecated"]):  # a deprecated package already says why
            flag("⚠️", "No version satisfies the rules")
        return row
    row["cand"] = c["version"]
    if vtuple(c["version"]) > vtuple(ver):
        note = f"Upgrade available: {c['version']} (existing packages are not upgraded unasked)"
        if e and license_key(c) != license_key(e):
            note += f"; ⚠️ license changes: {row['license']} → {license_label(c)} ({LICENSE_TEXT.get(classify_license(c['license']), 'commercially usable')})"
        if eco == "npm" and major(c["version"]) != major(ver) and c.get("peer"):
            note += "; peerDependencies of the new major: " + ", ".join(f"{k} {v}" for k, v in c["peer"].items())
        flag("ℹ️", note)
    return row


# ---------------------------------------------------------------------------
# Report
# ---------------------------------------------------------------------------
def cell(s) -> str:
    return str(s).replace("|", "\\|").replace("\n", " ")


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("root", nargs="?", default=".")
    root = Path(ap.parse_args().root).resolve()
    items: dict = {}
    errors: list[str] = []
    tfms: set[int] = set()
    files = list(walk(root))

    if any(f.suffix in (".csproj", ".sln", ".slnx") for f in files):
        collect_dotnet(root, items, errors, tfms)
    js_locks = [f for f in files if f.name in ("pnpm-lock.yaml", "package-lock.json")]
    for lock in js_locks:
        (collect_pnpm if lock.name == "pnpm-lock.yaml" else collect_npm)(lock, root, items, errors)
    for pj in (f for f in files if f.name == "package.json"):
        if not any((pj.parent / n).exists() for n in ("pnpm-lock.yaml", "package-lock.json")) and \
                not any(l.parent in pj.parents for l in js_locks):
            errors.append(f"{pj.parent.relative_to(root) or '.'}: package.json without a lockfile; resolved versions cannot be checked")

    if not items and not errors:
        print(f"No .NET project or npm/pnpm lockfile found under {root}.")
        return 2

    findings: list[tuple] = []
    for where, val in PNPM_GATES:
        findings.append(("❌", "pnpm", f"{where}/pnpm-workspace.yaml", val or "not set",
                         f"minimumReleaseAge must be ≥ {PNPM_MIN_RELEASE_AGE_MINUTES} (7 days)"))
    fw_rows = framework_findings(root, tfms, bool(js_locks), findings)

    uniq = {(it["eco"], it["name"].lower()): it["name"] for it in items.values()}
    metas: dict = {}
    with cf.ThreadPoolExecutor(max_workers=16) as pool:
        futs = {pool.submit(nuget_meta if eco == "nuget" else npm_meta, name): (eco, key)
                for (eco, key), name in uniq.items()}
        for fut in cf.as_completed(futs):
            try:
                metas[futs[fut]] = fut.result()
            except Exception as ex:  # network failure → cannot judge, never silently pass
                metas[futs[fut]] = None
                errors.append(f"Lookup of {futs[fut][1]} failed: {ex}")

    direct_rows = []
    for it in sorted(items.values(), key=lambda i: (i["eco"], i["name"].lower())):
        row = check(it, metas.get((it["eco"], it["name"].lower())), findings)
        if it["direct"]:
            direct_rows.append(row)

    n_direct = sum(1 for i in items.values() if i["direct"])
    count = {lv: sum(1 for f in findings if f[0] == lv) for lv in ("❌", "⚠️", "ℹ️")}
    out = ["# Package Compliance Report", "",
           f"- Project: `{root}`",
           f"- Checked: {NOW.strftime('%Y-%m-%d %H:%M')} UTC",
           f"- Scope: {n_direct} direct, {len(items) - n_direct} transitive",
           f"- Result: ❌ violations {count['❌']} · ⚠️ needs review {count['⚠️']} · ℹ️ suggestions {count['ℹ️']}", ""]
    if errors:
        out += ["## Could not check", ""] + [f"- {cell(e)}" for e in errors] + [""]
    if fw_rows:
        out += ["## Framework / runtime", "", "| Item | Current | Latest LTS | Result |", "|---|---|---|---|"]
        out += [f"| {' | '.join(cell(c) for c in r)} |" for r in fw_rows] + [""]
    for lv, title in (("❌", "Violations"), ("⚠️", "Needs review"), ("ℹ️", "Suggestions")):
        rows = [f for f in findings if f[0] == lv]
        if rows:
            out += [f"## {lv} {title}", "", "| Type | Package | Version | Detail |", "|---|---|---|---|"]
            out += [f"| {cell(t)} | {cell(n)} | {cell(v)} | {cell(m)} |" for _, t, n, v, m in rows] + [""]
    if direct_rows:
        out += ["## Direct packages", "", "| Ecosystem | Package | Current | Latest compliant | License | Last release | Result |",
                "|---|---|---|---|---|---|---|"]
        for r in direct_rows:
            res = "".join(sorted(set(r["flags"]), key="❌⚠️ℹ️".index)) or "✅"
            name = r["name"] + (" (dev)" if r["dev"] and r["eco"] == "npm" else "")
            out.append(f"| {r['eco']} | {cell(name)} | {cell(r['version'])} | {cell(r['cand'])} | "
                       f"{cell(r['license'])} | {r['last']} | {res} |")
        out.append("")
    print("\n".join(out))
    if errors and not findings:
        return 2
    return 1 if count["❌"] else 0


if __name__ == "__main__":
    sys.exit(main())
