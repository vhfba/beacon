namespace CentralServer.Application.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Application.DTOs;
using CentralServer.Application.Mappings;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class UpdateProbeStatusUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProbeStatusUseCase(IProbeRepository probeRepository, IUnitOfWork unitOfWork)
    {
        _probeRepository = probeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProbeDTO> ExecuteAsync(string probeId, string newStatus, CancellationToken cancellationToken = default)
    {
        var probe = await UseCaseGuards.GetRequiredProbeAsync(_probeRepository, probeId, cancellationToken);

        if (!Enum.TryParse<ProbeStatus>(newStatus, true, out var status))
            throw new DomainException($"Invalid probe status: {newStatus}");

        probe.UpdateStatus(status);
        await _probeRepository.UpdateAsync(probe, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return probe.ToDto();
    }
}
