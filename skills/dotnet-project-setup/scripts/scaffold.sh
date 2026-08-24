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
#   - conditional DB provider / OpenTelemetry / read-replica notes
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
#               [--mapper <mapperly|mapster|none>] \
#               [--otel <none|otlp|console|azure>] \
#               [--read-replicas] [--dir <parent-dir>]
#
# Example:
#   scaffold.sh --name Acme.OrderService --db sqlserver --mapper mapperly --otel otlp
# ============================================================================
set -euo pipefail

# ---- defaults ----
NAME=""
DB=""
MAPPER="mapperly"
OTEL="none"
READ_REPLICAS="false"
PARENT_DIR="."

usage() { grep -E '^#( |$)' "$0" | sed 's/^# \{0,1\}//'; exit "${1:-0}"; }

while [[ $# -gt 0 ]]; do
  case "$1" in
    --name)          NAME="$2"; shift 2 ;;
    --db)            DB="$2"; shift 2 ;;
    --mapper)        MAPPER="$2"; shift 2 ;;
    --otel)          OTEL="$2"; shift 2 ;;
    --read-replicas) READ_REPLICAS="true"; shift ;;
    --dir)           PARENT_DIR="$2"; shift 2 ;;
    -h|--help)       usage 0 ;;
    *) echo "Unknown arg: $1" >&2; usage 1 ;;
  esac
done

# ---- validation ----
[[ -n "$NAME" ]] || { echo "ERROR: --name is required" >&2; exit 1; }
case "$DB" in sqlserver|postgres) ;; *) echo "ERROR: --db must be sqlserver|postgres (got '$DB')" >&2; exit 1 ;; esac
case "$MAPPER" in mapperly|mapster|none) ;; *) echo "ERROR: --mapper must be mapperly|mapster|none" >&2; exit 1 ;; esac
case "$OTEL" in none|otlp|console|azure) ;; *) echo "ERROR: --otel must be none|otlp|console|azure" >&2; exit 1 ;; esac
command -v dotnet >/dev/null || { echo "ERROR: dotnet CLI not found" >&2; exit 1; }

ROOT="$PARENT_DIR/$NAME"
[[ ! -e "$ROOT" ]] || { echo "ERROR: target '$ROOT' already exists — refusing to overwrite" >&2; exit 1; }

S="src/$NAME"        # source path prefix
T="tests/$NAME"      # tests path prefix

echo ">>> Scaffolding $NAME  (db=$DB, mapper=$MAPPER, otel=$OTEL, read-replicas=$READ_REPLICAS)"
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
dotnet tool install dotnet-ef

# ---------------------------------------------------------------------------
# 3. Source + test projects
# ---------------------------------------------------------------------------
dotnet new classlib -n "$NAME.Domain"         -o "$S.Domain"
dotnet new classlib -n "$NAME.Application"    -o "$S.Application"
dotnet new classlib -n "$NAME.Infrastructure" -o "$S.Infrastructure"
dotnet new classlib -n "$NAME.Persistence"    -o "$S.Persistence"
dotnet new webapi   -n "$NAME.WebApi"         -o "$S.WebApi"
dotnet new xunit    -n "$NAME.UnitTests"        -o "$T.UnitTests"
dotnet new xunit    -n "$NAME.IntegrationTests" -o "$T.IntegrationTests"

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
# an ERROR. Move each version into Directory.Packages.props and strip it from
# the csproj — preserving the SDK-matched stable versions the templates chose.
# ---------------------------------------------------------------------------
normalize_cpm() {
  local csproj="$1"
  # Collect Include/Version pairs from this csproj
  perl -ne 'print "$1\t$2\n" if /<PackageReference\s+Include="([^"]+)"\s+Version="([^"]+)"\s*\/?>/' "$csproj" \
  | while IFS=$'\t' read -r pkg ver; do
      # Add to CPM file only if not already present
      if ! grep -q "PackageVersion Include=\"$pkg\"" Directory.Packages.props; then
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
dotnet add "$S.WebApi/$NAME.WebApi.csproj"                 reference "$S.Application/$NAME.Application.csproj"
dotnet add "$S.WebApi/$NAME.WebApi.csproj"                 reference "$S.Infrastructure/$NAME.Infrastructure.csproj"
dotnet add "$S.WebApi/$NAME.WebApi.csproj"                 reference "$S.Persistence/$NAME.Persistence.csproj"
dotnet add "$T.UnitTests/$NAME.UnitTests.csproj"          reference "$S.Domain/$NAME.Domain.csproj"
dotnet add "$T.UnitTests/$NAME.UnitTests.csproj"          reference "$S.Application/$NAME.Application.csproj"
dotnet add "$T.IntegrationTests/$NAME.IntegrationTests.csproj" reference "$S.WebApi/$NAME.WebApi.csproj"

# ---------------------------------------------------------------------------
# 5. Packages (versions auto-resolved to latest stable into Directory.Packages.props)
# ---------------------------------------------------------------------------
add() { local proj="$1"; local pkg="$2"; shift 2; dotnet add "$proj" package "$pkg" "$@"; }

# CQRS core lives in Application (single Cqrs/CQRS.cs); needs DI abstractions for AddCqrs assembly scanning
add "$S.Application/$NAME.Application.csproj" Microsoft.Extensions.DependencyInjection.Abstractions

# Persistence: EF Core (+ Relational for entity config APIs) + Dapper; WebApi: EF Core Design (for dotnet-ef)
add "$S.Persistence/$NAME.Persistence.csproj" Microsoft.EntityFrameworkCore
# Relational is REQUIRED in Persistence: ToTable/HasColumnName/HasDefaultValueSql/HasMaxLength
# and migrations all live in EntityFrameworkCore.Relational, not the base package.
add "$S.Persistence/$NAME.Persistence.csproj" Microsoft.EntityFrameworkCore.Relational
add "$S.Persistence/$NAME.Persistence.csproj" Dapper
add "$S.WebApi/$NAME.WebApi.csproj"           Microsoft.EntityFrameworkCore.Design

# Validation (Application defines validators; WebApi registers them)
add "$S.Application/$NAME.Application.csproj" FluentValidation
add "$S.WebApi/$NAME.WebApi.csproj"          FluentValidation.DependencyInjectionExtensions

# Serilog (WebApi)
for p in Serilog.AspNetCore Serilog.Settings.Configuration Serilog.Sinks.Console \
         Serilog.Sinks.File Serilog.Enrichers.Environment Serilog.Enrichers.Process \
         Serilog.Enrichers.Thread; do
  add "$S.WebApi/$NAME.WebApi.csproj" "$p"
done

# API docs: Microsoft OpenAPI + Scalar
add "$S.WebApi/$NAME.WebApi.csproj" Microsoft.AspNetCore.OpenApi
add "$S.WebApi/$NAME.WebApi.csproj" Scalar.AspNetCore

# --- Transitive security pin: Microsoft.OpenApi ---
# Microsoft.AspNetCore.OpenApi 10.x transitively pulls Microsoft.OpenApi 2.0.0,
# which has a KNOWN HIGH-SEVERITY vuln (NU1903, GHSA-v5pm-xwqc-g5wc). With
# TreatWarningsAsErrors that fails restore. The fix is subtle: DO NOT pin to the
# absolute latest (3.x) — its API made IOpenApiMediaType.Example read-only and
# breaks the ASP.NET Core source generator. Pin to the latest PATCHED 2.x line.
OPENAPI_2X="$(curl -s https://api.nuget.org/v3-flatcontainer/microsoft.openapi/index.json 2>/dev/null \
  | tr ',' '\n' | grep -oE '2\.[0-9]+\.[0-9]+' | sort -uV | tail -1 || true)"
if [[ -n "$OPENAPI_2X" ]]; then
  echo ">>> Pinning transitive Microsoft.OpenApi to latest patched 2.x ($OPENAPI_2X) to clear NU1903"
  add "$S.WebApi/$NAME.WebApi.csproj" Microsoft.OpenApi --version "$OPENAPI_2X"
else
  echo "!!! WARNING: could not resolve latest Microsoft.OpenApi 2.x. If restore fails with NU1903," >&2
  echo "!!! manually add a patched Microsoft.OpenApi 2.x PackageVersion to Directory.Packages.props." >&2
fi

# HTTP resilience (Infrastructure owns outbound HTTP registrations)
add "$S.Infrastructure/$NAME.Infrastructure.csproj" Microsoft.Extensions.Http.Resilience

# Tests
# FluentAssertions 8.x switched to a PAID commercial (Xceed) licence — forbidden by the
# MIT/Apache-only policy. Pin to the latest 7.x (Apache-2.0). Resolve it dynamically.
FA7="$(curl -s https://api.nuget.org/v3-flatcontainer/fluentassertions/index.json 2>/dev/null \
  | tr ',' '\n' | grep -oE '7\.[0-9]+\.[0-9]+' | sort -uV | tail -1)"
FA7="${FA7:-7.2.2}"   # fallback to a known Apache-2.0 release if offline
echo ">>> Pinning FluentAssertions to $FA7 (Apache-2.0; 8.x is a paid licence)"
add "$T.UnitTests/$NAME.UnitTests.csproj" FluentAssertions --version "$FA7"
add "$T.UnitTests/$NAME.UnitTests.csproj" NSubstitute
add "$T.IntegrationTests/$NAME.IntegrationTests.csproj" FluentAssertions --version "$FA7"
add "$T.IntegrationTests/$NAME.IntegrationTests.csproj" NSubstitute
add "$T.IntegrationTests/$NAME.IntegrationTests.csproj" Microsoft.AspNetCore.Mvc.Testing

# DB provider. WebApi owns DI registration, BUT the provider is ALSO needed in
# Persistence: `dotnet ef migrations` emits provider-specific annotations into the
# Persistence project, so without it migrations fail with CS0246 (provider not found).
DB_PKG=""
case "$DB" in
  sqlserver) DB_PKG="Microsoft.EntityFrameworkCore.SqlServer" ;;
  postgres)  DB_PKG="Npgsql.EntityFrameworkCore.PostgreSQL" ;;
esac
add "$S.WebApi/$NAME.WebApi.csproj"           "$DB_PKG"
add "$S.Persistence/$NAME.Persistence.csproj" "$DB_PKG"

# Object mapper
case "$MAPPER" in
  mapperly)
    add "$S.Application/$NAME.Application.csproj" Riok.Mapperly
    # Mapperly is a source generator: must not leak as a transitive runtime dep.
    # No CLI flag exists — edit the PackageReference to add PrivateAssets/IncludeAssets.
    APP_CSPROJ="$S.Application/$NAME.Application.csproj"
    # NOTE: 'compile' MUST be in IncludeAssets — Riok.Mapperly.Abstractions ships the
    # [Mapper] attribute as a compile-time reference. Omitting 'compile' makes the
    # attribute non-referenceable and every mapper fails with CS0246. PrivateAssets=all
    # still stops it leaking as a transitive runtime dependency.
    perl -0pi -e 's{<PackageReference Include="Riok\.Mapperly"\s*/>}{<PackageReference Include="Riok.Mapperly">\n      <IncludeAssets>compile; runtime; build; native; contentfiles; analyzers</IncludeAssets>\n      <PrivateAssets>all</PrivateAssets>\n    </PackageReference>}' "$APP_CSPROJ"
    ;;
  mapster)
    add "$S.Application/$NAME.Application.csproj" Mapster ;;
  none) ;;
esac

# OpenTelemetry (WebApi) — conditional
if [[ "$OTEL" != "none" ]]; then
  add "$S.WebApi/$NAME.WebApi.csproj" OpenTelemetry.Extensions.Hosting
  add "$S.WebApi/$NAME.WebApi.csproj" OpenTelemetry.Instrumentation.AspNetCore
  add "$S.WebApi/$NAME.WebApi.csproj" OpenTelemetry.Instrumentation.Http
  case "$OTEL" in
    otlp)    add "$S.WebApi/$NAME.WebApi.csproj" OpenTelemetry.Exporter.OpenTelemetryProtocol ;;
    console) add "$S.WebApi/$NAME.WebApi.csproj" OpenTelemetry.Exporter.Console ;;
    azure)   add "$S.WebApi/$NAME.WebApi.csproj" Azure.Monitor.OpenTelemetry.AspNetCore ;;
  esac
fi

# ---------------------------------------------------------------------------
# 5b. Pin vulnerable transitive packages (generic self-heal).
#     TreatWarningsAsErrors + a transitive NU1903 fails restore. `dotnet list
#     --vulnerable` needs a successful restore (which we don't have), so we scrape
#     the NU1903 lines from `dotnet restore` itself and pin each named package to
#     its latest STABLE version on the project the error names. Loop until clean
#     or no progress. Microsoft.OpenApi is already pinned to 2.x above (its 3.x
#     breaks the source generator) and is skipped here.
# ---------------------------------------------------------------------------
latest_stable() {
  local pkg_lc; pkg_lc="$(echo "$1" | tr '[:upper:]' '[:lower:]')"
  curl -s "https://api.nuget.org/v3-flatcontainer/$pkg_lc/index.json" 2>/dev/null \
    | tr ',' '\n' | grep -oE '[0-9]+\.[0-9]+\.[0-9]+' | sort -uV | tail -1
}

pin_vulnerable_transitives() {
  local round pinned_any restore_out line pkg ver_new proj
  for round in 1 2 3; do
    restore_out="$(dotnet restore 2>&1 || true)"
    echo "$restore_out" | grep -q 'NU1903' || { echo ">>> No vulnerable transitives remaining."; return 0; }
    pinned_any="false"
    # Each NU1903 line names: <csproj> ... Package 'X' <ver> has a known ...
    while IFS= read -r line; do
      pkg="$(echo "$line" | sed -nE "s/.*Package '([^']+)'.*/\1/p")"
      proj="$(echo "$line" | grep -oE '[^ ]+\.csproj' | head -1)"
      [[ -n "$pkg" && -n "$proj" ]] || continue
      [[ "$pkg" == "Microsoft.OpenApi" ]] && continue   # handled by explicit 2.x pin
      grep -q "PackageReference Include=\"$pkg\"" "$proj" 2>/dev/null && continue  # already pinned
      ver_new="$(latest_stable "$pkg")"
      [[ -n "$ver_new" ]] || { echo "!!! WARNING: cannot resolve patched version for $pkg" >&2; continue; }
      echo ">>> Pinning vulnerable transitive $pkg -> $ver_new in $(basename "$proj")"
      dotnet add "$proj" package "$pkg" --version "$ver_new" >/dev/null 2>&1 && pinned_any="true"
    done < <(echo "$restore_out" | grep 'NU1903')
    [[ "$pinned_any" == "true" ]] || { echo "!!! WARNING: NU1903 remains but nothing new to pin — resolve manually." >&2; return 1; }
  done
  echo "!!! WARNING: still seeing NU1903 after 3 rounds — resolve manually." >&2; return 1
}
pin_vulnerable_transitives || true

# ---------------------------------------------------------------------------
# 6. Summary + explicit next steps for the skill (content templates NOT written here)
# ---------------------------------------------------------------------------
cat <<EOF

>>> Structure + packages complete for $NAME.
>>> Directory.Packages.props now holds resolved latest-stable versions.

NEXT (done by the skill, not this script — they are context-dependent):
  - Overwrite $S.WebApi/Program.cs from dotnet-scaffold.md (Serilog bootstrap, OpenAPI+Scalar, etc.)
  - Copy CQRS core from dotnet-cqrs-template.cs into $S.Application/Cqrs/CQRS.cs (namespace $NAME.Application.Cqrs; replace YourProject -> $NAME)
  - Scaffold domain building blocks from dotnet-domain-template.cs per chosen model style
  - Write appsettings.json / appsettings.Development.json (read-replicas=$READ_REPLICAS)
  - Create AGENTS.md (root, short) + docs/ai/dotnet-rules.md (full rules)
  - Write meaningful tests, then: dotnet build && dotnet test
EOF
if [[ "$READ_REPLICAS" == "true" ]]; then
  echo ">>> READ REPLICAS: use ConnectionStrings:Write + ConnectionStrings:ReadOnly and separate Write/Read DbContext registrations."
fi
