using FluentValidation;

namespace Hems_Backend.Application.ParameterValueSettingSchemes.Commands.CreateParameterValueSettingScheme;

public class CreateParameterValueSettingSchemeCommandValidator : AbstractValidator<CreateParameterValueSettingSchemeCommand>
{
    public CreateParameterValueSettingSchemeCommandValidator()
    {
        RuleFor(v => v.ParameterId)
            .NotEmpty().WithMessage("Parametre ID'si zorunludur.");

        RuleFor(v => v.Value)
            .NotEmpty().WithMessage("Değer zorunludur.");

        RuleFor(v => v.ModelNumber)
            .NotEmpty().WithMessage("Model numarası zorunludur.")
            .MaximumLength(100).WithMessage("Model numarası 100 karakterden fazla olamaz.");

        RuleFor(v => v.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sıralama değeri 0'dan küçük olamaz.");
    }
}
