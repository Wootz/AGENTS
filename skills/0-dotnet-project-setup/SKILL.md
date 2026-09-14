---
name: 0-dotnet-project-setup
description: Scaffold a complete .NET backend project from scratch using the repository's .NET conventions for Clean Architecture, CQRS (with an Anemic or DDD Rich domain model), Central Package Management, package selection, testing, and validation. Use this skill whenever the user wants to create or initialize a new .NET solution, ASP.NET Core API, C# backend service, Web API, microservice, or backend project skeleton, even if they only say "new API", "set up backend", "create .NET project", "scaffold service", or similar. This skill is only for new .NET backend codebases, not routine feature work in an existing app.
---

# .NET Project Setup

This skill walks through scaffolding a production-ready .NET backend project following **DDD (Domain-Driven Design) + Clean Architecture + CQRS**, complying with the conventions in `AGENTS.md` and `dotnet-rules.md`.

> **Language**: All questions asked to the user and all responses **must be in Traditional Chinese (正體中文)**. Code, file paths, and technical identifiers remain in English.

## Step 0: Inspect Local Context

Before asking questions or writing files:

1. Check whether the target directory already exists.
2. If it exists, scan local docs first (`docs/`, `doc/`, `architecture/`, `specs/`, `README.md`, `AGENTS.md`) and read any files that define architecture, API contracts, DB conventions, or UI/API naming.
3. If local documentation conflicts with this skill or `references/`, **the user decides — always ask.** Neither side wins by default: not the docs, not this skill. Report **all** conflicts at once as a table of *item / what the docs say / what this skill says / consequence*, then ask a single question and wait:

   > "本地文件與 skill 預設有 N 處衝突。以何者為準？"
   > (a) **以文件為準** — skill 只補不衝突的部分
   > (b) **以 skill 為準** — 先更新文件，再依 skill 建置
   > (c) **逐項裁示**

   Present the options neutrally — do not mark one as recommended or likely-correct. Do not resolve any conflict on your own judgement, and do not treat a documented architectural decision as a defect to correct. Ask once with everything in it rather than walking the user through conflicts one at a time across several turns; that is what makes this step expensive. **Write nothing until the answer arrives.**
4. If the target is a truly new empty directory with no local docs, proceed with the reference files in this skill.

## Step 1: Capture Intent

Before writing any files, collect the following. Ask all questions upfront in a single message — do not ask one at a time.

### Required Before Scaffolding

Do not run scaffold commands until these are confirmed:

**0. Project purpose & goals** — If the user has not already described the system's purpose, ask explicitly before proceeding:
   - *"What is this system for? Who are the users? What are the core business workflows?"*
   - This is essential for giving accurate architectural recommendations. Without it, defaulting to generic advice risks over- or under-engineering the solution.
   - Use the answer to calibrate recommendations for items 6–12 below.

1. **Project name / namespace root** — e.g. `MyApp`, `Acme.OrderService`
2. **Database provider** — **SQL Server (preferred default)**, then **PostgreSQL**. Ask before adding provider packages. Do not use SQLite (not used for development here).
3. **Database timezone** — ⚠️ **MANDATORY — ask in Step 1, not later.** Ask explicitly: *"What timezone should the database use? (e.g., UTC, Asia/Taipei)"* It is not only a schema concern: it fixes the API's time contract (what `createdAt` in a response means) and therefore belongs in the README before any endpoint or document is written. A project that writes docs first and code later will otherwise reach migrations with the question still open.
4. **Database topology** — **do not ask; always scaffold `ConnectionStrings:Write` + `ConnectionStrings:Read` with a single `DbContext`.** There is no `ConnectionStrings:Default`. When there is no replica — the normal case — both keys hold the **same value** and only `Write` is read by the code; `Read` is a declared-but-unused key.

   This is the default because it costs nothing now and removes the expensive part of adding a replica later. The `appsettings` shape, the deployment secrets, and the env-var names (`ConnectionStrings__Write` / `__Read`) are correct from day one, so introducing a replica is a DI change plus a config value — not a rename that touches every environment, pipeline, and secret store.

   Do **not** scaffold two `DbContext`s to "prepare" for a replica. Two contexts over one database is a fake separation: it doubles the model configuration surface and invites drift while delivering no isolation. One context stays honest about what is actually deployed.

   - **Upgrading to a real replica** (`--read-replicas`): add a `ReadDbContext` bound to `Read`, move the read-repository implementations onto it, and point migrations and the design-time factory at `Write`. Handlers, interfaces, and query bodies do not change — that is what the always-present read/write repository split buys. Details in `references/dotnet-scaffold.md` → CQRS Pattern.
   - State in the README which shape is live, and that `Read` is currently unused when it is.
5. **Features / bounded contexts** — what domains does this service handle? (Helps decide initial module/aggregate names.)

5a. **Directory grouping** — derive it from the answer to item 5 and confirm; do not ask blind:
   - **By technical type** *(default)* — `Application/Commands/`, `Queries/`, `Domain/Entities/`… Right for one or a few feature areas.
   - **By module (vertical slice)** — `Application/Modules/{Module}/Commands/`… Right once the service has many distinct business modules (roughly 5+, or a requirements doc that already enumerates them). At that size a flat `Commands/` folder accumulates hundreds of files and every change means hopping between five technical folders.
   - Modules follow **aggregate boundaries**, not tables, or the module count explodes.
   - `Persistence` and `Infrastructure` are **never** split by module — see `references/dotnet-scaffold.md` → Directory Structure for the layout and why.

### Architecture Choices

Ask these in the same upfront message and include the recommended default. If the user says "use sensible defaults", apply the recommendations below and state them before scaffolding.

6. **Domain model style** — ask which of the two to use. Both are established architectures with published reference implementations; state the recommendation and the reference so the user is choosing between known options, not a house style:
   - ✅ **Anemic Domain Model** *(Default — recommended unless the domain has invariants that are genuinely hard to keep correct)* — Entities are plain data holders. Business logic lives in Application handlers; genuinely shared domain logic moves to capability-named Domain Services. This is **Clean Architecture / Onion Architecture**, as in Microsoft's **eShopOnWeb**. Call it Clean Architecture + CQRS — **not DDD**; it uses DDD's layering and vocabulary without the Rich Domain Model.
   - **Rich Domain Model** *(Choose when the domain has complex invariants to enforce or multiple aggregates that interact)* — Entities encapsulate behaviour and enforce invariants (`Order.AddItem()`, `Order.Cancel()`). This is **DDD** proper, as in Microsoft's **eShopOnContainers** Ordering service. Keeps the Application layer thin, at the cost of modelling effort the team has to sustain.
   - ⚠️ **Do not mix them.** Rich-looking entities whose rules actually live in handlers is neither architecture and has no reference implementation to check against. Whichever is chosen, apply it consistently.
   - ⚠️ **Anemic's known failure mode is rule scatter.** With no entity to own an invariant, the same rule gets re-implemented in each handler that needs it, and they drift. Say this when recommending Anemic, along with the two countermeasures: (a) every state transition goes through **one** entry point — never let a second handler assign the status field directly; (b) the moment a rule is needed by a second handler, extract it to a capability-named Domain Service (item 6a) rather than copying it. Neither is enforced by the compiler, so they belong in the project's `AGENTS.md`/review checklist.
   - The choice drives concrete enforcement at scaffold time (Rich: private setters + factory methods, no public parameterless ctors; Anemic: POCOs, plus capability-named Domain Services **only where shared domain logic actually exists** — see item 6a). Those details are applied in **Step 3** and specified in `references/dotnet-rules.md` → **Domain Model Style / DDD Building Blocks**.

6a. **Domain Services (Anemic only)** — **do not scaffold one per entity.** A Domain Service is justified only when logic passes both tests: (1) it is a **business rule** — not HTTP, persistence, or formatting, which belong in Infrastructure or stay in the handler; and (2) it is **shared by more than one handler, or complex enough to obscure the handler**. Business logic living directly in an Application handler is normal and correct for the Anemic model — **the handlers are the service layer**. Default to none and extract one when a second handler needs the same rule; the boundary is clearer then than it is up front.
   - **Name by capability, never by entity, and never with a `Manager` suffix**: `PricingService`, `CreditCheckService` — not `OrderManager`, not `OrderService`. Rationale and the full rule are in `references/dotnet-rules.md` → Domain Model Style.
   - **For teams coming from a three-tier `API → Manager → Repository` design**: the logic that used to sit in a Manager method moves *into the handler*, one handler per method — it is not re-created as a Domain Service that the handler forwards to. A handler whose body is a single call into a domain service has kept the old architecture and added boilerplate. Say this explicitly when scaffolding for such a team.
7. **Domain Events** — ask whether to enable Domain Events support. Applies to **both** domain model styles, but the dispatch mechanism differs:
   - ✅ **Recommended for Rich Domain Model** — aggregates queue events internally via `AddDomainEvent()`; `IUnitOfWork.CommitAsync()` commits persistence first, then dispatches domain events after the transaction succeeds. Side effects are fully decoupled from the command handler.
   - ✅ **Also supported for Anemic Domain Model** — the Application handler (or a Domain Service it calls) dispatches events explicitly after the use case commit succeeds. No `AggregateRoot` is needed. Recommended when the Anemic model still has meaningful state transitions that require decoupled in-process side effects (emails, audit logs, projections).
   - ⚠️ **Skip if the domain has no meaningful state transitions that trigger side effects** — unnecessary complexity for simple CRUD flows.
   - If enabled: see Step 3 for the full list of files to scaffold.
8. **Result Pattern** — **do not ask; always enabled.** Domain operations return `Result<T>` for expected business failures instead of throwing. This applies to Anemic and CRUD services too — "record not found", "email already taken", and "insufficient stock" are expected outcomes, and exceptions are for the unexpected. Scaffold `Domain/Common/Result.cs` from `references/dotnet-domain-template.cs`.
   - Infrastructure faults (DB unreachable, serialization errors) still throw — `Result` is for business outcomes only.
   - **Who returns it depends on the domain model.** Rich: aggregate methods and Domain Services (`order.Cancel()` → `Result`). Anemic: the entities are POCOs with no methods, so the **command/query handlers** return it, plus any Domain Service that exists. `Result<T>` still lives in `Domain/Common/` under both — it is a domain vocabulary type, not a persistence or transport concern, and the endpoint layer translates it (typically to RFC 7807 Problem Details).
   - Listed in the confirmation summary (Step 1.5) so the user can opt out there.
9. **Event-Driven Design (EDD / Integration Events)** — ask whether the system needs event-driven communication between services or bounded contexts. Calibrate the recommendation based on the project nature described in item 0:
   - ✅ **Recommend enabling when**: microservices architecture; integration with external platforms (payment gateways, notification services, logistics); async workflows where services must react to each other's state changes; high-decoupling requirements between bounded contexts.
   - ❌ **Recommend skipping when**: simple monolith or internal tool with no inter-service communication; all business flows are synchronous and self-contained; team lacks message broker operational experience and the project doesn't justify the overhead.
   - Compatible with **both** Rich and Anemic Domain Models — Integration Events are an Application/Infrastructure concern, not a domain modelling concern.
   - If enabled: scaffold only the minimal event contracts needed by the confirmed use case, such as `Application/IntegrationEvents/` and `Application/Interfaces/IEventBus.cs`, plus a broker-specific implementation in `Infrastructure/`. Ask which message broker to target (RabbitMQ, Azure Service Bus, Kafka, etc.).
10. **Endpoint framework** — ordered preference, not a fixed default. Recommend the highest-ranked option that fits and state why:

   | Rank | Option | When |
   |------|--------|------|
   | 1 | **Minimal API route groups** *(default)* | Normal case — endpoint count is manageable |
   | 2 | **FastEndpoints** | Many endpoints whose rules differ materially (one resource routed through different approval flows by type), where framework-enforced one-endpoint-one-class isolation is worth the dependency |
   | 3 | **MVC Controller** | Only to match an existing solution, or a real MVC dependency (View rendering) |

   Never propose Controllers yourself — accept them only to match an existing codebase. Whichever is chosen, the conventions in `references/dotnet-scaffold.md` → Endpoint Pattern apply; it covers Minimal API and FastEndpoints.
11. **Object mapper** — **do not ask; always Mapperly** (`--mapper mapperly`). Source generator: zero runtime overhead, compile-time type safety, mapping errors caught at build time. Switch to **Mapster** or none only if the user asks; it is offered in the Step 1.5 summary.
   - 🚫 **AutoMapper is strictly forbidden** regardless of user preference.
12. **OpenTelemetry** — **do not ask whether to install it; instrumentation is always scaffolded** (`OpenTelemetry.Extensions.Hosting`, `.Instrumentation.AspNetCore`, `.Instrumentation.Http`). With no exporter configured nothing is emitted and development is unaffected, so the cost of having it is near zero while the cost of retrofitting it is a `Program.cs` rewrite.
   - **Exporter defaults to none** (`--otel none`). Ask which exporter to wire up (OTLP, Console, Azure Monitor) **only** when the user has described an observability backend or a production/microservice deployment; otherwise leave it unset — turning it on later is a configuration change, not a code change.
   - A project that genuinely wants no OpenTelemetry at all deletes the three packages and the `AddOpenTelemetry()` block; that is cheaper than wiring them in afterwards.
13. **Auth mechanism** — JWT, API Key, none?

## Step 1.5: Confirm Before Scaffolding

⚠️ **Mandatory. Do not run `scripts/scaffold.sh` until the user has replied to this summary.**

Several items above are applied without being asked (Result Pattern, Minimal API, Mapperly, OpenTelemetry instrumentation). Silent defaults are only acceptable if the user gets one clear chance to reverse them, so present everything that was decided — asked *and* defaulted — as a single compact summary and wait for confirmation.

Render it as a table with a **Source** column so defaults are visually distinct from the user's own answers:

```
| Setting            | Value                    | Source    |
|--------------------|--------------------------|-----------|
| Project name       | Acme.OrderService        | you       |
| DB provider        | SQL Server               | you       |
| DB timezone        | Asia/Taipei              | you       |
| DB topology        | Write/Read strings, one DbContext | default |
| Domain model       | Anemic (Clean Arch + CQRS) | default |
| Domain Events      | Enabled                  | you       |
| Result Pattern     | Enabled                  | default   |
| EDD                | Skipped                  | you       |
| Endpoints          | Minimal API route groups | default   |
| Object mapper      | Mapperly                 | default   |
| OpenTelemetry      | Instrumentation, no exporter | default |
| Auth               | JWT                      | you       |
```

Then ask plainly: *"Scaffold with these settings, or change any of them first?"*

- Anything marked `default` is changeable — say so explicitly, and name the alternatives for the silent defaults (Rich Domain Model / DDD; Result Pattern off; FastEndpoints; Mapster or no mapper; an OTel exporter, or removing OpenTelemetry entirely).
- If the user changes something, re-render the table with the change applied and confirm again.
- "Use sensible defaults" earlier in the conversation does **not** skip this step — it fills the table in, and the table is still shown.

---

## Defaults for "Use Sensible Defaults"

When the user explicitly authorizes defaults, use:

- Anemic Domain Model (Clean Architecture + CQRS, eShopOnWeb style) unless the user describes invariants complex enough to justify Rich.
- Domain Events enabled for Rich Domain Model; skipped for Anemic CRUD unless side effects exist.
- No Domain Services scaffolded up front under Anemic — added only when shared domain logic appears, named by capability.
- Result Pattern always enabled (not a defaults-only choice — see item 8).
- EDD skipped unless there is cross-service or asynchronous integration.
- `ConnectionStrings:Write` + `ConnectionStrings:Read` with a single `DbContext` bound to `Write` (item 4) — not a user choice; two `DbContext`s only when a replica actually exists. CommandHandlers use `IXxxRepository` (aggregates/entities); QueryHandlers use `IXxxReadRepository` (DTOs); neither ever injects a `DbContext`. The routing rules that become binding once a replica exists (Command ⇒ Write always; Query ⇒ Read by default; the closed list of five cases where a query must use Write) are in `references/dotnet-rules.md` → CQRS Implementation.
- Minimal API route groups.
- Mapperly.
- OpenTelemetry instrumentation always installed; exporter left as none unless the user names an observability backend.
- Auth: ask if not specified. Do not assume public or no-auth for production APIs.

---

## Step 2: Read Reference Guides

Read these files **in full** before proceeding:

1. `references/dotnet-rules.md` — development & testing conventions to enforce throughout the project.
2. `references/dotnet-scaffold.md` — canonical directory structure, scaffold commands, CPM configuration, entry point template, CQRS conventions, and domain events dispatch flow.
3. `references/dotnet-domain-template.cs` — canonical implementations of `AggregateRoot<TId>`, `Entity<TId>`, `DomainEvent`, `IDomainEvent`, `Result<T>`, and `ValueObject`. Use verbatim — do not improvise these building blocks.
4. `references/dotnet-cqrs-template.cs` — canonical CQRS interfaces, dispatchers, and assembly scanning registration. Copy verbatim into `<ProjectName>.Application/Cqrs/CQRS.cs` (namespace `<ProjectName>.Application.Cqrs`).

Follow them exactly — do not improvise the structure.

---

## Step 3: Scaffold

⚠️ **Do not start until Step 1.5 has been confirmed by the user.**

Execute the scaffold in this order:

1. **Run `scripts/scaffold.sh` — it owns all the deterministic, error-prone mechanics** (solution + projects + references + CPM + package installation + Mapperly `PrivateAssets` + conditional DB provider/OTel + vulnerable-transitive pinning). Do not hand-run the `dotnet new`/`dotnet add` commands unless the script is unavailable.

   ```bash
   scripts/scaffold.sh --name <ProjectName> --db <sqlserver|postgres> \
     --mapper <mapperly|mapster|none> --otel <none|otlp|console|azure> [--read-replicas] [--dir <parent>]
   ```

   Pass the Step 1 choices as flags. What the script guarantees so you don't have to manage it manually:
   - **CPM is correct by construction**: `Directory.Build.props`/`Directory.Packages.props` are written first, so every `dotnet add` records the version centrally and leaves the `.csproj` version-less. It also strips the inline versions the `dotnet new` templates emit. *You do not need to hand-remove any `Version="..."` attributes.*
   - **Versions are latest-stable, never prerelease** — `dotnet add package` resolves them at scaffold time from NuGet. No remembered/hardcoded versions.
   - **Vulnerable transitive packages are auto-pinned** — because `TreatWarningsAsErrors=true` turns an `NU1903` transitive advisory into a build failure, the script detects the named packages and pins each to a patched version (e.g. `Microsoft.OpenApi` → latest 2.x). If it prints a `!!! WARNING` that it could not resolve a patch, resolve that package manually before continuing.

   If network access is unavailable, the script's package resolution will fail — stop and report that, rather than guessing versions.
   The reference package list in `dotnet-scaffold.md` remains the source of truth for *what* gets installed; do not add packages outside it without asking.
2. Wire up the entry point (`Program.cs`) with the minimum viable setup — **overwrite** the `dotnet new webapi` generated content entirely using the canonical template.
3. Copy `references/dotnet-cqrs-template.cs` verbatim into a single `Cqrs/CQRS.cs` file inside the `Application` project, replacing `YourProject` with the project namespace (namespace becomes `<ProjectName>.Application.Cqrs`). This file holds only the CQRS core (interfaces, dispatchers, `AddCqrs`); business commands/queries/handlers live in the `Commands/`, `Queries/`, and `EventHandlers/` folders of the same project.
4. Create the layer stubs and domain building blocks — what to scaffold differs by domain model style:

   **Rich Domain Model:**
   - Copy `AggregateRoot<TId>`, `Entity<TId>`, `IDomainEvent`, `DomainEvent` from `dotnet-domain-template.cs` into the `Domain` project.
   - Copy `ValueObject` abstract base class into `Domain/ValueObjects/` (for cases requiring custom validation logic; prefer `record` types otherwise).
   - Scaffold **one named repository interface per aggregate** in `Domain/Interfaces/` (e.g. `IOrderRepository.cs`), following the `IOrderRepository` example in `dotnet-domain-template.cs`. There is no generic `IRepository<T>` — do not create one. Declare only the operations that aggregate's CommandHandlers and Domain Services actually call; methods return tracked aggregate roots.
   - Scaffold `Domain/Interfaces/IUnitOfWork.cs` using the persistence-agnostic `CommitAsync(CancellationToken)` contract from `dotnet-domain-template.cs`.
   - If Domain Events were enabled (Step 1 item 7): scaffold the following files — use the reference implementations in `dotnet-domain-template.cs` verbatim:

     | 檔案 | 專案 | 說明 |
     |------|------|------|
     | `Domain/Interfaces/IDomainEventDispatcher.cs` | `<ProjectName>.Domain` | 分派 domain events 的介面；IUnitOfWork 依賴此介面 |
     | `Domain/Interfaces/IDomainEventHandler.cs` | `<ProjectName>.Domain` | 泛型 handler 介面 `IDomainEventHandler<TEvent>`；Application EventHandlers 實作此介面 |
     | `Infrastructure/DomainEventDispatcher.cs` | `<ProjectName>.Infrastructure` | `IDomainEventDispatcher` 的 in-process 實作；透過 DI 解析並呼叫所有 `IDomainEventHandler<TEvent>` |

     DI 註冊使用 assembly scanning（見 `dotnet-domain-template.cs` 的 DI registration 範例）。Application-layer event handlers 放在 `<ProjectName>.Application/EventHandlers/`，實作對應的 `IDomainEventHandler<TEvent>`。
   - If Result Pattern was enabled (Step 1 item 8): copy `Result<T>` and `Result` from `dotnet-domain-template.cs` into `Domain/Common/`.

   **Anemic Domain Model:**
   - **Do NOT** scaffold `AggregateRoot<TId>`, `Entity<TId>`, or `ValueObject` — these building blocks are not used in the Anemic model.
   - Scaffold **one named repository interface per entity** in `Domain/Interfaces/` (e.g. `IOrderRepository.cs`), following the `IOrderRepository` example in `dotnet-domain-template.cs` with the plain POCO entity type in place of the aggregate root. There is no generic `IRepository<T>` — do not create one, and no base class or generic constraint is involved.
   - Scaffold `Domain/Interfaces/IUnitOfWork.cs` using the same persistence-agnostic `CommitAsync(CancellationToken)` contract from `dotnet-domain-template.cs`.
   - Add concrete POCO entity classes directly in `Domain/Entities/` — no base class required.
   - Do **not** create Domain Service stubs up front. Add a capability-named Domain Service in `Domain/DomainServices/` (e.g. `PricingService`) only for **cross-entity operations** or **high-complexity pure business calculations** that are shared by more than one handler — simple CRUD and single-use logic belong in Application handlers. They inject Domain-defined interfaces only (never Infrastructure/Persistence types); see the naming rule and dependency rules in `references/dotnet-rules.md` → **Domain Model Style / Domain Services**.
   - If Domain Events were enabled (Step 1 item 7): scaffold the following files — use the reference implementations in `dotnet-domain-template.cs` verbatim:

     | 檔案 | 專案 | 說明 |
     |------|------|------|
     | `Domain/Events/IDomainEvent.cs` | `<ProjectName>.Domain` | Domain event marker interface |
     | `Domain/Events/DomainEvent.cs` | `<ProjectName>.Domain` | Domain event base record |
     | `Domain/Interfaces/IDomainEventDispatcher.cs` | `<ProjectName>.Domain` | 分派 domain events 的介面 |
     | `Domain/Interfaces/IDomainEventHandler.cs` | `<ProjectName>.Domain` | 泛型 handler 介面 `IDomainEventHandler<TEvent>` |
     | `Infrastructure/DomainEventDispatcher.cs` | `<ProjectName>.Infrastructure` | in-process 實作；透過 DI 解析並呼叫所有 `IDomainEventHandler<TEvent>` |

     Application handler（或其呼叫的 Domain Service）在 repository 操作完成且 use case commit 成功後手動呼叫 `DispatchAsync()`。DI 註冊使用 assembly scanning。Application-layer event handlers 放在 `<ProjectName>.Application/EventHandlers/`，實作 `IDomainEventHandler<TEvent>`.
   - If Result Pattern was enabled (Step 1 item 8): copy `Result<T>` and `Result` from `dotnet-domain-template.cs` into `Domain/Common/`.

   **Both models:**
   - Create remaining empty stub classes in all layers so the project compiles cleanly.
5. Fill in the testing projects (the script already created `UnitTests` and `IntegrationTests` with the right SDK and packages):
   - `UnitTests` — isolated tests of domain logic, validators, mappers, and application handlers.
   - `IntegrationTests` — ASP.NET Core request pipeline, DI, persistence, and infrastructure tests via `Microsoft.AspNetCore.Mvc.Testing` / `WebApplicationFactory` (already on the Web SDK).
   - The scaffold is not complete with placeholder tests only. Add meaningful tests that cover the generated feature skeletons and any user-confirmed workflows.
6. Create AI guidance files without bloating the root `AGENTS.md`:
   - Create `AGENTS.md` at the **workspace root** — the root directory of the generated solution. **Do NOT place it inside `src/`, `backend/`, or any project subdirectory.**
   - Keep root `AGENTS.md` short. It is an index and mandatory reading policy, not the full rule body.
   - Create `docs/agents/dotnet-rules.md` and write the full content of `references/dotnet-rules.md` into it.
   - If `~/.agents/AGENTS.md` exists, copy only the globally applicable essentials into root `AGENTS.md`: language, security, documentation alignment, brownfield safety, package manager, Tailwind/frontend rules if applicable, and git commit confirmation. Do not paste the entire global file verbatim.
   - Root `AGENTS.md` must explicitly say that before .NET/backend/API/database/test changes, agents must read `docs/agents/dotnet-rules.md`.
   - If a root `AGENTS.md` already exists, do not overwrite it. Append or merge a concise `Required References` section pointing to `docs/agents/dotnet-rules.md`.
7. **Compile & validate — mandatory final step.** After all files are written, run the following commands in order and confirm each succeeds before declaring the scaffold complete:
   ```bash
   dotnet build          # must produce 0 errors, 0 warnings
   dotnet test           # all tests must pass (green)
   ```
   If either command fails, fix all errors before proceeding. Do NOT report the scaffold as complete while build or test failures remain.

After each major step, confirm success before continuing to the next.

---

## Step 4: Post-Scaffold Checklist

Before declaring the scaffold complete, verify:

- [ ] Project builds / runs without errors
- [ ] Layer separation is correct — `Domain` project has **zero** `<PackageReference>` entries
- [ ] `Application/Cqrs/CQRS.cs` (namespace `<ProjectName>.Application.Cqrs`) contains only CQRS interfaces, dispatchers, and DI registration from `dotnet-cqrs-template.cs`; business commands/queries/handlers live in Application's `Commands/`/`Queries/`/`EventHandlers/`, not in this file
- [ ] Each command/query shares a file with its own handler, named after the message (`ApproveSupplierCommand.cs`); no file holds two unrelated commands, and no module-wide `XxxDtos.cs` bundles unrelated DTOs
- [ ] DDD directory structure in place — Rich model: `Events/`, `DomainServices/`, `ValueObjects/`, `Entities/` (with AggregateRoot base) in Domain, `EventHandlers/` in Application; Anemic model: `Entities/` (plain POCOs) in Domain, `Events/` and `ValueObjects/` folders omitted
- [ ] Domain model style consistently applied (Rich: private setters + factory methods; Anemic: public setters only)
- [ ] If Domain Events enabled: Rich model dispatches collected aggregate events after `IUnitOfWork.CommitAsync()` commits successfully; Anemic model dispatches explicit events only after the use case commit succeeds
- [ ] In-process domain event handlers do not perform irreversible/reliability-critical side effects directly; external side effects use explicit Application orchestration or retryable background work only when needed
- [ ] If Result Pattern enabled: `Result<T>` in `Domain/Common/`; domain methods return `Result` instead of throwing for business violations
- [ ] If Event-Driven Design enabled: `Application/IntegrationEvents/` scaffolded, `IEventBus` defined in `Application/Interfaces/`, stub implementation in `Infrastructure/`
- [ ] Exactly one DB provider package is added in `WebApi`, matching the user-selected database
- [ ] `appsettings` declares `ConnectionStrings:Write` **and** `ConnectionStrings:Read` (no `Default` key anywhere); without a replica both hold the same value and only `Write` is bound in DI, with exactly one `DbContext` registered
- [ ] No generic `IRepository<T>` exists anywhere; each aggregate has its own named `IXxxRepository` declaring only the operations its callers use, with no shared base interface
- [ ] Repository interfaces sit in the project the chosen domain model dictates — **Anemic**: `IXxxRepository`, `IXxxReadRepository`, `IUnitOfWork` and the domain-event interfaces all in `Application`, with `Domain` holding **no** persistence interfaces at all; **Rich**: `IXxxRepository`/`IUnitOfWork`/domain-event interfaces in `Domain/Interfaces/`, `IXxxReadRepository` in `Application/Interfaces/`. The layout in force is recorded in the README
- [ ] Write/read repository split is present regardless of topology: write side returns entities/aggregates, read side returns DTOs, both implemented in `Persistence`; no handler injects a `DbContext`, no CommandHandler injects a read repository, no QueryHandler injects a write repository
- [ ] If read replicas are enabled: `WriteDbContext` and `ReadDbContext` both derive from a shared abstract `AppDbContext` (model configured once) and bind to `ConnectionStrings:Write` / `ConnectionStrings:Read`; write repositories and `IUnitOfWork` take `WriteDbContext`, read repositories take `ReadDbContext`; a strong-consistency query is served by a second read-repository implementation on the write context resolved via a keyed service, never by injecting a `DbContext` into the handler, and the case is named in a comment; migrations and the design-time factory bind to `Write`; health checks probe both connections
- [ ] `Program.cs` ends with `public partial class Program;` so `WebApplicationFactory<Program>` compiles
- [ ] Integration-test config is injected with `UseSetting`, not `ConfigureAppConfiguration` (the app's own `appsettings.{Env}.json` overrides the latter and its secret fields are empty by policy); scopes resolving `IUnitOfWork` use `CreateAsyncScope()`
- [ ] `Directory.Packages.props` contains verified latest stable versions, no `LATEST_STABLE` placeholders, no preview/beta/RC packages
- [ ] Serilog bootstrap logger in `Program.cs`; `UseSerilogRequestLogging` registered; enrichers installed
- [ ] `appsettings.json` has full Serilog config (sinks, enrichers, level overrides); no hard-coded sink setup in `Program.cs`
- [ ] Environment variables used for all secrets — nothing hardcoded
- [ ] `.gitignore` includes secrets, build output, and local env files
- [ ] `.config/dotnet-tools.json` exists and is committed (contains `dotnet-ef` local tool)
- [ ] Root `AGENTS.md` created as a short index/policy file, not a full rules dump; `docs/agents/dotnet-rules.md` contains the complete .NET rules and is referenced from `AGENTS.md`
- [ ] README created with: project purpose, how to run, how to test
- [ ] Unit and integration test projects scaffolded; meaningful tests cover generated feature skeletons and user-confirmed workflows
- [ ] If OpenTelemetry was enabled: OTel packages installed, exporter configured, `Program.cs` wired up

---

## Key Constraints (always enforce)

The full normative detail for every rule below lives in `references/dotnet-rules.md` — that file is the single source of truth. This list is a quick index of the non-negotiables so you can catch violations without re-reading the spec. When a rule needs nuance (exact wording, edge cases, examples), consult the named section in `dotnet-rules.md`.

These hold regardless of what the user asks:

- **Package manager**: `dotnet` CLI for .NET packages; `pnpm` for frontend assets. Never `npm`/`yarn`.
- **Package policy**: latest stable only (no preview/beta/RC), looked up at scaffold time; MIT/Apache-2.0 licences only (no GPL/copyleft). *(rules.md → Environment & Architecture)*
- **Domain purity**: `Domain` project has **zero** `<PackageReference>` entries. *(rules.md → Backend Core Isolation)*
- **Aggregate boundary**: no cross-aggregate references to non-root entities; communicate via repository interfaces or domain events. *(rules.md → DDD Building Blocks)*
- **Secrets**: env vars / secret managers only — nothing hardcoded; `.gitignore` excludes secrets, env files, build output.
- **One class per file** (auxiliary DTO/record/request/response types serving one primary class may co-locate). *(rules.md → Environment & Architecture)*
- **Persistence & UoW**: EF Core default for aggregate/transactional writes; Dapper/raw SQL for reporting/perf. Everything commits through `IUnitOfWork.CommitAsync()` — handlers never call `SaveChangesAsync()` or manage transactions. No Persistence-local plumbing interfaces. *(rules.md → Data Access)*
- **CQRS DB routing**: CommandHandlers (incl. their reads) use the write DB; simple QueryHandlers use EF + `AsNoTracking()` on the read DB; complex/reporting queries use Dapper on the read DB unless read-your-writes is required and documented. *(rules.md → CQRS Implementation)*
- **Domain services**: Rich services are pure (no repository injection); Anemic Domain Services are capability-named (no `Manager` suffix), live in Domain, inject only Domain-defined interfaces, and never manage transactions or inject Infrastructure types. *(rules.md → DDD Building Blocks)*
- **Domain-event side effects**: in-process handlers do no irreversible/reliability-critical work directly; use Application orchestration or idempotent retryable background work. *(rules.md → Event-Driven Design)*
- **Testing**: only `UnitTests` (xUnit + NSubstitute) and `IntegrationTests` (`WebApplicationFactory`, Web SDK). Meaningful tests required — placeholders don't count. *(rules.md → Testing Guidelines)*
- **Timezone confirmed before any DB schema/migration** — never assume UTC. *(rules.md → Timezone & Time Abstraction)*
- **API docs**: `Microsoft.AspNetCore.OpenApi` + **Scalar** only — no Swashbuckle/Swagger. *(rules.md → API Documentation)*
- **Validation**: **FluentValidation** only; no DataAnnotations for business rules. *(rules.md → Validation)*
- **Object mapping**: **AutoMapper forbidden** — Mapperly / Mapster / manual only (chosen in Step 1). *(rules.md → Object Mapping)*
- **Caching**: none by default; prefer **HybridCache** if needed. *(rules.md → Caching)*
- **Asking questions**: always pair a technology choice with a clear recommendation — never ask blind.
- **Conflicts with existing project docs are the user's call**: when local documentation and this skill disagree, surface every conflict and let the user decide. Never silently follow either side, and never treat a documented architectural decision as a defect to be corrected. *(Step 0)*
- **Mandatory build verification**: the session is not complete until `dotnet build` (0 errors) and `dotnet test` (all green) both pass. Never skip.
