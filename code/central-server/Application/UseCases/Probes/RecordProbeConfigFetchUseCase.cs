namespace CentralServer.Application.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class RecordProbeConfigFetchUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RecordProbeConfigFetchUseCase(IProbeRepository probeRepository, IUnitOfWork unitOfWork)
    {
        _probeRepository = probeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(string probeId, CancellationToken cancellationToken = default)
    {
        var probe = await UseCaseGuards.GetRequiredProbeAsync(_probeRepository, probeId, cancellationToken);

        probe.RecordConfigFetch();
        await _probeRepository.UpdateAsync(probe, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
