using MediatR;
using Petatu.Domain.Common;

namespace Petatu.Application.Common.Abstractions;

public interface ICommand : IRequest<Result>, IBaseCommand;

public interface ICommand<TResponse> : IRequest<TResponse>, IBaseCommand;

public interface IBaseCommand;
