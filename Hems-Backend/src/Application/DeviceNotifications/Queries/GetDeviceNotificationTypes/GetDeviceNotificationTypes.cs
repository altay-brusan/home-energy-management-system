using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.DeviceNotifications.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceNotifications.Queries.GetDeviceNotificationTypes;

public record GetDeviceNotificationTypesQuery : IRequest<List<DeviceNotificationTypeDto>>;

public class GetDeviceNotificationTypesQueryHandler : IRequestHandler<GetDeviceNotificationTypesQuery, List<DeviceNotificationTypeDto>>
{
    private readonly IApplicationDbContext _context;

    public GetDeviceNotificationTypesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DeviceNotificationTypeDto>> Handle(GetDeviceNotificationTypesQuery request, CancellationToken cancellationToken)
    {
        var notificationTypes = await _context.DeviceNotificationTypes
            .Where(nt => nt.IsActive)
            .OrderBy(nt => nt.Code)
            .Select(nt => new DeviceNotificationTypeDto
            {
                Id = nt.Id,
                Code = nt.Code,
                Message = nt.Message,
                Status = nt.Status,
                Description = nt.Description,
                Register = nt.Register,
                Value = nt.Value,
                Category = nt.Category,
                Version = nt.Version,
                IsActive = nt.IsActive
            })
            .ToListAsync(cancellationToken);

        return notificationTypes;
    }
}
