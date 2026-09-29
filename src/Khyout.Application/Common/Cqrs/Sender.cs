using System.Collections;
using System.Diagnostics;
using FluentValidation;
using FluentValidation.Results;
using Khyout.Application.Common.Cqrs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Khyout.Application.Common.Cqrs;

/// <summary>
/// Dispatches commands/queries to their registered handlers, running FluentValidation
/// validators first. Handlers and validators are resolved from the current scope.
/// </summary>
public sealed class Sender(IServiceProvider provider, ILogger<Sender> logger) : ISender
{
    public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
        => Execute<TResponse>(command, cancellationToken);

    public Task<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
        => Execute<TResponse>(query, cancellationToken);

    private async Task<TResponse> Execute<TResponse>(object request, CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var stopwatch = Stopwatch.StartNew();

        await ValidateAsync(request, requestType, cancellationToken);

        Type handlerType;
        if (request is ICommand<TResponse>)
        {
            handlerType = typeof(ICommandHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        }
        else
        {
            handlerType = typeof(IQueryHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        }

        var handler = provider.GetService(handlerType)
                      ?? throw new InvalidOperationException($"No handler registered for '{requestType.Name}'.");

        var result = handler.GetType().GetMethod("Handle")!
            .Invoke(handler, new[] { request, cancellationToken });
        var response = await (Task<TResponse>)result!;

        logger.LogDebug("{Request} handled in {ElapsedMs} ms", requestType.Name, stopwatch.ElapsedMilliseconds);
        return response;
    }

    private async Task ValidateAsync(object request, Type requestType, CancellationToken cancellationToken)
    {
        var validatorType = typeof(IValidator<>).MakeGenericType(requestType);
        var validators = (IEnumerable?)provider.GetServices(validatorType) ?? Array.Empty<object>();

        var failures = new List<ValidationFailure>();
        foreach (var validatorObject in validators)
        {
            var validator = (IValidator)validatorObject;
            var result = await validator.ValidateAsync(new ValidationContext<object>(request), cancellationToken);
            if (!result.IsValid)
            {
                failures.AddRange(result.Errors);
            }
        }

        if (failures.Count > 0)
        {
            throw new RequestValidationException(failures);
        }
    }
}
