using FluentValidation;

namespace Hems_Backend.Application.Parameters.Commands.CreateParameter;

public class CreateParameterCommandValidator : AbstractValidator<CreateParameterCommand>
{
    public CreateParameterCommandValidator()
    {
        RuleFor(v => v.ParameterGroupId)
            .NotEmpty().WithMessage("Parametre grubu ID'si zorunludur.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Parametre adı zorunludur.")
            .MaximumLength(200).WithMessage("Parametre adı 200 karakterden fazla olamaz.");


        RuleFor(v => v.RoleType)
            .NotEmpty().WithMessage("Rol tipi zorunludur.")
            .MaximumLength(100).WithMessage("Rol tipi 100 karakterden fazla olamaz.");

        RuleFor(v => v.Unit)
            .MaximumLength(50).WithMessage("Birim 50 karakterden fazla olamaz.");

        RuleFor(v => v.DefaultValue)
            .MaximumLength(1000).WithMessage("Varsayılan değer 1000 karakterden fazla olamaz.");

        RuleFor(v => v.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sıralama değeri 0'dan küçük olamaz.");
    }
}
