#!/usr/bin/env bash
# ============================================================================
# scaffold.sh — mechanical .NET backend project scaffolder for dotnet-project-setup
#
# Owns ONLY the deterministic, error-prone mechanics:
#   - solution + all projects (DDD/Clean/CQRS layout)
#   - project references
#   - CPM files created FIRST so every `dotnet add package` respects CPM
#   - package installation (versions auto-resolved to latest stable by `dotnet`)
#   - Mapperly PrivateAssets csproj edit (no CLI flag exists for this)
#   - conditional DB provider / OpenTelemetry notes
#
# It does NOT write template file CONTENT (Program.cs, domain building blocks,
# appsettings, AGENTS.md, tests). Those are context-dependent (Rich vs Anemic,
# domain-event flow, chosen options) and are filled in by the skill from the
# reference templates AFTER this script runs.
#
# Version policy is satisfied automatically: modern `dotnet add package` under
# CPM writes NO Version to the csproj and resolves the latest STABLE (never
# prerelease) version into Directory.Packages.props. No manual version stripping.
#
# Usage:
#   scaffold.sh --name <ProjectName> --db <sqlserver|postgres> \
#               [--otel <none|otlp>]   # none = no OpenTelemetry package; otlp = logs/traces/metrics over OTLP \
#               [--into-existing] [--dir <parent-dir>]
#
# Example:
#   scaffold.sh --name Acme.OrderService --db sqlserver --otel otlp
#
# The entry-point project is always named "<Name>.Api" (this layer IS the HTTP entry point).
#
# --into-existing: scaffold into a directory that already exists. Intended for the
#   common case of a repo that already holds README/docs but no code yet. Still
#   refuses when a solution is already present (src/ or *.slnx).
# ============================================================================
set -euo pipefail

# ---- defaults ----
NAME=""
DB=""
OTEL="none"
INTO_EXISTING="false"
PARENT_DIR="."

usage() { grep -E '^#( |$)' "$0" | sed 's/^# \{0,1\}//'; exit "${1:-0}"; }

while [[ $# -gt 0 ]]; do
  case "$1" in
    --name)          NAME="$2"; shift 2 ;;
    --db)            DB="$2"; shift 2 ;;
    --otel)          OTEL="$2"; shift 2 ;;
    --into-existing) INTO_EXISTING="true"; shift ;;
    --dir)           PARENT_DIR="$2"; shift 2 ;;
    -h|--help)       usage 0 ;;
    *) echo "Unknown arg: $1" >&2; usage 1 ;;
  esac
done

# ---- validation ----
[[ -n "$NAME" ]] || { echo "ERROR: --name is required" >&2; exit 1; }
case "$DB" in sqlserver|postgres) ;; *) echo "ERROR: --db must be sqlserver|postgres (got '$DB')" >&2; exit 1 ;; esac
case "$OTEL" in none|otlp) ;; *) echo "ERROR: --otel must be none|otlp" >&2; exit 1 ;; esac
command -v dotnet >/dev/null || { echo "ERROR: dotnet CLI not found" >&2; exit 1; }

ROOT="$PARENT_DIR/$NAME"
if [[ -e "$ROOT" ]]; then
  # A directory holding only README/docs is a normal starting point; a directory
  # that already holds a solution is not — never scaffold over existing code.
  if [[ "$INTO_EXISTING" != "true" ]]; then
    echo "ERROR: target '$ROOT' already exists — pass --into-existing to scaffold into it" >&2; exit 1
  fi
  if [[ -d "$ROOT/src" ]] || compgen -G "$ROOT/*.slnx" >/dev/null || compgen -G "$ROOT/*.sln" >/dev/null; then
    echo "ERROR: '$ROOT' already contains a solution (src/ or *.sln[x]) — refusing to overwrite" >&2; exit 1
  fi
fi

S="src/$NAME"        # source path prefix
T="tests/$NAME"      # tests path prefix
W="$S.Api"                    # entry-point project dir
WCS="$W/$NAME.Api.csproj"     # entry-point csproj

echo ">>> Scaffolding $NAME  (db=$DB, otel=$OTEL)"
mkdir -p "$ROOT"; cd "$ROOT"

# ---------------------------------------------------------------------------
# 1. CPM files FIRST — so every subsequent `dotnet add package` respects CPM
#    (csproj gets no Version; version lands in Directory.Packages.props)
# ---------------------------------------------------------------------------
cat > Directory.Build.props <<'EOF'
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
EOF

cat > Directory.Packages.props <<'EOF'
<Project>
  <ItemGroup>
  </ItemGroup>
</Project>
EOF

# The target framework is fixed by Directory.Build.props; every project and
# package version below follows it, whatever newer SDK is installed.
TFM="$(sed -nE 's/.*<TargetFramework>(.*)<\/TargetFramework>.*/\1/p' Directory.Build.props)"
TFM_MAJOR="${TFM#net}"; TFM_MAJOR="${TFM_MAJOR%%.*}"

# 0 when a nuspec's dependency-group TFMs ($1, one per line) include one usable
# by $TFM. No groups = framework-neutral package (e.g. a dotnet tool).
tfm_compatible() {
  [[ -z "$1" ]] && return 0
  local t major
  while IFS= read -r t; do
    case "$t" in
      netstandard*|.netstandard*) return 0 ;;
      net[0-9]*.[0-9]*) major="${t#net}"; major="${major%%.*}"; (( major <= TFM_MAJOR )) && return 0 ;;
    esac
  done <<< "$1"
  return 1
}

# Latest STABLE version usable by $TFM — prerelease (any "-suffix", e.g.
# 11.0.0-rc.3) is excluded, and so is a version that only targets a newer .NET
# (e.g. net11.0-only on a net10.0 project). NuGet may serve CRLF line endings.
latest_stable() {
  local pkg_lc ver tfms; pkg_lc="$(echo "$1" | tr '[:upper:]' '[:lower:]')"
  for ver in $(curl -s "https://api.nuget.org/v3-flatcontainer/$pkg_lc/index.json" 2>/dev/null \
      | sed -E 's/.*\[//; s/\].*//' | tr ',' '\n' | tr -d '" \r\t' \
      | grep -E '^[0-9]+\.[0-9]+\.[0-9]+(\.[0-9]+)?$' | sort -uVr || true); do
    tfms="$(curl -s "https://api.nuget.org/v3-flatcontainer/$pkg_lc/$ver/$pkg_lc.nuspec" 2>/dev/null \
      | grep -oE 'targetFramework="[^"]*"' | cut -d'"' -f2 | tr '[:upper:]' '[:lower:]' | sort -u || true)"
    tfm_compatible "$tfms" && { echo "$ver"; return 0; }
  done
  return 0
}

# ---------------------------------------------------------------------------
# 2. Solution + local tool manifest (dotnet-ef committed as local tool)
# ---------------------------------------------------------------------------
dotnet new sln -n "$NAME" --format slnx
# On SDK 10.0.301 `dotnet new tool-manifest` writes ./dotnet-tools.json at the CWD,
# not .config/dotnet-tools.json. Normalize to the conventional .config/ location.
dotnet new tool-manifest
if [[ -f dotnet-tools.json && ! -f .config/dotnet-tools.json ]]; then
  mkdir -p .config && mv dotnet-tools.json .config/dotnet-tools.json
fi
# dotnet-ef matches the EF Core version the projects get.
EF_VER="$(latest_stable Microsoft.EntityFrameworkCore.Design)"
[[ -n "$EF_VER" ]] || { echo "ERROR: cannot resolve a $TFM-compatible EF Core version (network?)" >&2; exit 1; }
dotnet tool install dotnet-ef --version "$EF_VER"

# ---------------------------------------------------------------------------
# 3. Source + test projects
# ---------------------------------------------------------------------------
dotnet new classlib -n "$NAME.Domain"         -o "$S.Domain"         -f "$TFM"
dotnet new classlib -n "$NAME.Application"    -o "$S.Application"    -f "$TFM"
dotnet new classlib -n "$NAME.Infrastructure" -o "$S.Infrastructure" -f "$TFM"
dotnet new classlib -n "$NAME.Persistence"    -o "$S.Persistence"    -f "$TFM"
dotnet new webapi   -n "$NAME.Api" -o "$W"                           -f "$TFM"
dotnet new xunit    -n "$NAME.UnitTests"        -o "$T.UnitTests"        -f "$TFM"
dotnet new xunit    -n "$NAME.IntegrationTests" -o "$T.IntegrationTests" -f "$TFM"

# IntegrationTests must use the Web SDK for WebApplicationFactory
INTEG_CSPROJ="$T.IntegrationTests/$NAME.IntegrationTests.csproj"
# portable in-place edit (macOS/BSD & GNU sed)
perl -0pi -e 's/<Project Sdk="Microsoft\.NET\.Sdk">/<Project Sdk="Microsoft.NET.Sdk.Web">/' "$INTEG_CSPROJ"

# Remove template boilerplate
find src   -name "Class1.cs"    -delete || true
find tests -name "UnitTest1.cs" -delete || true

# ---------------------------------------------------------------------------
# CPM normalization: `dotnet new webapi`/`xunit` templates pre-add
# <PackageReference Include="X" Version="Y" />. Under CPM the inline Version is
# an ERROR. Move each package into Directory.Packages.props at its latest stable
# $TFM-compatible version (the template's SDK-bundled version is usually older) and strip the
# inline Version from the csproj. A later `dotnet add` never bumps a version
# already in Directory.Packages.props, so this is the only place to fix it.
# ---------------------------------------------------------------------------
normalize_cpm() {
  local csproj="$1"
  # Collect Include/Version pairs from this csproj
  perl -ne 'print "$1\t$2\n" if /<PackageReference\s+Include="([^"]+)"\s+Version="([^"]+)"\s*\/?>/' "$csproj" \
  | while IFS=$'\t' read -r pkg ver; do
      # Add to CPM file only if not already present
      if ! grep -q "PackageVersion Include=\"$pkg\"" Directory.Packages.props; then
        ver="$(latest_stable "$pkg")"
        [[ -n "$ver" ]] || { echo "ERROR: cannot resolve a $TFM-compatible stable version of $pkg (network?)" >&2; exit 1; }
        perl -0pi -e "s{(<ItemGroup>)}{\$1\n    <PackageVersion Include=\"$pkg\" Version=\"$ver\" />}" Directory.Packages.props
      fi
    done
  # Strip the inline Version attribute from every PackageReference in this csproj
  perl -0pi -e 's{(<PackageReference\s+Include="[^"]+")\s+Version="[^"]+"\s*(/?>)}{$1 $2}g' "$csproj"
}
for c in $(find src tests -name "*.csproj"); do normalize_cpm "$c"; done

# Add every project to the solution
dotnet sln add $(find src tests -name "*.csproj")

# ---------------------------------------------------------------------------
# 4. Project references (dependency direction: outer -> inner)
# ---------------------------------------------------------------------------
dotnet add "$S.Application/$NAME.Application.csproj"       reference "$S.Domain/$NAME.Domain.csproj"
dotnet add "$S.Infrastructure/$NAME.Infrastructure.csproj" reference "$S.Application/$NAME.Application.csproj"
dotnet add "$S.Persistence/$NAME.Persistence.csproj"       reference "$S.Application/$NAME.Application.csproj"
dotnet add "$S.Persistence/$NAME.Persistence.csproj"       reference "$S.Domain/$NAME.Domain.csproj"
dotnet add "$WCS"                 reference "$S.Application/$NAME.Application.csproj"
dotnet add "$WCS"                 reference "$S.Infrastructure/$NAME.Infrastructure.csproj"
dotnet add "$WCS"                 reference "$S.Persistence/$NAME.Persistence.csproj"
dotnet add "$T.UnitTests/$NAME.UnitTests.csproj"          reference "$S.Domain/$NAME.Domain.csproj"
dotnet add "$T.UnitTests/$NAME.UnitTests.csproj"          reference "$S.Application/$NAME.Application.csproj"
dotnet add "$T.IntegrationTests/$NAME.IntegrationTests.csproj" reference "$WCS"

# ---------------------------------------------------------------------------
# 5. Packages (latest stable $TFM-compatible version, recorded in Directory.Packages.props)
#    `dotnet add` alone picks the newest stable even when it targets a newer .NET,
#    so the version is resolved here and passed explicitly.
# ---------------------------------------------------------------------------
add() {
  local proj="$1" pkg="$2" ver
  ver="$(latest_stable "$pkg")"
  [[ -n "$ver" ]] || { echo "ERROR: cannot resolve a $TFM-compatible stable version of $pkg (network?)" >&2; exit 1; }
  dotnet add "$proj" package "$pkg" --version "$ver"
}

# CQRS core lives in Application (single Cqrs/CQRS.cs); needs DI abstractions for AddCqrs assembly scanning
add "$S.Application/$NAME.Application.csproj" Microsoft.Extensions.DependencyInjection.Abstractions

# Persistence: EF Core (+ Relational for entity config APIs) + Dapper; entry-point project: EF Core Design (for dotnet-ef)
add "$S.Persistence/$NAME.Persistence.csproj" Microsoft.EntityFrameworkCore
# Relational is REQUIRED in Persistence: ToTable/HasColumnName/HasDefaultValueSql/HasMaxLength
# and migrations all live in EntityFrameworkCore.Relational, not the base package.
add "$S.Persistence/$NAME.Persistence.csproj" Microsoft.EntityFrameworkCore.Relational
add "$S.Persistence/$NAME.Persistence.csproj" Dapper
# Design-time factory (WriteDbContextFactory) builds IConfiguration from appsettings.json + env vars.
add "$S.Persistence/$NAME.Persistence.csproj" Microsoft.Extensions.Configuration.Json
add "$S.Persistence/$NAME.Persistence.csproj" Microsoft.Extensions.Configuration.EnvironmentVariables
add "$WCS"           Microsoft.EntityFrameworkCore.Design

# Validation (Application defines validators; the entry-point project registers them)
add "$S.Application/$NAME.Application.csproj" FluentValidation
add "$WCS"          FluentValidation.DependencyInjectionExtensions

# Serilog (entry-point project)
for p in Serilog.AspNetCore Serilog.Settings.Configuration Serilog.Sinks.Console \
         Serilog.Sinks.File Serilog.Enrichers.Environment Serilog.Enrichers.Process \
         Serilog.Enrichers.Thread; do
  add "$WCS" "$p"
done

# API docs: Microsoft OpenAPI + Scalar
add "$WCS" Microsoft.AspNetCore.OpenApi
add "$WCS" Scalar.AspNetCore

# HTTP resilience (Infrastructure owns outbound HTTP registrations)
add "$S.Infrastructure/$NAME.Infrastructure.csproj" Microsoft.Extensions.Http.Resilience

# Tests
add "$T.UnitTests/$NAME.UnitTests.csproj" NSubstitute
add "$T.IntegrationTests/$NAME.IntegrationTests.csproj" NSubstitute
add "$T.IntegrationTests/$NAME.IntegrationTests.csproj" Microsoft.AspNetCore.Mvc.Testing

# DB provider. The entry-point project owns DI registration, BUT the provider is ALSO needed in
# Persistence: `dotnet ef migrations` emits provider-specific annotations into the
# Persistence project, so without it migrations fail with CS0246 (provider not found).
DB_PKG=""
case "$DB" in
  sqlserver) DB_PKG="Microsoft.EntityFrameworkCore.SqlServer" ;;
  postgres)  DB_PKG="Npgsql.EntityFrameworkCore.PostgreSQL" ;;
esac
add "$WCS"           "$DB_PKG"
add "$S.Persistence/$NAME.Persistence.csproj" "$DB_PKG"

# Object mapper — Mapperly, fixed
add "$S.Application/$NAME.Application.csproj" Riok.Mapperly
# Mapperly is a source generator: must not leak as a transitive runtime dep.
# No CLI flag exists — edit the PackageReference to add PrivateAssets/IncludeAssets.
APP_CSPROJ="$S.Application/$NAME.Application.csproj"
# NOTE: 'compile' MUST be in IncludeAssets — Riok.Mapperly.Abstractions ships the
# [Mapper] attribute as a compile-time reference. Omitting 'compile' makes the
# attribute non-referenceable and every mapper fails with CS0246. PrivateAssets=all
# still stops it leaking as a transitive runtime dependency.
perl -0pi -e 's{<PackageReference Include="Riok\.Mapperly"\s*/>}{<PackageReference Include="Riok.Mapperly">\n      <IncludeAssets>compile; runtime; build; native; contentfiles; analyzers</IncludeAssets>\n      <PrivateAssets>all</PrivateAssets>\n    </PackageReference>}' "$APP_CSPROJ"

# OpenTelemetry (entry-point project) — optional. none installs nothing (tracing and
# metrics use the built-in Activity/Meter APIs); otlp exports logs/traces/metrics.
case "$OTEL" in
  otlp)
    for p in OpenTelemetry.Extensions.Hosting OpenTelemetry.Instrumentation.AspNetCore \
             OpenTelemetry.Instrumentation.Http OpenTelemetry.Exporter.OpenTelemetryProtocol; do
      add "$WCS" "$p"
    done ;;
  none) : ;;
esac

# ---------------------------------------------------------------------------
# 5b. Pin vulnerable transitive packages (generic self-heal).
#     TreatWarningsAsErrors + a transitive NU1901–NU1904 (low → critical) fails
#     restore. `dotnet list --vulnerable` needs a successful restore (which we
#     don't have), so we scrape those lines from `dotnet restore` itself and pin
#     each named package to its latest STABLE version on the project the error names.
#     Loop until clean or no progress.
# ---------------------------------------------------------------------------
pin_vulnerable_transitives() {
  local round pinned_any restore_out line pkg ver_new proj add_out
  for round in 1 2 3; do
    restore_out="$(dotnet restore 2>&1 || true)"
    echo "$restore_out" | grep -qE 'NU190[1-4]' || { echo ">>> No vulnerable transitives remaining."; return 0; }
    pinned_any="false"
    # Each NU190x line names: <csproj> ... Package 'X' <ver> has a known ...
    while IFS= read -r line; do
      pkg="$(echo "$line" | sed -nE "s/.*Package '([^']+)'.*/\1/p")"
      proj="$(echo "$line" | grep -oE '[^ ]+\.csproj' | head -1)"
      [[ -n "$pkg" && -n "$proj" ]] || continue
      grep -q "PackageReference Include=\"$pkg\"" "$proj" 2>/dev/null && continue  # already pinned
      ver_new="$(latest_stable "$pkg")"
      [[ -n "$ver_new" ]] || { echo "!!! WARNING: cannot resolve patched version for $pkg" >&2; continue; }
      echo ">>> Pinning vulnerable transitive $pkg -> $ver_new in $(basename "$proj")"
      if add_out="$(dotnet add "$proj" package "$pkg" --version "$ver_new" 2>&1)"; then
        pinned_any="true"
      else
        echo "!!! WARNING: pinning $pkg $ver_new failed:" >&2
        echo "$add_out" | grep -iE 'error' >&2 || true
      fi
    done < <(echo "$restore_out" | grep -E 'NU190[1-4]' | sort -u)
    [[ "$pinned_any" == "true" ]] || { echo "!!! WARNING: vulnerable transitives remain but nothing new to pin — resolve manually." >&2; return 1; }
  done
  echo "!!! WARNING: still seeing vulnerable transitives after 3 rounds — resolve manually." >&2; return 1
}
pin_vulnerable_transitives || true

# ---------------------------------------------------------------------------
# 6. Summary + explicit next steps for the skill (content templates NOT written here)
# ---------------------------------------------------------------------------
cat <<EOF

>>> Structure + packages complete for $NAME.
>>> Directory.Packages.props now holds resolved latest-stable versions.

NEXT (done by the skill, not this script — they are context-dependent):
  - Overwrite $W/Program.cs from dotnet-scaffold.md (Serilog bootstrap, OpenAPI+Scalar, etc.);
    --otel $OTEL: apply the OpenTelemetry (--otel otlp) block only for otlp; replace <ProjectName> -> $NAME
  - Copy CQRS core from dotnet-cqrs-template.cs into $S.Application/Cqrs/CQRS.cs (namespace $NAME.Application.Cqrs; replace YourProject -> $NAME)
  - Scaffold domain building blocks from dotnet-domain-template.cs per chosen model style
  - Write appsettings.json / appsettings.Development.json (ConnectionStrings:Write + :Read, same value without a replica)
  - Create AGENTS.md (root, short) + docs/agents/dotnet-rules.md (full rules)
  - Write meaningful tests, then: dotnet build && dotnet test
EOF
echo ">>> DB CONFIG (fixed): CQRS + read/write split is mandatory. Register WriteDbContext (ConnectionStrings:Write) AND ReadDbContext (ConnectionStrings:Read), both deriving from abstract AppDbContext; both connection strings hold the same value when no replica exists. Never register a single AppDbContext. Routing rules: see docs/agents/dotnet-rules.md -> CQRS Implementation."
