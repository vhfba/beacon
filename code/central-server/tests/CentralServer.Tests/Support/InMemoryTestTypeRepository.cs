namespace CentralServer.Tests.Support;

using CentralServer.Domain.Models;
using CentralServer.Domain.Repositories;

internal sealed class InMemoryTestTypeRepository : ITestTypeRepository
{
    private readonly Dictionary<string, TestType> _types = new(StringComparer.OrdinalIgnoreCase);

    public Task<TestType?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        _types.TryGetValue(name, out var value);
        return Task.FromResult(value);
    }

    public Task<IReadOnlyList<TestType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<TestType>>(_types.Values.ToList());
    }

    public Task<TestType> CreateAsync(TestType testType, CancellationToken cancellationToken = default)
    {
        _types[testType.Name] = testType;
        return Task.FromResult(testType);
    }
}
