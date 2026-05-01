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
        var probe = await _probeRepository.GetByIdAsync(new ProbeId(probeId), cancellationToken);
        if (probe is null)
            throw new DomainException($"Probe {probeId} not found");

        probe.RecordConfigFetch();
        await _probeRepository.UpdateAsync(probe, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
