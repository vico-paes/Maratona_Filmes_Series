using Microsoft.AspNetCore.Diagnostics;

namespace Maratona.Api.Infrastructure;

public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (context.RequestAborted.IsCancellationRequested)
        {
            return false;
        }
        var (status, title) = exception switch
        {
            BadHttpRequestException bad => (bad.StatusCode, "Requisição inválida"),
            HttpRequestException or TaskCanceledException => (StatusCodes.Status502BadGateway, "Serviço externo indisponível"),
            _ => (0, "")
        };
        if (status == 0)
        {
            return false;
        }
        if (status == StatusCodes.Status502BadGateway)
        {
            logger.LogWarning(exception, "Falha ao consultar serviço externo");
        }
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { title, status }, ct);
        return true;
    }
}
