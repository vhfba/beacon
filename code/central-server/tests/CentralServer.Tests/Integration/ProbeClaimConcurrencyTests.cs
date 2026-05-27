using CentralServer.Domain.Models;
using CentralServer.Infrastructure.Persistence;
using CentralServer.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CentralServer.Tests.Integration;

public class ProbeClaimConcurrencyTests
{
    private DbContextOptions<CentralServerDbContext> GetOptions(string dbName)
    {
        return new DbContextOptionsBuilder<CentralServerDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
    }

    [Fact]
    public async Task ClaimPendingForProbeAsync_ConcurrentCalls_DoNotDoubleClaim()
    {
        var options = GetOptions(Guid.NewGuid().ToString());
        var probeId = new ProbeId("concurrent-test-probe");

        using (var setupContext = new CentralServerDbContext(options, null))
        {
            var probeEntity = new Infrastructure.Persistence.Entities.ProbeEntity
            {
                Id = probeId.Value,
                Status = ProbeStatus.Active,
                RegisteredAt = DateTime.UtcNow
            };
            setupContext.Probes.Add(probeEntity);

            setupContext.ProbeControlCommands.Add(new Infrastructure.Persistence.Entities.ProbeControlCommandEntity
            {
                CommandId = Guid.NewGuid().ToString(),
                ProbeId = probeId.Value,
                Status = ProbeControlCommandStatus.Pending,
                RequestedAtUtc = DateTime.UtcNow
            });
            await setupContext.SaveChangesAsync();
        }

        var results = new List<IReadOnlyList<ProbeControlCommand>>();

        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            using var context = new CentralServerDbContext(options, null);
            var repo = new ProbeControlCommandRepositoryAdapter(context);
            var claimed = await repo.ClaimPendingForProbeAsync(probeId, 1);
            if (claimed.Count > 0)
            {
                await context.SaveChangesAsync();
            }
            lock (results)
            {
                results.Add(claimed);
            }
        });

        await Task.WhenAll(tasks);

        var totalClaimed = results.SelectMany(x => x).Count();
        Assert.Equal(1, totalClaimed);
    }
}
