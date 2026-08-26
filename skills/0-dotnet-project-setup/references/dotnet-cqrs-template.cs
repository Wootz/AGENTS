// ============================================================================
// 自建 CQRS 核心介面、Dispatcher 實作與自動註冊擴充 (不使用 MediatR)
// 範本支援：.NET 10 Primary Constructors & 1-based/2-based 開放泛型自動掃描
// ----------------------------------------------------------------------------
// 放置位置：整檔複製為 <ProjectName>.Application/Cqrs/CQRS.cs，命名空間
//           <ProjectName>.Application.Cqrs。business command/query/handler 放在
//           Application 的 Commands/、Queries/ 子資料夾。
// ============================================================================

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace YourProject.Application.Cqrs;

public interface ICommand { }
public interface ICommand<out TResult> { }

public interface ICommandHandler<in TCommand>
    where TCommand : class, ICommand
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : class, ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface IQuery<out TResult> { }

public interface IQueryHandler<in TQuery, TResult>
    where TQuery : class, IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}

public interface ICommandDispatcher
{
    Task DispatchAsync(ICommand command, CancellationToken cancellationToken = default);

    Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default);
}

public sealed class CommandDispatcher(IServiceProvider serviceProvider) : ICommandDispatcher
{
    public Task DispatchAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        dynamic handler = serviceProvider.GetRequiredService(
            typeof(ICommandHandler<>).MakeGenericType(command.GetType()));
        return handler.HandleAsync((dynamic)command, cancellationToken);
    }

    public Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        dynamic handler = serviceProvider.GetRequiredService(
            typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult)));
        return handler.HandleAsync((dynamic)command, cancellationToken);
    }
}

public interface IQueryDispatcher
{
    Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
}

public sealed class QueryDispatcher(IServiceProvider serviceProvider) : IQueryDispatcher
{
    public Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        dynamic handler = serviceProvider.GetRequiredService(
            typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult)));
        return handler.HandleAsync((dynamic)query, cancellationToken);
    }
}

public static class CqrsDependencyInjection
{
    public static IServiceCollection AddCqrs(this IServiceCollection services, params Assembly[] handlerAssemblies)
    {
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        if (handlerAssemblies == null || handlerAssemblies.Length == 0)
        {
            return services;
        }

        foreach (var assembly in handlerAssemblies)
        {
            var concreteTypes = assembly.GetExportedTypes().Where(t => t is { IsAbstract: false, IsInterface: false });

            foreach (var type in concreteTypes)
            {
                var interfaces = type.GetInterfaces().Where(i => i.IsGenericType);

                foreach (var iface in interfaces)
                {
                    var genericTypeDefinition = iface.GetGenericTypeDefinition();
                    if (genericTypeDefinition == typeof(ICommandHandler<>) ||
                        genericTypeDefinition == typeof(ICommandHandler<,>) ||
                        genericTypeDefinition == typeof(IQueryHandler<,>))
                    {
                        services.AddScoped(iface, type);
                    }
                }
            }
        }

        return services;
    }
}