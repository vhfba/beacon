namespace CentralServer.Application.UseCases;

using CentralServer.Application.Abstractions;
using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

public class DeleteProbeUseCase
{
    private readonly IProbeRepository _probeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProbeUseCase(IProbeRepository probeRepository, IUnitOfWork unitOfWork)
    {
        _probeRepository = probeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(string probeId, CancellationToken cancellationToken = default)
    {
        var existing = await UseCaseGuards.GetRequiredProbeAsync(_probeRepository, probeId, cancellationToken);

        await _probeRepository.DeleteAsync(existing.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
