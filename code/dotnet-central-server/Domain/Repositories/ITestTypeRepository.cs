namespace CentralServer.Domain.Repositories;

using CentralServer.Domain.Models;
public interface ITestTypeRepository
{
    Task<TestType?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TestType>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TestType> CreateAsync(TestType testType, CancellationToken cancellationToken = default);
}
