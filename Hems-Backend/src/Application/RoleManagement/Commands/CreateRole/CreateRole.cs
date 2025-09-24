using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Common.Interfaces;
using MediatR;

namespace Hems_Backend.Application.RoleManagement.Commands.CreateRole;

public record CreateRoleCommand : IRequest<Result>
{
    public string Name { get; init; } = string.Empty;
}

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result>
{
    private readonly IIdentityService _identityService;

    public CreateRoleCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Result> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        return await _identityService.CreateRoleAsync(request.Name);
    }
}
