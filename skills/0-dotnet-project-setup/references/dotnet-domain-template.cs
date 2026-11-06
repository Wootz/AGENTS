// ============================================================================
// DDD Domain Layer Templates
// Scope: Domain project only — zero framework dependencies
// Place files in src/<ProjectName>.Domain/ following the directory structure
// Code comments (XML docs and inline comments) are copied into the project and
// are written in Traditional Chinese; the FILE banners and the ═══ note blocks
// are guidance for the agent and are not copied.
// ============================================================================

// ─────────────────────────────────────────────────────────────────────────────
// FILE: Domain/Events/IDomainEvent.cs
// ─────────────────────────────────────────────────────────────────────────────

namespace YourProject.Domain.Events;

/// <summary>
/// 所有 domain event 的標記介面。
/// Domain event 於 aggregate 內產生，並在交易 commit 後
/// 透過 IDomainEventDispatcher（Domain 層）派送。
/// </summary>
public interface IDomainEvent
{
    /// <summary>事件發生的 UTC 時間。</summary>
    DateTime OccurredOn { get; }
}

// ─────────────────────────────────────────────────────────────────────────────
// FILE: Domain/Events/DomainEvent.cs
// ─────────────────────────────────────────────────────────────────────────────

namespace YourProject.Domain.Events;

/// <summary>
/// Domain event 的基底 record，所有具體事件皆繼承此型別。
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// ─────────────────────────────────────────────────────────────────────────────
// FILE: Domain/Entities/AggregateRoot.cs
// ─────────────────────────────────────────────────────────────────────────────

using YourProject.Domain.Events;

namespace YourProject.Domain.Entities;

/// <summary>
/// 所有 Aggregate Root 的基底類別。
/// 保存本次操作中產生、尚未 commit 的 domain event。
/// 事件由 Application 層在交易 commit 後派送——絕不在 domain 內部派送。
/// </summary>
public abstract class AggregateRoot<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>強型別的 aggregate 識別值。</summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// 尚未 commit 的 domain event 唯讀快照。
    /// 由 IUnitOfWork 於 CommitAsync 取用，在持久化交易成功 commit 後派送。
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// 於 aggregate 方法內呼叫以記錄 domain event。
    /// 此處不會派送事件——只會排入佇列等待稍後派送。
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    /// <summary>
    /// 清除所有已排入的 domain event。由 IUnitOfWork 在事件派送成功後呼叫。
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}

// ─────────────────────────────────────────────────────────────────────────────
// FILE: Domain/Entities/Entity.cs  (for non-root entities within an aggregate)
// ─────────────────────────────────────────────────────────────────────────────

namespace YourProject.Domain.Entities;

/// <summary>
/// 非 root entity（aggregate 內的子實體）的基底類別。
/// 不產生 domain event——只有 AggregateRoot 會產生事件。
/// </summary>
public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id!.Equals(other.Id);
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        => left?.Equals(right) ?? right is null;

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
        => !(left == right);
}

// ─────────────────────────────────────────────────────────────────────────────
// FILE: Domain/Common/Result.cs
// ─────────────────────────────────────────────────────────────────────────────

namespace YourProject.Domain.Common;

/// <summary>
/// Domain 操作的 Result pattern。
/// 預期內的業務規則違反以此回傳，不拋出例外；
/// 只有真正非預期的錯誤或基礎設施故障才拋出例外。
///
/// 成功：Result.Ok(order)
/// 失敗：Result.Fail&lt;Order&gt;("Order already cancelled.")
/// </summary>
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string Error { get; }

    private Result(T? value, bool isSuccess, string error)
    {
        Value = value;
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result<T> Ok(T value)
        => new(value, true, string.Empty);

    public static Result<T> Fail(string error)
        => new(default, false, error);

    /// <summary>
    /// 成功時轉換值；失敗時原樣傳遞錯誤。
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
        => IsSuccess ? Result<TOut>.Ok(mapper(Value!)) : Result<TOut>.Fail(Error);
}

/// <summary>
/// 無回傳值操作使用的非泛型 Result。
/// </summary>
public sealed class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    private Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Ok() => new(true, string.Empty);
    public static Result Fail(string error) => new(false, error);
}

// ─────────────────────────────────────────────────────────────────────────────
// FILE: Domain/ValueObjects/ValueObject.cs  (optional base — prefer records)
// ─────────────────────────────────────────────────────────────────────────────

namespace YourProject.Domain.ValueObjects;

/// <summary>
/// Value Object 優先使用 C# `record`——不需任何樣板程式碼就有結構相等性：
///
///   public sealed record Money(decimal Amount, string Currency);
///   public sealed record EmailAddress(string Value);
///
/// 只有在建構子需要 record primary constructor 放不下的自訂驗證邏輯時，
/// 才使用這個抽象類別。
/// </summary>
public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        return GetEqualityComponents()
            .SequenceEqual(((ValueObject)obj).GetEqualityComponents());
    }

    public override int GetHashCode()
        => GetEqualityComponents()
            .Aggregate(0, HashCode.Combine);

    public static bool operator ==(ValueObject? left, ValueObject? right)
        => left?.Equals(right) ?? right is null;

    public static bool operator !=(ValueObject? left, ValueObject? right)
        => !(left == right);
}

// ─────────────────────────────────────────────────────────────────────────────
// FILE: Domain/Interfaces/IOrderRepository.cs            (Rich)
//       Application/Interfaces/IOrderRepository.cs       (Anemic — namespace YourProject.Application.Interfaces)
//
// Every aggregate gets its own named interface declaring only the operations
// that aggregate actually needs. The same rule applies to both Rich and Anemic models; only the entity type
// referenced by the signatures differs (aggregate root vs. plain POCO).
// ─────────────────────────────────────────────────────────────────────────────

using YourProject.Domain.Entities;

namespace YourProject.Domain.Interfaces;

/// <summary>
/// Order aggregate 的寫入端 repository。
/// Rich model 定義於 Domain（aggregate 自身的持久化契約），Anemic model 定義於 Application。
/// 只由 CommandHandler 注入——Domain Service 與 QueryHandler 一律不注入
/// （QueryHandler 使用 IXxxReadRepository）。
/// 實作位於 Persistence 層（EF Core、Dapper/raw SQL 或其他 provider）。
/// Repository 以 aggregate 為邊界運作——IQueryable&lt;T&gt; 不得流出 Persistence，
/// 也不回傳 DTO（那是 IOrderReadRepository 的職責）。
/// </summary>
public interface IOrderRepository
{
    /// <summary>依識別值載入受追蹤的 aggregate；不存在時回傳 null。</summary>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 專屬於此 aggregate 的載入方法放在這裡，以其服務的 use case 命名。
    /// 只宣告 CommandHandler 實際呼叫的方法——
    /// 不要為了「完整」預先加入 CRUD 方法。
    /// </summary>
    Task<IReadOnlyList<Order>> GetPendingForCustomerAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    void Update(Order order);

    void Remove(Order order);
}

// ════════════════════════════════════════════════════════════════════════════
// Notes that apply to every write-side repository interface
//
//  - Name it after the aggregate: IOrderRepository, ICustomerRepository.
//    No shared base interface — each aggregate's surface is its own.
//  - No GetAllAsync unless the table is genuinely bounded (lookup/reference
//    data). Unbounded reads belong on the read side with paging.
//  - Methods return tracked aggregates. Reads that feed a screen go through
//    IXxxReadRepository in Application/Interfaces/ and return DTOs.
//  - Repositories never call SaveChanges — the commit boundary is IUnitOfWork.
//  - Anemic model: identical shape, declared in Application/Interfaces/, with the
//    plain POCO entity type in place of the aggregate root.
// ════════════════════════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────────────────────
// FILE: Domain/Interfaces/IUnitOfWork.cs   (Rich)
//       Application/Interfaces/IUnitOfWork.cs (Anemic — namespace YourProject.Application.Interfaces)
//
// The contract is persistence-agnostic and is used by both Rich and Anemic models.
// ─────────────────────────────────────────────────────────────────────────────

namespace YourProject.Domain.Interfaces;

// ════════════════════════════════════════════════════════════════════════════
// UNIT OF WORK — persistence-agnostic commit boundary
// Repositories record persistence operations. Application handlers call
// CommitAsync once per use case. EF, Dapper/raw SQL, MongoDB, or other
// persistence technologies decide how the commit is implemented.
// ════════════════════════════════════════════════════════════════════════════

public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>
    /// Commit 本次 use case 所有待處理的持久化操作。
    /// 實作必須將交易邊界留在 Persistence 內，
    /// 不得把 EF Core 或 SQL 的交易型別暴露給 Domain / Application。
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);
}

// ── Rich EF + Dapper/raw SQL UnitOfWork reference implementation ─────────────

// using System.Data.Common;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Storage;
// using YourProject.Domain.Entities;        // AggregateRoot<T>
// using YourProject.Domain.Interfaces;      // IUnitOfWork, IDomainEventDispatcher
//
// public sealed class AppUnitOfWork(WriteDbContext dbContext, IDomainEventDispatcher dispatcher)
//     : IUnitOfWork
// {
//     private IDbContextTransaction? _transaction;
//
//     // 執行 Dapper/raw SQL 的 Persistence repository 可注入這個具體的
//     // AppUnitOfWork 並呼叫 EnsureTransactionAsync()，讓 EF 與 raw SQL
//     // 共用同一條連線與交易。不要把這個具體型別暴露給 Domain 或 Application。
//     public async Task<IDbContextTransaction> EnsureTransactionAsync(CancellationToken cancellationToken = default)
//         => _transaction ??= await dbContext.Database.BeginTransactionAsync(cancellationToken);
//
//     // 關鍵——Connection 與 CurrentTransaction 必須都來自同一個 dbContext。
//     // BeginTransactionAsync() 是在這條連線上開啟交易，所以 Dapper 指令
//     // (a) 在這個 Connection 上執行、且 (b) 傳入這個 CurrentTransaction，
//     // 才會與 EF 的 SaveChanges 加入同一筆交易。
//     // 若 repository 自行開 SqlConnection 而非使用這個 Connection，傳入
//     // CurrentTransaction 不是拋出例外（「transaction not associated with
//     // connection」），就是更糟——兩條連線各自成為獨立交易，一致性被靜默破壞。
//     // 絕對不要自行建立連線。
//     public DbConnection Connection => dbContext.Database.GetDbConnection();
//
//     public DbTransaction? CurrentTransaction =>
//         dbContext.Database.CurrentTransaction?.GetDbTransaction();
//
//     public async Task CommitAsync(CancellationToken cancellationToken = default)
//     {
//         // 1. 存檔前先收集待派送的 domain event。
//         //    注意：這裡寫死 AggregateRoot<Guid>。若專案的 aggregate 識別值是
//         //    其他型別（int、long、強型別 id），必須同步修改這個型別參數——
//         //    否則永遠收集不到事件，而且失敗是靜默的：資料全部 commit，事件一個都不派送。
//         //    若有多種識別值型別，改以非泛型的 IHasDomainEvents 介面標記 aggregate，
//         //    並查詢 Entries<IHasDomainEvents>()。
//         var aggregates = dbContext.ChangeTracker
//             .Entries<AggregateRoot<Guid>>()
//             .Where(e => e.Entity.DomainEvents.Count > 0)
//             .Select(e => e.Entity)
//             .ToList();
//
//         var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();
//
//         // 2. 先 commit 持久化。只有在 SaveChangesAsync 以外還有其他寫入時——
//         //    也就是 Dapper/raw SQL repository 呼叫過 EnsureTransactionAsync()——
//         //    才需要明確的交易。單獨的 SaveChangesAsync 本身就是 atomic，
//         //    每個 use case 都開交易只會多一次 BEGIN/COMMIT 往返，並延長佔用連線的時間。
//         if (_transaction is null)
//         {
//             await dbContext.SaveChangesAsync(cancellationToken);
//         }
//         else
//         {
//             try
//             {
//                 await dbContext.SaveChangesAsync(cancellationToken);
//                 await _transaction.CommitAsync(cancellationToken);
//             }
//             catch
//             {
//                 // Rollback 本身也可能失敗——連線可能已斷，或伺服器已中止交易。
//                 // 吞掉這個例外：呼叫端需要看到的是「原始」例外，
//                 // 讓 rollback 的例外往外拋會掩蓋真正的失敗原因。
//                 try { await _transaction.RollbackAsync(cancellationToken); }
//                 catch { /* 保留原始例外作為回報的失敗原因 */ }
//                 throw;
//             }
//             finally
//             {
//                 // 不論成敗都要釋放：已 commit 或已 rollback 的交易無法重用。
//                 // 若不清空，同一個 scope 內第二次 CommitAsync() 會重用已結束的交易
//                 // （EnsureTransactionAsync 的 ??= 會交回那個失效的交易），
//                 // 拋出「This transaction has completed; it is no longer usable」。
//                 // 在同一個 scope 內處理多個 use case 的批次作業會立刻踩到。
//                 await _transaction.DisposeAsync();
//                 _transaction = null;
//             }
//         }
//
//         // 3. 只有在 DB 交易成功 commit 後才派送 in-process domain event。
//         //    handler 保持在本地端且不可承擔關鍵工作。
//         //
//         //    ⚠️ 此時資料「已經」commit。若 handler 拋出例外，這個方法會一併拋出，
//         //    呼叫端會看到失敗，但寫入其實已成功——遺失的是副作用，不是資料。
//         //    這正是「in-process handler 不得執行不可逆或可靠度關鍵的工作
//         //    （寄信、呼叫外部系統、跨服務通知）」這條規則背後刻意的取捨。
//         //    「一定要發生」的事，應放進依已 commit 狀態觸發、可重試的背景工作，
//         //    或透過 outbox 發布的 integration event——絕不放在這裡。
//         //    吞掉 handler 的例外也不是解法：那會藏住真正的 bug。
//         //    讓它拋出，並保持 handler 簡單。
//         if (domainEvents.Count > 0)
//             await dispatcher.DispatchAsync(domainEvents, cancellationToken);
//
//         foreach (var aggregate in aggregates)
//             aggregate.ClearDomainEvents();
//     }
//
//     public async ValueTask DisposeAsync()
//     {
//         // 只有在 use case 從未 commit 時，才會帶著進行中的交易走到這裡
//         // ——發生例外，或 Dapper repository 開了交易後提前 return。
//         // Dispose IDbContextTransaction 會隱含 rollback，但仍明確 rollback，
//         // 讓意圖寫在程式碼裡，而不是依賴 EF 的 dispose 語意。
//         if (_transaction is not null)
//         {
//             try { await _transaction.RollbackAsync(); }
//             catch { /* 交易已結束或連線已斷——沒有可復原的東西 */ }
//             await _transaction.DisposeAsync();
//             _transaction = null;
//         }
//     }
// }
//
// DI 註冊：
// services.AddScoped<AppUnitOfWork>();
// services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppUnitOfWork>());

// ── Usage in a Rich command handler ──────────────────────────────────────────

// await using var uow = serviceProvider.GetRequiredService<IUnitOfWork>();
// var result = aggregate.DoSomething();
// if (result.IsFailure) return result;
// await _repository.AddAsync(aggregate, cancellationToken);
// await uow.CommitAsync(cancellationToken); // 先 commit 持久化，再派送 domain event

// ════════════════════════════════════════════════════════════════════════════
// ANEMIC DOMAIN MODEL
// Use the same IUnitOfWork interface. Repositories must not call SaveChanges
// or commit transactions themselves; Application handlers call CommitAsync
// once after all repository operations for the use case are complete.
// ════════════════════════════════════════════════════════════════════════════

// ── Anemic EF/Dapper UnitOfWork reference implementation ─────────────────────

// using System.Data.Common;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Storage;
// using YourProject.Application.Interfaces; // IUnitOfWork
//
// public sealed class AppUnitOfWork(WriteDbContext dbContext) : IUnitOfWork
// {
//     private IDbContextTransaction? _transaction;
//
//     // 執行 Dapper/raw SQL 的 Persistence repository 可注入這個具體的
//     // AppUnitOfWork 並呼叫 EnsureTransactionAsync()，讓 EF 與 raw SQL
//     // 共用同一條連線與交易。不要把這個具體型別暴露給 Domain 或 Application。
//     public async Task<IDbContextTransaction> EnsureTransactionAsync(CancellationToken cancellationToken = default)
//         => _transaction ??= await dbContext.Database.BeginTransactionAsync(cancellationToken);
//
//     // 關鍵——Connection 與 CurrentTransaction 必須都來自同一個 dbContext
//     // （見 Rich 實作的說明）。Dapper repository 在這個 Connection 上執行並傳入
//     // 這個 CurrentTransaction；絕對不要另外建立連線。
//     public DbConnection Connection => dbContext.Database.GetDbConnection();
//
//     public DbTransaction? CurrentTransaction =>
//         dbContext.Database.CurrentTransaction?.GetDbTransaction();
//
//     public async Task CommitAsync(CancellationToken cancellationToken = default)
//     {
//         // 只有在 SaveChangesAsync 以外還有其他寫入時——也就是 Dapper/raw SQL
//         // repository 呼叫過 EnsureTransactionAsync()——才需要明確的交易。
//         // SaveChangesAsync 本身就是 atomic，無條件開交易只會多一次
//         // BEGIN/COMMIT 往返，並延長佔用連線的時間。
//         if (_transaction is null)
//         {
//             await dbContext.SaveChangesAsync(cancellationToken);
//             return;
//         }
//
//         try
//         {
//             await dbContext.SaveChangesAsync(cancellationToken);
//             await _transaction.CommitAsync(cancellationToken);
//         }
//         catch
//         {
//             // Rollback 本身也可能失敗（連線已斷、伺服器端已中止交易）。
//             // 吞掉這個例外——呼叫端需要的是「原始」例外；
//             // 讓 rollback 的例外往外拋會掩蓋它。
//             try { await _transaction.RollbackAsync(cancellationToken); }
//             catch { /* 保留原始例外作為回報的失敗原因 */ }
//             throw;
//         }
//         finally
//         {
//             // 不論成敗都要釋放——已結束的交易無法重用。若不清空，同一個 scope 內
//             // 第二次 CommitAsync() 會透過 EnsureTransactionAsync 的 ??= 重用失效的交易，
//             // 拋出「This transaction has completed; it is no longer usable」。
//             // 在同一個 scope 內執行多個 use case 的批次作業會立刻踩到。
//             await _transaction.DisposeAsync();
//             _transaction = null;
//         }
//     }
//
//     public async ValueTask DisposeAsync()
//     {
//         // 只有在 use case 從未 commit 時，才會帶著進行中的交易走到這裡
//         // ——發生例外，或 Dapper repository 開了交易後提前 return。
//         // Dispose 會隱含 rollback，但仍明確 rollback，
//         // 讓意圖寫在程式碼裡，而不是依賴 EF 的 dispose 語意。
//         if (_transaction is not null)
//         {
//             try { await _transaction.RollbackAsync(); }
//             catch { /* 交易已結束或連線已斷 */ }
//             await _transaction.DisposeAsync();
//             _transaction = null;
//         }
//     }
// }
//
// DI 註冊：
// services.AddScoped<AppUnitOfWork>();
// services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppUnitOfWork>());

// ── Usage in an Anemic command handler ───────────────────────────────────────

// await using var uow = serviceProvider.GetRequiredService<IUnitOfWork>();
// await _repository.AddAsync(entity, cancellationToken);
// otherRepository.Update(otherEntity);
// await uow.CommitAsync(cancellationToken); // 此 use case 唯一的 commit 點

// ── Dapper/raw SQL repository sharing the UoW transaction ────────────────────
// A Persistence-layer repo that needs a narrow raw-SQL UPDATE (counter, flag,
// performance-sensitive write) enrols in the SAME transaction as EF by injecting
// the concrete AppUnitOfWork. Three things must ALL hold, or the write leaks out
// of the transaction:
//   1. EnsureTransactionAsync() is awaited FIRST — otherwise CurrentTransaction is
//      null, and a null transaction makes Dapper autocommit the statement as its
//      own independent write (no error, silent consistency break).
//   2. The command executes on uow.Connection (EF's connection), not a new one.
//   3. transaction: uow.CurrentTransaction is passed (the tx opened on that connection).
// The handler still owns the only commit point: uow.CommitAsync() commits EF's
// SaveChanges and this UPDATE together, or rolls both back.
//
// using Dapper;
// using YourProject.Persistence.UnitOfWork;   // 具體的 AppUnitOfWork（僅限 Persistence 內部）
//
// public sealed class ProductStockRepository(AppUnitOfWork uow)
// {
//     public async Task DeductStockAsync(Guid productId, int qty, CancellationToken ct)
//     {
//         await uow.EnsureTransactionAsync(ct);            // (1) 確保交易已開啟
//         var affected = await uow.Connection.ExecuteAsync(new CommandDefinition(
//             "UPDATE Products SET Stock = Stock - @qty WHERE Id = @productId AND Stock >= @qty",
//             new { productId, qty },
//             transaction: uow.CurrentTransaction,         // (2)+(3) 同一條連線＋同一筆交易
//             cancellationToken: ct));
//         if (affected == 0)
//             throw new DomainException("Insufficient stock"); // atomic 防護：不會超賣
//     }
// }

// ─────────────────────────────────────────────────────────────────────────────
// FILE: Domain/Interfaces/IDomainEventDispatcher.cs   (Rich; Anemic: Application/Interfaces/)
// ─────────────────────────────────────────────────────────────────────────────

using YourProject.Domain.Events;

namespace YourProject.Domain.Interfaces;

/// <summary>
/// 將 domain event 派送給 Application 層的 handler。
/// 定義於 Domain，讓同在 Domain 的 IUnitOfWork 能直接參照。
/// 實作位於 Infrastructure（in-process、記憶體內派送）。
/// 事件在持久化交易 commit「之後」才派送——絕不在交易內派送。
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

// ─────────────────────────────────────────────────────────────────────────────
// FILE: Domain/Interfaces/IDomainEventHandler.cs   (Rich; Anemic: Application/Interfaces/)
// ─────────────────────────────────────────────────────────────────────────────

using YourProject.Domain.Events;

namespace YourProject.Domain.Interfaces;

/// <summary>
/// 所有 domain event handler 的介面。
/// 於 Application/EventHandlers/ 為每種事件型別實作。
/// 註冊於 DI，讓 IDomainEventDispatcher 能解析並呼叫。
/// </summary>
public interface IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

// ─────────────────────────────────────────────────────────────────────────────
// FILE: Infrastructure/DomainEventDispatcher.cs  (reference implementation)
// ─────────────────────────────────────────────────────────────────────────────

// using Microsoft.Extensions.DependencyInjection;
// using YourProject.Domain.Events;
// using YourProject.Domain.Interfaces;
//
// public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
// {
//     public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
//     {
//         foreach (var domainEvent in domainEvents)
//         {
//             var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
//             var handlers = serviceProvider.GetServices(handlerType);
//             foreach (var handler in handlers)
//                 await ((dynamic)handler).HandleAsync((dynamic)domainEvent, cancellationToken);
//         }
//     }
// }

// DI 註冊（於 Api 或 Infrastructure 的 DI extension）：
// services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
// services.AddScoped<IDomainEventHandler<OrderCreatedEvent>, OrderCreatedEventHandler>();
// ── 或使用 assembly scanning ───────────────────────────────────────────────────────────
// foreach (var type in typeof(SomeHandler).Assembly.GetExportedTypes()
//     .Where(t => !t.IsAbstract && !t.IsInterface))
// {
//     foreach (var iface in type.GetInterfaces()
//         .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEventHandler<>)))
//         services.AddScoped(iface, type);
// }

// ─────────────────────────────────────────────────────────────────────────────
// Example Application-layer event handler (place in Application/EventHandlers/)
// ─────────────────────────────────────────────────────────────────────────────

// using YourProject.Domain.Interfaces;
// using YourProject.Domain.Events;
//
// public sealed class OrderCreatedEventHandler
//     : IDomainEventHandler<OrderCreatedEvent>
// {
//     public Task HandleAsync(OrderCreatedEvent domainEvent, CancellationToken cancellationToken = default)
//     {
//         // in-process handler 保持在本地端且不承擔關鍵工作，不要在這裡直接呼叫外部系統。
//         // 需要外部副作用時，改由 Application 編排或可重試的背景工作處理。
//         return Task.CompletedTask;
//     }
// }

// ─────────────────────────────────────────────────────────────────────────────
// ─────────────────────────────────────────────────────────────────────────────

// using YourProject.Domain.Common;
// using YourProject.Domain.Entities;
// using YourProject.Domain.Events;
//
// public sealed class Order : AggregateRoot<Guid>
// {
//     private readonly List<OrderItem> _items = [];
//
//     public string CustomerId { get; private set; } = default!;
//     public OrderStatus Status { get; private set; }
//     public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
//
//     // 私有建構子——一律透過 factory method 建立
//     private Order() { }
//
//     public static Result<Order> Create(string customerId)
//     {
//         if (string.IsNullOrWhiteSpace(customerId))
//             return Result<Order>.Fail("Customer ID is required.");
//
//         var order = new Order
//         {
//             Id = Guid.NewGuid(),
//             CustomerId = customerId,
//             Status = OrderStatus.Pending
//         };
//
//         order.AddDomainEvent(new OrderCreatedEvent(order.Id, customerId));
//         return Result<Order>.Ok(order);
//     }
//
//     public Result AddItem(string productId, int quantity, decimal unitPrice)
//     {
//         if (Status != OrderStatus.Pending)
//             return Result.Fail("Cannot add items to a non-pending order.");
//
//         _items.Add(new OrderItem(Guid.NewGuid(), productId, quantity, unitPrice));
//         return Result.Ok();
//     }
//
//     public Result Cancel()
//     {
//         if (Status == OrderStatus.Cancelled)
//             return Result.Fail("Order is already cancelled.");
//
//         Status = OrderStatus.Cancelled;
//         AddDomainEvent(new OrderCancelledEvent(Id));
//         return Result.Ok();
//     }
// }

// ─────────────────────────────────────────────────────────────────────────────
// Example: Domain Event (place in Domain/Events/)
// ─────────────────────────────────────────────────────────────────────────────

// public sealed record OrderCreatedEvent(Guid OrderId, string CustomerId) : DomainEvent;
// public sealed record OrderCancelledEvent(Guid OrderId) : DomainEvent;
