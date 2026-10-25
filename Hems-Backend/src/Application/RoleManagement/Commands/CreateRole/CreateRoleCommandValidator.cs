using FluentValidation;

namespace Hems_Backend.Application.RoleManagement.Commands.CreateRole;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("Role name is required and must not exceed 50 characters.");
    }
}
