using MediatR;
using Microsoft.Extensions.Logging;
using Petatu.Domain.Common;

namespace DefaultNamespace;

internal sealed class RequestLoggingPipelineBehaviour<TRequest, TResponse>(
    ILogger<RequestLoggingPipelineBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Proceed before invoking
        string requestName = typeof(TRequest).Name;

        logger.LogInformation("Processing request {requestName}", requestName);

        TResponse response = await next(cancellationToken);

        // Proceed after invoking
        if (response.IsSuccess)
        {
            logger.LogInformation("Completed request {requestName}", requestName);
        }
        else
        {
            using (LogContext.PushProperty("Error", response.Error, true))
            {
                logger.LogError("Completed request {RequestName} with error", requestName);
            }
        }

        return response;
    }
}
