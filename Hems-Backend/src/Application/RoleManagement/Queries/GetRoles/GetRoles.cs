using Hems_Backend.Application.Common.Interfaces;
using MediatR;

namespace Hems_Backend.Application.RoleManagement.Queries.GetRoles;

public record GetRolesQuery : IRequest<RolesVm>;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, RolesVm>
{
    private readonly IIdentityService _identityService;

    public GetRolesQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<RolesVm> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _identityService.GetAllRolesAsync();
        
        var roleDtos = roles.Select(roleName => new RoleDto
        {
            Name = roleName
        }).ToList();

        return new RolesVm { Roles = roleDtos };
    }
}

public class RolesVm
{
    public IList<RoleDto> Roles { get; set; } = new List<RoleDto>();
}

public class RoleDto
{
    public string Name { get; set; } = string.Empty;
}
