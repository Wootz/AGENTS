// ============================================================================
// DDD Domain Layer Templates
// Scope: Domain project only — zero framework dependencies
// Place files in src/<ProjectName>.Domain/ following the directory structure
// ============================================================================

// ─────────────────────────────────────────────────────────────────────────────
// FILE: Domain/Events/IDomainEvent.cs
// ─────────────────────────────────────────────────────────────────────────────

namespace YourProject.Domain.Events;

/// <summary>
/// Marker interface for all domain events.
/// Domain events are raised inside aggregates and dispatched after the
/// transaction commits via IDomainEventDispatcher (Domain layer).
/// </summary>
public interface IDomainEvent
{
    /// <summary>UTC timestamp when the event occurred.</summary>
    DateTime OccurredOn { get; }
}

// ─────────────────────────────────────────────────────────────────────────────
// FILE: Domain/Events/DomainEvent.cs
// ─────────────────────────────────────────────────────────────────────────────

namespace YourProject.Domain.Events;

/// <summary>
/// Base record for domain events. Use as a base for all concrete events.
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
/// Base class for all Aggregate Roots.
/// Holds a collection of uncommitted domain events raised during the current
/// operation. Events are dispatched by the Application layer after the
/// transaction commits — never dispatched inside the domain itself.
/// </summary>
public abstract class AggregateRoot<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Strongly-typed aggregate identity.</summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// Read-only snapshot of uncommitted domain events.
    /// Consumed by IUnitOfWork during CommitAsync to dispatch events after
    /// the persistence transaction commits successfully.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Called inside aggregate methods to record a domain event.
    /// The event is NOT dispatched here — only queued for later dispatch.
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Clears all queued domain events. Called by IUnitOfWork after events
    /// have been dispatched successfully.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}

// ─────────────────────────────────────────────────────────────────────────────
// FILE: Domain/Entities/Entity.cs  (for non-root entities within an aggregate)
// ─────────────────────────────────────────────────────────────────────────────

namespace YourProject.Domain.Entities;

/// <summary>
/// Base class for non-root entities (children within an aggregate).
/// No domain events — only AggregateRoot raises events.
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
/// Result pattern for domain operations.
/// Use instead of throwing exceptions for expected business rule violations.
/// Only throw exceptions for truly unexpected / infrastructure failures.
///
/// Usage (success):  Result.Ok(order)
/// Usage (failure):  Result.Fail&lt;Order&gt;("Order already cancelled.")
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
    /// Map the value if successful; propagate the error otherwise.
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
        => IsSuccess ? Result<TOut>.Ok(mapper(Value!)) : Result<TOut>.Fail(Error);
}

/// <summary>
/// Non-generic Result for void operations.
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
/// Prefer using C# `record` types for Value Objects — they provide structural
/// equality for free with zero boilerplate:
///
///   public sealed record Money(decimal Amount, string Currency);
///   public sealed record EmailAddress(string Value);
///
/// Use this abstract class only when you need custom validation logic in the
/// constructor that cannot live in a record primary constructor.
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
// FILE: Domain/Interfaces/IRepository.cs
//
// Two variants — use the one matching the chosen domain model style.
// ─────────────────────────────────────────────────────────────────────────────

// ════════════════════════════════════════════════════════════════════════════
// RICH DOMAIN MODEL variant — T must extend AggregateRoot<Guid>
// Use when entities extend AggregateRoot<TId> and participate in domain events.
// ════════════════════════════════════════════════════════════════════════════

using YourProject.Domain.Entities;

namespace YourProject.Domain.Interfaces;

/// <summary>
/// Generic repository abstraction for aggregate roots (Rich Domain Model).
/// Defined in the Domain layer so Domain Services can inject repositories directly.
/// Implemented in the Persistence layer (EF Core, Dapper/raw SQL, or another provider).
/// Repositories operate at the aggregate boundary — never expose IQueryable&lt;T&gt; outside Persistence.
/// </summary>
public interface IRepository<T> where T : AggregateRoot<Guid>
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}

// ════════════════════════════════════════════════════════════════════════════
// ANEMIC DOMAIN MODEL variant — T is a plain POCO class
// Replace the Rich variant above with this version when using Anemic model.
// No import of YourProject.Domain.Entities is needed.
// ════════════════════════════════════════════════════════════════════════════

// namespace YourProject.Domain.Interfaces;
//
// /// <summary>
// /// Generic repository abstraction for plain entity POCOs (Anemic Domain Model).
// /// Defined in the Domain layer so Domain Services in DomainServices/ can inject repositories.
// /// Implemented in the Persistence layer (EF Core, Dapper/raw SQL, or another provider).
// /// </summary>
// public interface IRepository<T> where T : class
// {
//     Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
//     Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
//     Task AddAsync(T entity, CancellationToken cancellationToken = default);
//     void Update(T entity);
//     void Remove(T entity);
// }

// ─────────────────────────────────────────────────────────────────────────────
// FILE: Domain/Interfaces/IUnitOfWork.cs
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
    /// Commits all pending persistence operations for the current use case.
    /// Implementations must keep the transaction boundary inside Persistence
    /// and must not expose EF Core or SQL transaction types to Domain/Application.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);
}

// ── Rich EF + Dapper/raw SQL UnitOfWork reference implementation ─────────────

// using System.Data.Common;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Storage;
// using YourProject.Domain.Entities;        // AggregateRoot<T>
// using YourProject.Domain.Interfaces;      // IUnitOfWork, IRepository<T>, IDomainEventDispatcher
//
// public sealed class AppUnitOfWork(AppDbContext dbContext, IDomainEventDispatcher dispatcher)
//     : IUnitOfWork
// {
//     private IDbContextTransaction? _transaction;
//
//     // Persistence repositories that execute Dapper/raw SQL may inject this
//     // concrete AppUnitOfWork and call EnsureTransactionAsync() so EF and raw
//     // SQL share the same connection and transaction. Do not expose this
//     // concrete type to Domain or Application.
//     public async Task<IDbContextTransaction> EnsureTransactionAsync(CancellationToken cancellationToken = default)
//         => _transaction ??= await dbContext.Database.BeginTransactionAsync(cancellationToken);
//
//     // CRITICAL — Connection and CurrentTransaction MUST both come from this same
//     // dbContext. BeginTransactionAsync() opens the transaction ON this connection,
//     // so a Dapper command that (a) executes on THIS Connection and (b) passes THIS
//     // CurrentTransaction is enrolled in the same transaction as EF's SaveChanges.
//     // If a repo opens its own SqlConnection instead of using this Connection,
//     // passing CurrentTransaction either throws ("transaction not associated with
//     // connection") or — worse — the two connections run as independent
//     // transactions and consistency is silently broken. Never new up a connection.
//     public DbConnection Connection => dbContext.Database.GetDbConnection();
//
//     public DbTransaction? CurrentTransaction =>
//         dbContext.Database.CurrentTransaction?.GetDbTransaction();
//
//     public async Task CommitAsync(CancellationToken cancellationToken = default)
//     {
//         // 1. Collect pending domain events before saving.
//         //    NOTE: AggregateRoot<Guid> is hard-coded here. If the project uses a
//         //    different aggregate id type (int, long, a strongly-typed id), change
//         //    this type argument to match — otherwise no events are ever collected
//         //    and the failure is SILENT: everything commits, nothing dispatches.
//         //    With several id types in play, mark aggregates with a non-generic
//         //    IHasDomainEvents interface and query Entries<IHasDomainEvents>().
//         var aggregates = dbContext.ChangeTracker
//             .Entries<AggregateRoot<Guid>>()
//             .Where(e => e.Entity.DomainEvents.Count > 0)
//             .Select(e => e.Entity)
//             .ToList();
//
//         var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();
//
//         // 2. Commit persistence first. An explicit transaction is only needed
//         //    when something OTHER than SaveChangesAsync also writes — a Dapper
//         //    /raw-SQL repository that called EnsureTransactionAsync(). On its
//         //    own SaveChangesAsync is already atomic, so opening a transaction
//         //    for every use case just adds a BEGIN/COMMIT round-trip and holds
//         //    the connection longer.
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
//                 // Rolling back can itself fail — the connection may already be
//                 // gone, or the server may have aborted the transaction. Swallow
//                 // that one: the ORIGINAL exception is what the caller needs to
//                 // see, and letting the rollback's exception propagate would mask
//                 // the actual cause of failure.
//                 try { await _transaction.RollbackAsync(cancellationToken); }
//                 catch { /* keep the original exception as the reported cause */ }
//                 throw;
//             }
//             finally
//             {
//                 // Release it either way: a committed or rolled-back transaction
//                 // cannot be reused, and leaving it set would make a second
//                 // CommitAsync() in the same scope reuse a finished transaction
//                 // (EnsureTransactionAsync's ??= would hand back the dead one),
//                 // throwing "This transaction has completed; it is no longer
//                 // usable". A batch job processing several use cases inside one
//                 // scope hits this immediately.
//                 await _transaction.DisposeAsync();
//                 _transaction = null;
//             }
//         }
//
//         // 3. Dispatch in-process domain events only after the DB transaction
//         //    commits successfully. Keep handlers local and non-critical.
//         //
//         //    ⚠️ The data is ALREADY committed at this point. If a handler throws,
//         //    this method throws with it and the caller sees a failure even though
//         //    the write succeeded — the side effect is lost, not the data. That is
//         //    the deliberate trade-off behind the rule that in-process handlers
//         //    must not perform irreversible or reliability-critical work (email,
//         //    external calls, cross-service notifications). Anything that MUST
//         //    happen belongs in retryable background work keyed off the committed
//         //    state, or in an integration event published through the outbox —
//         //    never here. Swallowing handler exceptions is not the fix either: it
//         //    hides genuine bugs. Let it throw, and keep handlers trivial.
//         if (domainEvents.Count > 0)
//             await dispatcher.DispatchAsync(domainEvents, cancellationToken);
//
//         foreach (var aggregate in aggregates)
//             aggregate.ClearDomainEvents();
//     }
//
//     public async ValueTask DisposeAsync()
//     {
//         // Reached with a live transaction only when the use case never committed
//         // — an exception, or an early return after a Dapper repo opened one.
//         // Disposing an IDbContextTransaction does roll back implicitly, but roll
//         // back explicitly so the intent is in the code rather than in EF's
//         // disposal semantics.
//         if (_transaction is not null)
//         {
//             try { await _transaction.RollbackAsync(); }
//             catch { /* already completed or the connection is gone — nothing to undo */ }
//             await _transaction.DisposeAsync();
//             _transaction = null;
//         }
//     }
// }
//
// DI registration:
// services.AddScoped<AppUnitOfWork>();
// services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppUnitOfWork>());

// ── Usage in a Rich command handler ──────────────────────────────────────────

// await using var uow = serviceProvider.GetRequiredService<IUnitOfWork>();
// var result = aggregate.DoSomething();
// if (result.IsFailure) return result;
// await _repository.AddAsync(aggregate, cancellationToken);
// await uow.CommitAsync(cancellationToken); // commit persistence, then dispatch domain events

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
// using YourProject.Domain.Interfaces;      // IUnitOfWork
//
// public sealed class AppUnitOfWork(AppDbContext dbContext) : IUnitOfWork
// {
//     private IDbContextTransaction? _transaction;
//
//     // Persistence repositories that execute Dapper/raw SQL may inject this
//     // concrete AppUnitOfWork and call EnsureTransactionAsync() so EF and raw
//     // SQL share the same connection and transaction. Do not expose this
//     // concrete type to Domain or Application.
//     public async Task<IDbContextTransaction> EnsureTransactionAsync(CancellationToken cancellationToken = default)
//         => _transaction ??= await dbContext.Database.BeginTransactionAsync(cancellationToken);
//
//     // CRITICAL — Connection and CurrentTransaction MUST both come from this same
//     // dbContext (see the Rich implementation's note). Dapper repos execute on this
//     // Connection and pass this CurrentTransaction; never new up a separate connection.
//     public DbConnection Connection => dbContext.Database.GetDbConnection();
//
//     public DbTransaction? CurrentTransaction =>
//         dbContext.Database.CurrentTransaction?.GetDbTransaction();
//
//     public async Task CommitAsync(CancellationToken cancellationToken = default)
//     {
//         // An explicit transaction is only needed when something OTHER than
//         // SaveChangesAsync also writes — i.e. a Dapper/raw-SQL repository called
//         // EnsureTransactionAsync(). SaveChangesAsync is already atomic on its
//         // own, so opening a transaction unconditionally only costs a BEGIN/COMMIT
//         // round-trip and holds the connection longer.
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
//             // Rolling back can itself fail (connection already gone, transaction
//             // aborted server-side). Swallow that one — the ORIGINAL exception is
//             // the cause the caller needs; letting the rollback's exception
//             // propagate would mask it.
//             try { await _transaction.RollbackAsync(cancellationToken); }
//             catch { /* keep the original exception as the reported cause */ }
//             throw;
//         }
//         finally
//         {
//             // Release it either way — a completed transaction cannot be reused,
//             // and leaving it set would make a second CommitAsync() in the same
//             // scope reuse a dead transaction via EnsureTransactionAsync's ??=,
//             // throwing "This transaction has completed; it is no longer usable".
//             // A batch job running several use cases inside one scope hits this
//             // immediately.
//             await _transaction.DisposeAsync();
//             _transaction = null;
//         }
//     }
//
//     public async ValueTask DisposeAsync()
//     {
//         // Only reached with a live transaction when the use case never committed
//         // — an exception, or an early return after a Dapper repo opened one.
//         // Disposal rolls back implicitly, but do it explicitly so the intent is
//         // in the code rather than in EF's disposal semantics.
//         if (_transaction is not null)
//         {
//             try { await _transaction.RollbackAsync(); }
//             catch { /* already completed or the connection is gone */ }
//             await _transaction.DisposeAsync();
//             _transaction = null;
//         }
//     }
// }
//
// DI registration:
// services.AddScoped<AppUnitOfWork>();
// services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppUnitOfWork>());

// ── Usage in an Anemic command handler ───────────────────────────────────────

// await using var uow = serviceProvider.GetRequiredService<IUnitOfWork>();
// await _repository.AddAsync(entity, cancellationToken);
// otherRepository.Update(otherEntity);
// await uow.CommitAsync(cancellationToken); // the only commit point for the use case

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
// using YourProject.Persistence.UnitOfWork;   // concrete AppUnitOfWork (Persistence-internal)
//
// public sealed class ProductStockRepository(AppUnitOfWork uow)
// {
//     public async Task DeductStockAsync(Guid productId, int qty, CancellationToken ct)
//     {
//         await uow.EnsureTransactionAsync(ct);            // (1) ensure tx is open
//         var affected = await uow.Connection.ExecuteAsync(new CommandDefinition(
//             "UPDATE Products SET Stock = Stock - @qty WHERE Id = @productId AND Stock >= @qty",
//             new { productId, qty },
//             transaction: uow.CurrentTransaction,         // (2)+(3) same connection + tx
//             cancellationToken: ct));
//         if (affected == 0)
//             throw new DomainException("Insufficient stock"); // atomic guard: no oversell
//     }
// }

// ─────────────────────────────────────────────────────────────────────────────
// FILE: Domain/Interfaces/IDomainEventDispatcher.cs
// ─────────────────────────────────────────────────────────────────────────────

using YourProject.Domain.Events;

namespace YourProject.Domain.Interfaces;

/// <summary>
/// Dispatches domain events to their Application-layer handlers.
/// Defined in Domain so IUnitOfWork (also in Domain) can reference it directly.
/// Implemented in Infrastructure (in-process, in-memory dispatch).
/// Events are dispatched AFTER the persistence transaction commits — never inside it.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

// ─────────────────────────────────────────────────────────────────────────────
// FILE: Domain/Interfaces/IDomainEventHandler.cs
// ─────────────────────────────────────────────────────────────────────────────

using YourProject.Domain.Events;

namespace YourProject.Domain.Interfaces;

/// <summary>
/// Marker interface for all domain event handlers.
/// Implement in Application/EventHandlers/ for each event type.
/// Registered in DI so IDomainEventDispatcher can resolve and invoke them.
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

// DI registration (in WebApi or Infrastructure DI extension):
// services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
// services.AddScoped<IDomainEventHandler<OrderCreatedEvent>, OrderCreatedEventHandler>();
// ── or use assembly scanning ─────────────────────────────────────────────────────────────
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
//         // Keep in-process handlers local and non-critical. Do not call external
//         // systems directly here. Use Application orchestration or retryable
//         // background work when external side effects are required.
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
//     // Private constructor — use factory method for creation
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
