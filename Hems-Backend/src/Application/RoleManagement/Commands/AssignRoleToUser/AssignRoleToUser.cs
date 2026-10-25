using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Common.Interfaces;
using MediatR;

namespace Hems_Backend.Application.RoleManagement.Commands.AssignRoleToUser;

public record AssignRoleToUserCommand : IRequest<Result>
{
    public string UserId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
}

public class AssignRoleToUserCommandHandler : IRequestHandler<AssignRoleToUserCommand, Result>
{
    private readonly IIdentityService _identityService;

    public AssignRoleToUserCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result> Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.AssignRoleToUserAsync(request.UserId, request.RoleName);
    }
}
