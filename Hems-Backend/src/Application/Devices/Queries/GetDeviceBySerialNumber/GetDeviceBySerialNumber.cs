using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Devices.Commands.CreateDevice;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.Devices.Queries.GetDeviceBySerialNumber;

public record GetDeviceBySerialNumberQuery(string SerialNumber) : IRequest<CreateDeviceResult?>;

public class GetDeviceBySerialNumberQueryHandler : IRequestHandler<GetDeviceBySerialNumberQuery, CreateDeviceResult?>
{
    private readonly IApplicationDbContext _context;

    public GetDeviceBySerialNumberQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateDeviceResult?> Handle(GetDeviceBySerialNumberQuery request, CancellationToken cancellationToken)
    {
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.SerialNumber == request.SerialNumber, cancellationToken);

        if (device is null)
            return null;

        return new CreateDeviceResult
        {
            DeviceId = device.Id,
            SerialNumber = device.SerialNumber,
            TypeNumber = device.TypeNumber,
            ThingsBoardAccessToken = device.ThingsBoardAccessToken ?? string.Empty
        };
    }
}
