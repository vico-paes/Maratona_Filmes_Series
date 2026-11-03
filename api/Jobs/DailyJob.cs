using Maratona.Api.Data;
using Maratona.Api.Data.Entities;

namespace Maratona.Api.Jobs;

public abstract class DailyJob(IServiceScopeFactory scopes, ILogger logger) : BackgroundService
{
    private static readonly TimeSpan CheckEvery = TimeSpan.FromMinutes(30);

    protected abstract string Name { get; }

    protected virtual TimeSpan Interval => TimeSpan.FromDays(1);

    protected abstract Task Run(IServiceProvider services, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), ct);
        using var timer = new PeriodicTimer(CheckEvery);
        do
        {
            try
            {
                await RunIfDue(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Job {Name} falhou", Name);
            }
        }
        while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task RunIfDue(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var run = await db.JobRuns.FindAsync([Name], ct);
        if (run is not null && run.LastRunAt > DateTime.UtcNow - Interval)
        {
            return;
        }
        logger.LogInformation("Job {Name} iniciado", Name);
        using (var work = scopes.CreateScope())
        {
            await Run(work.ServiceProvider, ct);
        }
        run ??= db.JobRuns.Add(new JobRun { Name = Name }).Entity;
        run.LastRunAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Job {Name} concluído", Name);
    }
}
