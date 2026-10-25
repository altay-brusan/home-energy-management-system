using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;

namespace Hems_Backend.Application.Users.Commands.UpdateUser;

public record UpdateUserCommand : IRequest<Result>
{
    public string? FirstName { get; init; }
    public string? Surname { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
}

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result>
{
    private readonly IIdentityService _identityService;
    private readonly IUser _user;

    public UpdateUserCommandHandler(IIdentityService identityService, IUser user)
    {
        _identityService = identityService;
        _user = user;
    }

    public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var userId = _user.Id ?? throw new UnauthorizedAccessException("User not authenticated");

        return await _identityService.UpdateUserAsync(
            userId,
            request.FirstName,
            request.Surname,
            request.Email,
            request.PhoneNumber,
            cancellationToken);
    }
}
