# .NET Backend Project Scaffold Guide

> Referenced by `0-dotnet-project-setup` skill. Read this file in full before scaffolding a .NET project.

## Prerequisites

- .NET 10 SDK installed
- `dotnet` CLI available

---

## Architecture

This project follows **DDD (Domain-Driven Design) + Clean Architecture + CQRS**.

Dependency direction (outer → inner):

- `Api` (entry point) → `Application` → `Domain`
- `Infrastructure` → `Application`
- `Persistence` → `Application`
- `Persistence` → `Domain`

Key principles:

- **Domain** is the innermost layer: entities, aggregates, value objects, domain services, domain events. Under the Rich model it also defines the per-aggregate `IXxxRepository` interfaces and `IUnitOfWork`; under Anemic those live in `Application` (see dotnet-rules.md -> *Where repository interfaces live*). Zero framework references either way.
- **Application** orchestrates domain objects via command/query handlers. It owns `IXxxReadRepository` always, and — under the Anemic model — `IXxxRepository`, `IUnitOfWork`, and the domain-event interfaces as well. No direct DB or HTTP calls.
- **Persistence** implements the per-aggregate `IXxxRepository` interfaces, `IUnitOfWork`, and `IXxxReadRepository`, wherever each is declared. Owns EF Core DbContext, Dapper/raw SQL access, migrations, repositories, Unit of Work. Do not create Persistence-local interfaces for internal plumbing.
- **Infrastructure** owns external service integrations (email, storage, queues, HTTP clients). It implements Domain-defined interfaces when the capability is required by Domain Services, and Application-defined interfaces only for application-only orchestration concerns.
- **Api** is the entry point: endpoints, middleware, DI wiring. Delegates all business decisions to Application through CQRS dispatchers.

---

## Directory Structure

```
<ProjectName>/
├── <ProjectName>.slnx                          # Modern solution format
├── Directory.Build.props                        # Shared MSBuild properties
├── Directory.Packages.props                     # Central Package Management (CPM)
├── AGENTS.md                                    # Short AI instruction index; points to docs/agents/*.md
├── .gitignore
├── README.md
├── docs/
│   └── agents/
│       └── dotnet-rules.md                      # Full .NET development rules copied from this skill
├── src/
│   ├── <ProjectName>.Domain/                   # DDD Domain layer — zero framework dependencies
│   │   ├── Common/                             # Result<T>, Result (domain operation outcomes)
│   │   ├── Entities/                           # Aggregate roots and entities
│   │   ├── ValueObjects/                       # Immutable value objects (record types)
│   │   ├── Enums/
│   │   ├── Events/                             # Domain events
│   │   ├── DomainServices/                     # Shared domain logic, capability-named (e.g. PricingService)
│   │   ├── Interfaces/                         # IXxxRepository (one per aggregate), IUnitOfWork, IDomainEventDispatcher, IDomainEventHandler<TEvent>
│   │   └── <ProjectName>.Domain.csproj
│   ├── <ProjectName>.Application/              # Use cases — commands, queries, handlers, validators, mappers
│   │   ├── Cqrs/
│   │   │   └── CQRS.cs                         # CQRS core — ICommand/IQuery/IHandler, dispatchers, AddCqrs (ns .Application.Cqrs)
│   │   ├── Commands/
│   │   ├── Queries/
│   │   ├── EventHandlers/                      # Domain event handlers
│   │   ├── IntegrationEvents/                  # Optional: cross-service integration events (if EDD enabled)
│   │   ├── Interfaces/                         # Optional: IEventBus or application-only ports
│   │   ├── Validators/                         # FluentValidation validators
│   │   ├── Mappers/                            # Mapperly / Mapster mapper classes (if used)
│   │   └── <ProjectName>.Application.csproj
│   ├── <ProjectName>.Infrastructure/           # External integrations (email, storage, queues)
│   │   ├── Services/
│   │   └── <ProjectName>.Infrastructure.csproj
│   ├── <ProjectName>.Persistence/             # EF Core DbContext, Repositories, Unit of Work
│   │   ├── Configurations/
│   │   ├── Migrations/
│   │   ├── Repositories/                       # Write side — implements Domain's IXxxRepository interfaces
│   │   ├── ReadRepositories/                   # Read side — implements Application's IXxxReadRepository
│   │   ├── UnitOfWork/
│   │   └── <ProjectName>.Persistence.csproj
│   └── <ProjectName>.Api/                     # Entry point — routes, middleware
│       ├── Endpoints/
│       ├── Middlewares/
│       ├── Program.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── <ProjectName>.Api.csproj
└── tests/
    ├── <ProjectName>.UnitTests/
    │   └── <ProjectName>.UnitTests.csproj
    └── <ProjectName>.IntegrationTests/
        └── <ProjectName>.IntegrationTests.csproj
```

> **Entry-point project name**: `<ProjectName>.Api` by default. `scaffold.sh --api-suffix WebApi` restores the older spelling for solutions that already use it. `Web` carries no information here — this layer *is* the HTTP entry point, and gRPC/GraphQL endpoints would live in the same project — while `WebApi` only echoes the retired ASP.NET Web API product name.

> **DI registration files**: each layer that registers services owns one `DependencyInjection.cs` holding a `public static class DependencyInjection` with its `AddPersistence()` / `AddInfrastructure()` extension. Do not prefix the file or class with the layer name (`PersistenceDependencyInjection`) — the namespace already says which layer it is, and the call site reads the same either way.

### Module-grouped variant

When the service has many business modules (SKILL.md item 5a), `Application` and `Domain` group by module instead of by technical type. `Modules/` is a fixed level; `Common/` stays at the project root:

```
src/
├── <ProjectName>.Domain/
│   ├── Modules/
│   │   ├── {Module}/
│   │   │   ├── Entities/
│   │   │   ├── ValueObjects/          # Rich only
│   │   │   ├── Events/
│   │   │   └── Enums/
│   │   └── {Module}/…
│   └── Common/                        # Result<T>, domain event base
│                                      # (+ Entity/AggregateRoot bases, Rich only)
├── <ProjectName>.Application/
│   ├── Cqrs/CQRS.cs                   # core — belongs to no module
│   ├── Modules/
│   │   ├── {Module}/
│   │   │   ├── Commands/
│   │   │   ├── Queries/
│   │   │   ├── EventHandlers/
│   │   │   ├── Dtos/
│   │   │   ├── Validators/
│   │   │   ├── Mappers/
│   │   │   └── Repositories/          # IXxxRepository / IXxxReadRepository (Anemic)
│   │   └── {Module}/…
│   └── Common/                        # IUnitOfWork, IDomainEventDispatcher,
│                                      # IDomainEventHandler<T> (Anemic)
├── <ProjectName>.Persistence/         # NOT split by module
│   ├── DependencyInjection.cs
│   ├── Configurations/ Repositories/ ReadRepositories/ UnitOfWork/ Migrations/
├── <ProjectName>.Infrastructure/      # NOT split by module
│   ├── DependencyInjection.cs
│   └── …grouped by external resource kind (Mail/, Storage/, ExternalServices/)
└── <ProjectName>.Api/
    └── Endpoints/{Module}/
```

**The asymmetry is deliberate.** `Persistence` shares one `DbContext` and one migration history, and cross-module queries are normal — splitting it by module creates folders that cannot own their schema. `Infrastructure` groups by external resource (mail, storage, each integrated system), which does not map one-to-one onto business modules. Only `Domain`, `Application`, and the endpoint layer get `Modules/`.

---

## Scaffold Commands

> **Primary path: run `scripts/scaffold.sh`** (see SKILL.md Step 3). It executes everything below in the correct order, keeps CPM correct by construction, and auto-pins vulnerable transitive packages. The command list here is the reference for *what* the script installs and a manual fallback if the script is unavailable — you normally do not hand-run these.
>
> **Project naming in this block:** the commands below spell the entry-point project `<ProjectName>.WebApi` throughout. The script's default is now `<ProjectName>.Api` (`--api-suffix` overrides it) — when hand-running these as a fallback, substitute the suffix you intend everywhere it appears.
>
> **Vulnerable-transitive note (NU1903):** `TreatWarningsAsErrors=true` turns a transitive security advisory into a build failure. One is known on .NET 10: `Microsoft.AspNetCore.OpenApi` pulls a vulnerable `Microsoft.OpenApi 2.0.0` — pin to the **latest 2.x** (its 3.x breaks the ASP.NET Core source generator, so *not* latest overall). The script pins it automatically and self-heals any *new* NU1903 it sees at restore time.

```bash
# 1. Create solution
mkdir <ProjectName> && cd <ProjectName>
dotnet new sln -n <ProjectName> --format slnx

# 1a. Set up local tool manifest and install dotnet-ef as a local tool
#     Commits .config/dotnet-tools.json to source control — no global install required.
#     Any team member can restore with: dotnet tool restore
dotnet new tool-manifest
dotnet tool install dotnet-ef

# 2. Create source projects
dotnet new classlib -n <ProjectName>.Domain         -o src/<ProjectName>.Domain
dotnet new classlib -n <ProjectName>.Application    -o src/<ProjectName>.Application
dotnet new classlib -n <ProjectName>.Infrastructure -o src/<ProjectName>.Infrastructure
dotnet new classlib -n <ProjectName>.Persistence    -o src/<ProjectName>.Persistence
dotnet new webapi   -n <ProjectName>.WebApi         -o src/<ProjectName>.WebApi

# 3. Create test projects
dotnet new xunit -n <ProjectName>.UnitTests        -o tests/<ProjectName>.UnitTests
dotnet new xunit -n <ProjectName>.IntegrationTests -o tests/<ProjectName>.IntegrationTests

# 3a. Configure IntegrationTests to use Microsoft.NET.Sdk.Web.
# ASP.NET Core integration tests use Microsoft.AspNetCore.Mvc.Testing /
# WebApplicationFactory and the integration test project must use the Web SDK.
# In tests/<ProjectName>.IntegrationTests/<ProjectName>.IntegrationTests.csproj,
# change the root project SDK to:
# <Project Sdk="Microsoft.NET.Sdk.Web">

# 3b. Remove template boilerplate generated by dotnet new
find src  -name "Class1.cs"    -delete   # classlib default stub
find tests -name "UnitTest1.cs" -delete  # xunit default stub
# Program.cs will be fully replaced with the canonical template in the next step.
# WeatherForecast content is embedded in Program.cs by dotnet new webapi;
# overwriting Program.cs (step 4) is sufficient — no separate file to delete.

# 3c. Central Package Management cleanup
# After Directory.Packages.props is created, remove Version attributes from all
# generated PackageReference entries. CPM requires versions to live only in
# Directory.Packages.props; project-level Version attributes will break restore.

# 4. Add all projects to solution
dotnet sln add src/**/*.csproj tests/**/*.csproj

# 5. Wire up project references
dotnet add src/<ProjectName>.Application/    reference src/<ProjectName>.Domain/
dotnet add src/<ProjectName>.Infrastructure/ reference src/<ProjectName>.Application/
dotnet add src/<ProjectName>.Persistence/    reference src/<ProjectName>.Application/
dotnet add src/<ProjectName>.Persistence/    reference src/<ProjectName>.Domain/
dotnet add src/<ProjectName>.WebApi/         reference src/<ProjectName>.Application/
dotnet add src/<ProjectName>.WebApi/         reference src/<ProjectName>.Infrastructure/
dotnet add src/<ProjectName>.WebApi/         reference src/<ProjectName>.Persistence/
dotnet add tests/<ProjectName>.UnitTests/         reference src/<ProjectName>.Domain/
dotnet add tests/<ProjectName>.UnitTests/         reference src/<ProjectName>.Application/
dotnet add tests/<ProjectName>.IntegrationTests/  reference src/<ProjectName>.WebApi/

# 5a. Add DI Abstractions to Application (Cqrs/CQRS.cs needs IServiceCollection / IServiceProvider for AddCqrs)
dotnet add src/<ProjectName>.Application/ package Microsoft.Extensions.DependencyInjection.Abstractions

# 6. Add EF Core to Persistence project, and EF Core Design to WebApi (required for dotnet-ef CLI)
dotnet add src/<ProjectName>.Persistence/ package Microsoft.EntityFrameworkCore
# Relational is REQUIRED in Persistence — ToTable/HasColumnName/HasDefaultValueSql/HasMaxLength
# and migrations live here, NOT in the base EntityFrameworkCore package.
dotnet add src/<ProjectName>.Persistence/ package Microsoft.EntityFrameworkCore.Relational
dotnet add src/<ProjectName>.WebApi/      package Microsoft.EntityFrameworkCore.Design

# 7. Add Dapper to Persistence project
dotnet add src/<ProjectName>.Persistence/ package Dapper

# 7b. Add FluentValidation to Application (validators) and the entry-point project (DI registration)
dotnet add src/<ProjectName>.Application/ package FluentValidation
dotnet add src/<ProjectName>.WebApi/      package FluentValidation.DependencyInjectionExtensions

# 7c. Add Serilog to the entry-point project
dotnet add src/<ProjectName>.WebApi/ package Serilog.AspNetCore
dotnet add src/<ProjectName>.WebApi/ package Serilog.Settings.Configuration
dotnet add src/<ProjectName>.WebApi/ package Serilog.Sinks.Console
dotnet add src/<ProjectName>.WebApi/ package Serilog.Sinks.File
dotnet add src/<ProjectName>.WebApi/ package Serilog.Enrichers.Environment
dotnet add src/<ProjectName>.WebApi/ package Serilog.Enrichers.Process
dotnet add src/<ProjectName>.WebApi/ package Serilog.Enrichers.Thread

# 7d. Add API documentation packages to the entry-point project
dotnet add src/<ProjectName>.WebApi/ package Microsoft.AspNetCore.OpenApi
dotnet add src/<ProjectName>.WebApi/ package Scalar.AspNetCore

# 7e. Add test dependencies
dotnet add tests/<ProjectName>.UnitTests/ package FluentAssertions
dotnet add tests/<ProjectName>.UnitTests/ package NSubstitute
dotnet add tests/<ProjectName>.IntegrationTests/ package FluentAssertions
dotnet add tests/<ProjectName>.IntegrationTests/ package NSubstitute
dotnet add tests/<ProjectName>.IntegrationTests/ package Microsoft.AspNetCore.Mvc.Testing

# 8. Add HTTP Client Resilience to Infrastructure project
#    (Infrastructure owns the AddHttpClient + AddStandardResilienceHandler registrations)
dotnet add src/<ProjectName>.Infrastructure/ package Microsoft.Extensions.Http.Resilience

# 9. OpenTelemetry — instrumentation is ALWAYS installed; the exporter is optional
#    dotnet add src/<ProjectName>.WebApi/ package OpenTelemetry.Extensions.Hosting
#    dotnet add src/<ProjectName>.WebApi/ package OpenTelemetry.Instrumentation.AspNetCore
#    dotnet add src/<ProjectName>.WebApi/ package OpenTelemetry.Instrumentation.Http
#    Then add ONE exporter based on user's choice:
#    OTLP   : dotnet add src/<ProjectName>.WebApi/ package OpenTelemetry.Exporter.OpenTelemetryProtocol
#    Console: dotnet add src/<ProjectName>.WebApi/ package OpenTelemetry.Exporter.Console
#    Azure  : dotnet add src/<ProjectName>.WebApi/ package Azure.Monitor.OpenTelemetry.AspNetCore

# 10. Add DB provider to BOTH WebApi (DI registration) and Persistence (migrations).
#     The provider MUST be in Persistence too: `dotnet ef` emits provider-specific
#     annotations into the Persistence project, so provider-in-WebApi-only fails CS0246.
#    SQL Server (default) : dotnet add src/<ProjectName>.WebApi/      package Microsoft.EntityFrameworkCore.SqlServer
#                           dotnet add src/<ProjectName>.Persistence/ package Microsoft.EntityFrameworkCore.SqlServer
#    PostgreSQL           : dotnet add src/<ProjectName>.WebApi/      package Npgsql.EntityFrameworkCore.PostgreSQL
#                           dotnet add src/<ProjectName>.Persistence/ package Npgsql.EntityFrameworkCore.PostgreSQL
```

> **DB Provider Selection**: Ask the user which database they are targeting before step 10. Add the chosen provider to **both** the WebApi project (owns DI registration) and the Persistence project (needed by `dotnet ef` migrations). Record the choice in `Directory.Packages.props` under a `<!-- DB Provider -->` comment.

> **EF Core Migrations**: The DB provider lives in both WebApi and Persistence (see step 10). `dotnet-ef` is installed as a **local tool** (committed via `.config/dotnet-tools.json`); run `dotnet tool restore` to install on a fresh clone. Always specify both projects when running migrations:
> ```bash
> dotnet ef migrations add <MigrationName> \
>   --project src/<ProjectName>.Persistence \
>   --startup-project src/<ProjectName>.WebApi
>
> dotnet ef database update \
>   --project src/<ProjectName>.Persistence \
>   --startup-project src/<ProjectName>.WebApi
> ```

---

## Central Package Management (CPM)

### `Directory.Build.props`
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
```

### `Directory.Packages.props`

> ⚠️ **Version Policy**: Do NOT hardcode version numbers here. At scaffold time, look up and use the **current latest stable** version for every package. Never use preview, beta, or RC releases.
> ⚠️ **CPM Policy**: Project `.csproj` files must not contain `Version="..."` on `<PackageReference>` entries. All package versions live only in this file.

```xml
<Project>
  <ItemGroup>
    <!-- Logging -->
    <PackageVersion Include="Serilog.AspNetCore"                             Version="LATEST_STABLE" />
    <PackageVersion Include="Serilog.Settings.Configuration"                 Version="LATEST_STABLE" />
    <PackageVersion Include="Serilog.Sinks.Console"                          Version="LATEST_STABLE" />
    <PackageVersion Include="Serilog.Sinks.File"                             Version="LATEST_STABLE" />
    <PackageVersion Include="Serilog.Enrichers.Environment"                  Version="LATEST_STABLE" />
    <PackageVersion Include="Serilog.Enrichers.Process"                      Version="LATEST_STABLE" />
    <PackageVersion Include="Serilog.Enrichers.Thread"                       Version="LATEST_STABLE" />

    <!-- HTTP Client Resilience: required for any outbound HTTP call to external APIs -->
    <PackageVersion Include="Microsoft.Extensions.Http.Resilience"           Version="LATEST_STABLE" />

    <!-- OpenTelemetry: instrumentation packages are always added; the exporter line only when one is chosen -->
    <!-- <PackageVersion Include="OpenTelemetry.Extensions.Hosting"           Version="LATEST_STABLE" /> -->
    <!-- <PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore"   Version="LATEST_STABLE" /> -->
    <!-- <PackageVersion Include="OpenTelemetry.Instrumentation.Http"         Version="LATEST_STABLE" /> -->
    <!-- Exporters: choose ONE based on user's target backend -->
    <!-- OTLP:          <PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="LATEST_STABLE" /> -->
    <!-- Console:       <PackageVersion Include="OpenTelemetry.Exporter.Console"               Version="LATEST_STABLE" /> -->
    <!-- Azure Monitor: <PackageVersion Include="Azure.Monitor.OpenTelemetry.AspNetCore"       Version="LATEST_STABLE" /> -->

    <!-- API Documentation: Microsoft OpenAPI + Scalar UI (NO Swashbuckle) -->
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi"                   Version="LATEST_STABLE" />
    <PackageVersion Include="Scalar.AspNetCore"                              Version="LATEST_STABLE" />

    <!-- Validation -->
    <PackageVersion Include="FluentValidation"                               Version="LATEST_STABLE" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="LATEST_STABLE" />

    <!-- Caching: HybridCache is NOT built into the shared framework. If caching is    -->
    <!-- required, add Microsoft.Extensions.Caching.Hybrid and call AddHybridCache().   -->
    <!-- <PackageVersion Include="Microsoft.Extensions.Caching.Hybrid" Version="LATEST_STABLE" /> -->

    <!-- Data Access: EF Core — Persistence needs base + Relational (config APIs + migrations) -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore"                  Version="LATEST_STABLE" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational"       Version="LATEST_STABLE" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design"           Version="LATEST_STABLE" />
    <!-- DB Provider: referenced by BOTH the entry-point project (owns DI registration) AND Persistence
         (dotnet ef emits provider-specific annotations there; omitting it fails with CS0246).
         Add the chosen provider after asking the user: -->
    <!-- <PackageVersion Include="Microsoft.EntityFrameworkCore.SqlServer"    Version="LATEST_STABLE" /> -->
    <!-- <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL"      Version="LATEST_STABLE" /> -->

    <!-- DI Abstractions: required by Application/Cqrs/CQRS.cs (IServiceCollection, IServiceProvider) -->
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="LATEST_STABLE" />

    <!-- Data Access: Dapper — complex/reporting queries in Persistence project -->
    <PackageVersion Include="Dapper"                                         Version="LATEST_STABLE" />

    <!-- Object Mapping: choose ONE during project setup (AutoMapper is FORBIDDEN)       -->
    <!-- Mappers live in Application/Mappers/ and are referenced from Application only   -->
    <!--                                                                                  -->
    <!-- Recommended: Mapperly (source-gen, zero runtime overhead, compile-time safe)    -->
    <!-- IMPORTANT: Mapperly is a source generator. When adding to Application.csproj,   -->
    <!--   use PrivateAssets="all" so it does not leak as a transitive runtime dep:       -->
    <!--   dotnet add src/<ProjectName>.Application/ package Riok.Mapperly                -->
    <!--   Then in Application.csproj, the PackageReference must have:                   -->
    <!--     <IncludeAssets>compile; runtime; build; native; contentfiles; analyzers</IncludeAssets> -->
    <!--     <PrivateAssets>all</PrivateAssets>                                           -->
    <!-- <PackageVersion Include="Riok.Mapperly" Version="LATEST_STABLE" />               -->
    <!--                                                                                  -->
    <!-- Alternative: Mapster (runtime mapper, flexible configuration)                   -->
    <!-- <PackageVersion Include="Mapster"       Version="LATEST_STABLE" />               -->

    <!-- Testing: xUnit + NSubstitute + FluentAssertions -->
    <PackageVersion Include="xunit"                                          Version="LATEST_STABLE" />
    <PackageVersion Include="xunit.runner.visualstudio"                      Version="LATEST_STABLE" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk"                         Version="LATEST_STABLE" />
    <!-- FluentAssertions 8.x is a PAID commercial (Xceed) licence — pin to latest 7.x (Apache-2.0) -->
    <PackageVersion Include="FluentAssertions"                               Version="LATEST_STABLE_7X" />
    <PackageVersion Include="NSubstitute"                                    Version="LATEST_STABLE" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing"               Version="LATEST_STABLE" />
  </ItemGroup>
</Project>
```

---

## WebApi Entry Point (`Program.cs`)

```csharp
using Serilog;
using Serilog.Events;
using Scalar.AspNetCore;

// Bootstrap logger: captures startup errors before full Serilog config loads.
// Replaced by ReadFrom.Configuration() once the host is built.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Logging — full config read from appsettings.json
    builder.Host.UseSerilog((ctx, services, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)   // allows ILogger sinks resolved from DI
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithProcessId()
        .Enrich.WithThreadId());

    // OpenAPI (Microsoft.AspNetCore.OpenApi + Scalar UI)
    builder.Services.AddOpenApi();

    // Caching: register HybridCache only if the project has confirmed caching needs.
    // builder.Services.AddHybridCache();

    // Validation (FluentValidation — validators defined in Application project)
    // builder.Services.AddValidatorsFromAssembly(typeof(SomeValidator).Assembly);
    // Note: endpoint request validation is invoked manually via IValidator<T>.ValidateAndThrowAsync().

    // TODO: builder.Services.AddCqrs(typeof(SomeHandler).Assembly);
    // TODO: builder.Services.AddDbContext<AppDbContext>(...);
    // TODO: builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(); // Scalar UI default path: /scalar/v1
    }

    // Serilog request logging — replaces default ASP.NET Core request logs with
    // a single structured log entry per request (includes status code, elapsed ms).
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseHttpsRedirection();

    // TODO: map endpoint groups here

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Exposes the top-level-statement entry point to the integration test project.
/// REQUIRED: `WebApplicationFactory<Program>` cannot see the compiler-generated
/// `Program` class otherwise — it is `internal`, and every integration test fails
/// to compile. Do not delete.
/// </summary>
public partial class Program;
```

---

## Endpoint Pattern

Each feature module must define a dedicated static class under `src/<ProjectName>.WebApi/Endpoints/` and mount routes via an extension method. Naming convention: `<Feature>Endpoint` class, `Map<Feature>Endpoints` method.

**Rules:**
- Class and method must both be `internal static`
- Route handler methods must be declared as `private static async Task<IResult>` with a named method (no inline lambdas)
- Every `Map*` call **must chain** `.WithName()`, `.WithSummary()`, `.WithDescription()`
- `Program.cs` only calls `app.Map<Feature>Endpoints()` — no handler logic inside

```csharp
internal static class ArticlesEndpoint
{
    internal static void MapArticlesEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/demo/articles", ListArticlesAsync)
           .WithName("ListDemoArticles")
           .WithSummary("List Demo Articles")
           .WithDescription("Retrieve all articles from SQL Server.");

        app.MapPost("/api/demo/articles", CreateArticleAsync)
           .WithName("CreateDemoArticle")
           .WithSummary("Create Demo Article")
           .WithDescription("Insert a new article into SQL Server.");
    }

    private static async Task<IResult> ListArticlesAsync(
        IQueryDispatcher queries,
        CancellationToken ct)
    {
        var result = await queries.DispatchAsync(new ListArticlesQuery(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateArticleAsync(
        CreateArticleRequest req,
        ICommandDispatcher commands,
        CancellationToken ct)
    {
        var result = await commands.DispatchAsync<CreateArticleResponse>(
            new CreateArticleCommand(req.Title, req.Content), ct);
        return Results.Created($"/api/demo/articles/{result.Id}", result);
    }
}
```

Route mounting section in `Program.cs`:
```csharp
// Map endpoints
app.MapArticlesEndpoints();
```

### FastEndpoints variant

When FastEndpoints is chosen (rank 2 in SKILL.md item 10), these conventions replace the ones above. Everything else — CQRS dispatch, validators living in `Application`, the layer boundaries — is unchanged.

**Rules:**
- One endpoint per class, named `<Verb><Feature>Endpoint` (`CreateSupplierEndpoint`), under `Endpoints/{Module}/`
- Derive from `Endpoint<TRequest, TResponse>`, or `EndpointWithoutRequest<TResponse>` when there is no request body
- `Configure()` declares the route, auth, and OpenAPI metadata; `ExecuteAsync()` dispatches to CQRS and does nothing else
- **`ExecuteAsync` returns `Task<TResponse>`** in FastEndpoints 8.x — return the response object directly. The older `Task` + `await SendOkAsync(...)` pattern found in many online samples does **not** compile against these generic base classes (CS0508)
- Request/response records may co-locate in the endpoint file (they serve that one class)

```csharp
internal sealed class CreateSupplierEndpoint(ICommandDispatcher commands)
    : Endpoint<CreateSupplierRequest, CreateSupplierResponse>
{
    public override void Configure()
    {
        Post("/api/v1/suppliers");
        Description(b => b
            .WithName("CreateSupplier")
            .WithSummary("Create a supplier")
            .WithDescription("Registers a supplier and returns its identifier."));
    }

    public override async Task<CreateSupplierResponse> ExecuteAsync(
        CreateSupplierRequest req, CancellationToken ct)
    {
        var id = await commands.DispatchAsync(new CreateSupplierCommand(req.TaxId, req.Name), ct);
        return new CreateSupplierResponse(id);
    }
}

internal sealed record CreateSupplierRequest(string TaxId, string Name);
internal sealed record CreateSupplierResponse(Guid Id);
```

For a non-200 outcome (a `Result<T>` failure, say), send explicitly instead of returning — `await Send.ResultAsync(...)` / `ThrowError(...)` — and keep the declared return type.

**Wiring in `Program.cs`:** `builder.Services.AddFastEndpoints();` and `app.UseFastEndpoints();` — endpoints are discovered by assembly scan, so there is no per-feature `Map*` call.

**Validation:** FastEndpoints auto-runs a `Validator<TRequest>` found in the *endpoint's own* assembly. Validators for commands/queries stay in `Application` per the standard rule; a request-shape validator may sit beside its endpoint. Do not duplicate the same rule in both places.

**Two things to get right:**

1. **Zero endpoints is a startup failure.** `AddFastEndpoints()` throws `InvalidOperationException: FastEndpoints was unable to find any endpoint declarations!` when the assembly declares none — the host never builds and every integration test fails with a misleading error. Minimal API has no such constraint. If the first scaffold has no business endpoints yet, ship a `/health` endpoint (which a K8s/Cloud Run deployment needs regardless) so the assembly is never empty.
2. **OpenAPI metadata comes from `Description(b => ...)`**, not from `.WithName()` chained on a `Map*` call. FastEndpoints also ships its own NSwag-based generator; do **not** enable it alongside `Microsoft.AspNetCore.OpenApi` — pick one document source. `AddOpenApi()` + `MapOpenApi()` with the metadata above is verified working (`/openapi/v1.json` serves correctly); richer cases (multiple response types, `ProducesProblem`, file upload) have not been verified and should be checked before relying on them.

---

## AI Guidance Files

Create AI guidance as two layers:

1. Root `AGENTS.md` — short index and mandatory reading policy.
2. `docs/agents/dotnet-rules.md` — full .NET development and testing rules.

Root `AGENTS.md` should stay concise. Do not paste the entire global `~/.agents/AGENTS.md` or the full `.NET` rules into it. Include only:

- Language rule: all AI responses use Traditional Chinese.
- Security and documentation alignment reminders.
- Brownfield safety: scan before editing; no unsolicited refactors.
- Package manager rule: `dotnet` for .NET, `pnpm` for frontend assets.
- Git safety: never run `git commit` without explicit user approval.
- Required references: before .NET/backend/API/database/test changes, read `docs/agents/dotnet-rules.md`.

Recommended root `AGENTS.md` shape:

```markdown
# Project Agent Instructions

All AI responses must be in Traditional Chinese (正體中文).

## Required References

Before .NET/backend/API/database/test changes, read:

- docs/agents/dotnet-rules.md

Also read local docs/specs before changing contracts:

- docs/
- README.md

## Operating Rules

- Follow existing project patterns before introducing new structure.
- Do not change API contracts without updating documentation first.
- Do not hardcode secrets.
- Use `dotnet` for .NET packages and `pnpm` for frontend assets.
- Never run `git commit` without explicit user approval.
- Before completion, run `dotnet build` and `dotnet test`.
```

Copy the full contents of `references/dotnet-rules.md` into `docs/agents/dotnet-rules.md`. The rules file must be self-contained and must not reference `~/.agents/` or this skill path.

---

## Domain Templates

Refer to `references/dotnet-domain-template.cs` (same directory as this file) for canonical implementations. **Which building blocks to scaffold depends on the chosen domain model style.**

**Rich Domain Model** — scaffold all of the following:
- One named `IXxxRepository` per aggregate in `Domain/Interfaces/` (see the `IOrderRepository` example) — **no generic `IRepository<T>`**
- `IUnitOfWork` — persistence-agnostic `CommitAsync(CancellationToken)` contract in `Domain/Interfaces/`
- `IDomainEventDispatcher` in `Domain/Interfaces/` (if Domain Events enabled)
- `IDomainEventHandler<TEvent>` in `Domain/Interfaces/` (if Domain Events enabled)
- `IDomainEvent` / `DomainEvent` base record (`Domain/Events/`)
- `AggregateRoot<TId>` with `DomainEvents` collection and `ClearDomainEvents()` (`Domain/Entities/`)
- `Entity<TId>` for non-root child entities (`Domain/Entities/`)
- `ValueObject` abstract base — prefer `record` types for simple value objects (`Domain/ValueObjects/`)
- `Result<T>` and `Result` for domain operation outcomes (`Domain/Common/`) — if Result Pattern enabled
- Example Aggregate Root with factory method, behaviour methods, and domain events
- Rich Domain Services, if scaffolded, are pure domain logic and do not inject repositories. Application handlers load required aggregates and pass them into domain methods/services.

**Anemic Domain Model** — scaffold only:
- One named `IXxxRepository` per entity in `Domain/Interfaces/`, typed against the plain POCO — **no generic `IRepository<T>`**, no base class or generic constraint
- `IUnitOfWork` — same persistence-agnostic `CommitAsync(CancellationToken)` contract in `Domain/Interfaces/`
- Concrete POCO entities in `Domain/Entities/` with no base class
- Capability-named Domain Services in `Domain/DomainServices/` (only where shared logic exists) — receive only Domain-defined interfaces such as an `IXxxRepository`, custom domain capability interfaces, and optionally `IDomainEventDispatcher` via primary constructor injection
- `Result<T>` and `Result` in `Domain/Common/` — if Result Pattern enabled
- If Domain Events enabled: also scaffold `IDomainEvent`, `DomainEvent` (`Domain/Events/`), `IDomainEventDispatcher`, `IDomainEventHandler<TEvent>` (`Domain/Interfaces/`), and `DomainEventDispatcher` in `Infrastructure/`
- **Do NOT** scaffold `AggregateRoot<TId>`, `Entity<TId>`, or `ValueObject`

**Domain Events dispatch flow — Rich Domain Model (automatic):**
1. Aggregate raises event via `AddDomainEvent(new SomethingHappenedEvent(...))` inside a behaviour method.
2. Command handler calls `await unitOfWork.CommitAsync(ct)` to commit the use case.
3. `IUnitOfWork` implementation collects pending `DomainEvents`, commits persistence first, then calls `IDomainEventDispatcher.DispatchAsync(events, ct)` after the transaction succeeds.
4. `DomainEventDispatcher` (Infrastructure) resolves `IDomainEventHandler<TEvent>` instances from DI and calls `HandleAsync` for each.
5. `IUnitOfWork` clears event queues after successful dispatch.
6. Application-layer event handlers in `Application/EventHandlers/` implement `IDomainEventHandler<TEvent>` and execute only local in-process side effects.

In-process domain event handlers must not perform irreversible or reliability-critical side effects directly. For simple workflows, perform external calls through explicit Application orchestration. If asynchronous processing is required, use retryable background work with idempotency.

**Domain Events dispatch flow — Anemic Domain Model (manual):**
1. The Application handler, or a Domain Service it calls, injects `IDomainEventDispatcher` via primary constructor.
2. After completing repository operations and after the use case commit succeeds, the handler or Domain Service explicitly calls `await dispatcher.DispatchAsync(new[] { new SomethingHappenedEvent(...) }, ct)` for in-process side effects only.
3. `DomainEventDispatcher` (Infrastructure) resolves and invokes `IDomainEventHandler<TEvent>` instances from DI.
4. Application-layer event handlers execute side effects.

`IDomainEventDispatcher` and `IDomainEventHandler<TEvent>` interfaces live in `Domain/Interfaces/`. The `DomainEventDispatcher` implementation lives in `Infrastructure/`. Register via assembly scanning (see `references/dotnet-domain-template.cs`).

---

## CQRS Pattern

Follow the template in `references/dotnet-cqrs-template.cs` (same directory as this file). Place the generated code in `src/<ProjectName>.Application/Cqrs/CQRS.cs` (namespace `<ProjectName>.Application.Cqrs`) — a single file inside the Application project, not a standalone project. Business commands/queries/handlers go in Application's `Commands/`/`Queries/`/`EventHandlers/` folders (namespace `<ProjectName>.Application.*`).

> **The normative CQRS rules — command/query DB routing, the persistence-agnostic `IUnitOfWork` contract, EF+Dapper transaction sharing, and the `AppUnitOfWork` scoping rule — live in `dotnet-rules.md` → CQRS Implementation & Data Access. Do not restate them; this section only covers the mechanical wiring.**

**File layout — one file per use case.** A command/query lives in the same file as its handler, named after the message. This is the exception to one-class-per-file spelled out in `dotnet-rules.md` → One Class Per File:

```
Application/Modules/Supplier/
├─ Commands/
│  ├─ RegisterSupplierCommand.cs      # RegisterSupplierCommand + RegisterSupplierCommandHandler
│  ├─ ApproveSupplierCommand.cs       # ApproveSupplierCommand  + ApproveSupplierCommandHandler
│  └─ SetSupplierActivationCommand.cs
└─ Queries/
   ├─ GetSupplierListQuery.cs         # GetSupplierListQuery    + GetSupplierListQueryHandler
   └─ GetSupplierByIdQuery.cs
```

```csharp
// Commands/ApproveSupplierCommand.cs — the whole use case, readable top to bottom.
public sealed record ApproveSupplierCommand(Guid SupplierId, string ApprovedBy) : ICommand<Result>;

public sealed class ApproveSupplierCommandHandler(
    ISupplierRepository suppliers,
    IUnitOfWork unitOfWork,
    IDomainEventDispatcher dispatcher,
    TimeProvider timeProvider)
    : ICommandHandler<ApproveSupplierCommand, Result>
{
    public async Task<Result> HandleAsync(ApproveSupplierCommand command, CancellationToken ct = default)
    {
        // …
    }
}
```

The contract and the logic that honours it change together, so keeping them together removes a navigation step from every edit — and a file that grows uncomfortable is telling you the *use case* is too big, which is a more useful signal than a long folder listing. Do not extend this to unrelated types: two commands never share a file, and a module-wide `XxxDtos.cs` is still a one-class-per-file violation.

Mechanics specific to scaffolding:

- Register handlers with `services.AddCqrs(typeof(SomeHandler).Assembly)` — assembly scanning only, never line-by-line. No MediatR, no in-memory event buses.
- All DI classes use **.NET 10 Primary Constructors** — no manual `private readonly` fields.
- `IUnitOfWork` and the per-aggregate `IXxxRepository` interfaces are defined in `Domain/Interfaces/` and implemented in `Persistence/UnitOfWork/` and `Persistence/Repositories/`. `IXxxReadRepository` is defined in `Application/Interfaces/` and implemented in `Persistence/ReadRepositories/`.
- **Persistence abstractions are identical in both topologies.** Handlers never inject a `DbContext` — `Application` references `Domain` only, so a concrete context is not even reachable from a handler. Two abstractions exist regardless of how many databases are deployed:
  - `IXxxRepository` — **write side**, one named interface per aggregate in `Domain/Interfaces/`, returns tracked aggregate roots, used by CommandHandlers and Domain Services. There is no generic `IRepository<T>`.
  - `IXxxReadRepository` — **read side**, defined in `Application/Interfaces/`, returns DTOs/projections, used by QueryHandlers. It lives in `Application` rather than `Domain` because it returns DTOs, which are an Application concept; `Domain` must not know about them.

  Both are implemented in `Persistence` (`Repositories/` and `ReadRepositories/`). This keeps the topology a **deployment decision, not an architectural one**: moving between one database and two changes DI registration and `appsettings` only — no handler, no interface, and no query implementation changes. That matters in practice, because projects routinely start on one database and add a replica later, or provision two and collapse back to one on cost.

- **Single context, two connection strings** (default): one `AppDbContext` bound to `ConnectionStrings:Write`. Both `IXxxRepository` and `IXxxReadRepository` are implemented against it; read-side implementations still use `AsNoTracking()`. `ConnectionStrings:Read` exists in config from the start and holds the same value, but nothing reads it yet — do not bind a second context to it just because the key is there (two contexts over one database is drift waiting to happen; see SKILL.md item 4).

- **Read replica added** (`--read-replicas`, an upgrade from the default): two contexts sharing one model by inheritance, registered against different connections. The read-side implementations switch to `ReadDbContext`; their query bodies are unchanged. Take this step only when a replica genuinely exists — never to "prepare" for one.

  Derive both from a common abstract base so `OnModelCreating` and the entity configurations are written **once** — two independently configured contexts over the same tables drift, and a missed change on one side produces behaviour differences that are hard to trace.

  ```csharp
  // Persistence/AppDbContext.cs — shared model, never registered in DI directly.
  public abstract class AppDbContext(DbContextOptions options) : DbContext(options)
  {
      protected override void OnModelCreating(ModelBuilder modelBuilder)
          => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
  }

  // Persistence/WriteDbContext.cs — the only context that may write.
  public sealed class WriteDbContext(DbContextOptions<WriteDbContext> options)
      : AppDbContext(options);

  // Persistence/ReadDbContext.cs — read-only by construction, not by convention.
  public sealed class ReadDbContext : AppDbContext
  {
      public ReadDbContext(DbContextOptions<ReadDbContext> options) : base(options)
          => ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

      public override int SaveChanges()
          => throw new InvalidOperationException("ReadDbContext is read-only.");

      public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
          => throw new InvalidOperationException("ReadDbContext is read-only.");
  }
  ```

  The base **must** take the non-generic `DbContextOptions`. `DbContextOptions<T>` is invariant, so a derived context declaring `DbContextOptions<ReadDbContext>` cannot pass it to a base expecting `DbContextOptions<WriteDbContext>` — that is a compile error, not a runtime one. Each concrete context keeps its own generic options type so DI can bind them to different connections.

  `ReadDbContext` defaults to `NoTracking`, so a forgotten `AsNoTracking()` is not a bug, and `SaveChanges` on the read side throws instead of silently attempting a write against a replica.

  ```csharp
  builder.Services.AddDbContext<WriteDbContext>(options =>
      options.UseSqlServer(builder.Configuration.GetConnectionString("Write")));

  builder.Services.AddDbContext<ReadDbContext>(options =>
      options.UseSqlServer(builder.Configuration.GetConnectionString("Read")));
  ```
  Replace `UseSqlServer` with the chosen provider (`UseNpgsql` for PostgreSQL).

  **Do not add `DbSet`s to `ReadDbContext` or override its `OnModelCreating`.** Both contexts must expose exactly the same model; once they diverge EF treats them as different schemas and migration comparison breaks. Shape query results with `.Select()` projections or keyless entity types instead of altering the inherited model.

  **Which connection a handler uses** is normative and lives in `dotnet-rules.md` → CQRS Implementation → *Write vs Read routing*. Scaffold only the mechanism it depends on:

  - The **only** difference between the two topologies is which context the read-side implementation takes. The query body is identical:
    ```csharp
    // Persistence/ReadRepositories/OrderReadRepository.cs
    // Default (one context):  (AppDbContext db)
    // With a read replica:    (ReadDbContext db)   <-- the entire change
    public sealed class OrderReadRepository(ReadDbContext db) : IOrderReadRepository
    {
        public Task<IReadOnlyList<OrderListItemDto>> GetPagedAsync(...) =>
            db.Orders.AsNoTracking()
              .Select(o => new OrderListItemDto(o.Id, o.Code, o.Total))
              .ToListAsync(cancellationToken);
    }
    ```
  - **Strong-consistency queries** (the five cases in `dotnet-rules.md`) must not reach for `WriteDbContext` from the handler — `Application` cannot see it. Register a second implementation of the same read interface bound to the write context and resolve it by key, so the choice stays in `Persistence` and the handler still depends only on an Application-layer abstraction:
    ```csharp
    // Persistence — same query code, write connection.
    public sealed class OrderStrongReadRepository(WriteDbContext db) : IOrderReadRepository { /* ... */ }

    services.AddScoped<IOrderReadRepository, OrderReadRepository>();
    services.AddKeyedScoped<IOrderReadRepository, OrderStrongReadRepository>("strong");

    // Application — the case is named at the injection point.
    // Strong consistency, case (1): backs the detail page the user is redirected
    // to immediately after CreateOrderCommand — must not read a lagging replica.
    public sealed class GetOrderByIdQueryHandler(
        [FromKeyedServices("strong")] IOrderReadRepository orders)
        : IQueryHandler<GetOrderByIdQuery, Result<OrderDto>> { /* ... */ }
    ```
    Under the default single context the keyed registration points at that same context, so handlers written this way need no change when a replica is added later.
  - EF Core migrations and the design-time factory bind to the **Write** connection explicitly, so `dotnet ef` never issues DDL against a replica. With two contexts present `dotnet ef` can no longer infer which one to use, so migration commands must name it — `dotnet ef migrations add <Name> --context WriteDbContext` — and only `WriteDbContext` gets a design-time factory:
    ```csharp
    public sealed class WriteDbContextFactory : IDesignTimeDbContextFactory<WriteDbContext>
    {
        public WriteDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var options = new DbContextOptionsBuilder<WriteDbContext>()
                .UseSqlServer(configuration.GetConnectionString("Write"))
                .Options;

            return new WriteDbContext(options);
        }
    }
    ```
  - Health checks probe **both** connections separately; a healthy write connection says nothing about the replica.
  - `IUnitOfWork` and every write-side repository take `WriteDbContext`; `ReadDbContext` is taken only by read-side repository implementations. Both stay inside `Persistence`.
  - A transaction never spans `WriteDbContext` and `ReadDbContext`. `IUnitOfWork` owns the write context only.

---

## Environment & Configuration

`appsettings.json` (production defaults):

```json
{
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Console",
      "Serilog.Sinks.File"
    ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithProcessId", "WithThreadId" ]
  },
  "ConnectionStrings": {
    "Write": "",
    "Read": ""
  }
}
```

`Write` and `Read` are always both present, whether or not a replica exists — see SKILL.md item 4. With no replica they hold the same value and only `Write` is read by the code, so adding a replica later is a config change rather than a rename across every environment and secret store.

`appsettings.Development.json` (full file — overrides production defaults):

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft.EntityFrameworkCore.Database.Command": "Information"
      }
    }
  },
  "ConnectionStrings": {
    "Write": "",
    "Read": ""
  }
}
```

In CI/production, inject `ConnectionStrings__Write` and `ConnectionStrings__Read` as environment variables — both, even without a replica, where they carry the same value. Never commit real connection strings.

If automated auth tests require test backdoors, add a non-production-only `Testing` config section (for example `FixedOtp` or `EnableTestUserHeader`) and middleware that rejects those bypasses outside Development/Test environments. Do not scaffold this by default.

---

## Testing Setup

> Full testing policy (frameworks, coverage standard, excluded projects) lives in `dotnet-rules.md` → Testing Guidelines. Scaffold-specific wiring only:

- Two projects only: `tests/<ProjectName>.UnitTests/` and `tests/<ProjectName>.IntegrationTests/`.
- The **IntegrationTests** project file must use `Microsoft.NET.Sdk.Web` (`<Project Sdk="Microsoft.NET.Sdk.Web">`) so `WebApplicationFactory` resolves the ASP.NET Core host — this is the one setup detail that is easy to miss.
- Packages per the scaffold command list above (xUnit + NSubstitute + FluentAssertions; IntegrationTests also gets `Microsoft.AspNetCore.Mvc.Testing`).
- `Program.cs` must end with `public partial class Program;` (see the entry point template) or `WebApplicationFactory<Program>` will not compile.

### WebApplicationFactory — two traps that cost real debugging time

**1. Inject test configuration with `UseSetting`, not `ConfigureAppConfiguration`.**

Secrets are empty strings in the committed `appsettings.*.json` by policy, and `ConfigureAppConfiguration` sources are overridden by the application's own `appsettings.{Environment}.json` — so the app reads the empty value back. Nothing fails at startup: DI resolves, the host builds, and every DI-level test passes. Only actual HTTP requests return 500, from deep inside a component that received an empty key (for example `IDX10703: ... key length is zero` from JWT). The symptom is far from the cause.

`UseSetting` takes precedence over file-based sources:

```csharp
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private const string TestConnectionString = "Host=localhost;Database=app_test;Username=postgres;Password=postgres";
    private const string TestSigningKey = "integration-test-signing-key-value-at-least-32-chars";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        // NOT ConfigureAppConfiguration — appsettings.Development.json would win.
        builder.UseSetting("ConnectionStrings:Write", TestConnectionString);
        builder.UseSetting("ConnectionStrings:Read", TestConnectionString);
        builder.UseSetting("Jwt:SigningKey", TestSigningKey);
    }
}
```

EF Core does not connect at startup, so a syntactically valid connection string is enough for host-composition tests; tests that actually read or write need a real database (Testcontainers or a local instance).

**2. Resolving `IUnitOfWork` in a test requires `CreateAsyncScope()`.**

`IUnitOfWork : IAsyncDisposable`, and disposing a *synchronous* scope that holds an `IAsyncDisposable`-only service throws:

> `InvalidOperationException: '...UnitOfWork' type only implements IAsyncDisposable. Use DisposeAsync to dispose the container.`

In production the container handles scope disposal, so this only surfaces where scopes are created by hand — integration tests and background/batch work:

```csharp
await using var scope = factory.Services.CreateAsyncScope();   // not CreateScope()
var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
```

The test method must then be `async Task`, not `void`.
