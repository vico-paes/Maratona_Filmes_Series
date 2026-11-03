using Microsoft.AspNetCore.Diagnostics;

namespace Maratona.Api.Infrastructure;

public class UpstreamExceptionHandler(ILogger<UpstreamExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is not (HttpRequestException or TaskCanceledException) || context.RequestAborted.IsCancellationRequested)
        {
            return false;
        }
        logger.LogWarning(exception, "Falha ao consultar serviço externo");
        context.Response.StatusCode = StatusCodes.Status502BadGateway;
        await context.Response.WriteAsJsonAsync(new
        {
            title = "Serviço externo indisponível",
            status = StatusCodes.Status502BadGateway
        }, ct);
        return true;
    }
}
