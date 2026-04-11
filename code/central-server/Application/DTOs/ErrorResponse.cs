namespace CentralServer.Application.DTOs;
public record ErrorResponse
{
    public string Message { get; init; } = string.Empty;
    public string? Code { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
