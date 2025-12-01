namespace Hems_Backend.Application.Common.Models;

public record SocResponseDto
{
    public string TimestampUtc { get; init; } = string.Empty;
    public int Version { get; init; }
    public Guid DeviceId { get; init; }
    public List<InverterBatChannelSocDto> InverterBatChannelSoc { get; init; } = new();
    public List<BmsSocDetailDto> bmsSocDetails { get; init; } = new();
}

public record InverterBatChannelSocDto
{
    public int ChannelId { get; init; }
    public double socPercent { get; init; }
}

public record BmsSocDetailDto
{
    public int ChannelId { get; init; }
    public string DeviceId { get; init; } = string.Empty;
    public double socPercent { get; init; }
}
