using FluentValidation;

namespace Hems_Backend.Application.ParameterGroups.Commands.CreateParameterGroup;

public class CreateParameterGroupCommandValidator : AbstractValidator<CreateParameterGroupCommand>
{
    public CreateParameterGroupCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Grup adı zorunludur.")
            .MaximumLength(200).WithMessage("Grup adı 200 karakterden fazla olamaz.");

        RuleFor(v => v.Description)
            .MaximumLength(500).WithMessage("Açıklama 500 karakterden fazla olamaz.");

        RuleFor(v => v.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sıralama değeri 0'dan küçük olamaz.");
    }
}
