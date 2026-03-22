namespace Petatu.Application.Common.Abstractions;

public interface ICommandHandler<in TCommand>
    : IRequestHandler<TComand, Reult>
    where TComand : ICommand;

public interface ICommandHandler<in TCommand, TResponse>
    : IReqiestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>;
