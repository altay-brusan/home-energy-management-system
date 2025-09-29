using Hems_Backend.Application.Common.Interfaces;
using MediatR;

namespace Hems_Backend.Application.RoleManagement.Queries.GetUserRoles;

public record GetUserRolesQuery(string UserId) : IRequest<UserRolesVm>;

public class GetUserRolesQueryHandler : IRequestHandler<GetUserRolesQuery, UserRolesVm>
{
    private readonly IIdentityService _identityService;

    public GetUserRolesQueryHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<UserRolesVm> Handle(GetUserRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await _identityService.GetUserRolesAsync(request.UserId);
        
        return new UserRolesVm 
        { 
            UserId = request.UserId,
            Roles = roles 
        };
    }
}

public class UserRolesVm
{
    public string UserId { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
}























