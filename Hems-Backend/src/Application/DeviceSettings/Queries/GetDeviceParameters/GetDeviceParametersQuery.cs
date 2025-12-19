using Hems_Backend.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceSettings.Queries.GetDeviceParameters;

public record GetDeviceParametersQuery : IRequest<List<DeviceParameterDto>>
{
    public Guid SetupDeviceId { get; init; }
}

public class GetDeviceParametersQueryHandler : IRequestHandler<GetDeviceParametersQuery, List<DeviceParameterDto>>
{
    private readonly IApplicationDbContext _context;

    public GetDeviceParametersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DeviceParameterDto>> Handle(GetDeviceParametersQuery request, CancellationToken cancellationToken)
    {
        var parameters = await _context.SetupDeviceParameterValues
            .Where(p => p.SetupDeviceId == request.SetupDeviceId)
            .Include(p => p.Parameter)
            .ThenInclude(p => p.ParameterGroup)
            .OrderBy(p => p.Parameter.ParameterGroup.SortOrder)
            .ThenBy(p => p.SortOrder)
            .Select(p => new DeviceParameterDto
            {
                Id = p.Id,
                ParameterId = p.ParameterId,
                ParameterName = p.Parameter.Name,
                ParameterDescription = p.Parameter.Description,
                ParameterGroupId = p.ParameterGroupId,
                ParameterGroupName = p.ParameterGroup != null ? p.ParameterGroup.Name : null,
                Value = p.Value,
                ElementType = p.Parameter.ElementType,
                Unit = p.Parameter.Unit,
                DefaultValue = p.Parameter.DefaultValue,
                SortOrder = p.SortOrder,
                ModelNumber = p.ModelNumber,
                LastModified = p.LastModified,
                LastModifiedBy = p.LastModifiedBy
            })
            .ToListAsync(cancellationToken);

        return parameters;
    }
}
