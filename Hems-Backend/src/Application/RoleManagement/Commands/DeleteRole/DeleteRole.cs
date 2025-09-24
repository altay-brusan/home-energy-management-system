using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Common.Interfaces;
using MediatR;

namespace Hems_Backend.Application.RoleManagement.Commands.DeleteRole;

public record DeleteRoleCommand(string RoleName) : IRequest<Result>;

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result>
{
    private readonly IIdentityService _identityService;

    public DeleteRoleCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.DeleteRoleAsync(request.RoleName);
    }
}
