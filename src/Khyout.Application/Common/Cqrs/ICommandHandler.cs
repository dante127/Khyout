namespace Khyout.Application.Common.Cqrs;

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken);
}
