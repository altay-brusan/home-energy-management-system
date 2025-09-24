namespace Hems_Backend.Application.Common.Interfaces;

public interface ISmartHouseService
{
    Task<bool> IpAddressExistsAsync(string ipAddress, Guid? excludeId = null, CancellationToken cancellationToken = default);
}












