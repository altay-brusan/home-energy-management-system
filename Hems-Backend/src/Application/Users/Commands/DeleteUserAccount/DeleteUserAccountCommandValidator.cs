using FluentValidation;

namespace Hems_Backend.Application.Users.Commands.DeleteUserAccount;

public class DeleteUserAccountCommandValidator : AbstractValidator<DeleteUserAccountCommand>
{
    public DeleteUserAccountCommandValidator()
    {
        // No properties to validate — the command operates on the current authenticated user.
    }
}
