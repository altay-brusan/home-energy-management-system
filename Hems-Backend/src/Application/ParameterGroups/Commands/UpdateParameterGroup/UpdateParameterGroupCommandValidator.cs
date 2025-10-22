using FluentValidation;

namespace Hems_Backend.Application.ParameterGroups.Commands.UpdateParameterGroup;

public class UpdateParameterGroupCommandValidator : AbstractValidator<UpdateParameterGroupCommand>
{
    public UpdateParameterGroupCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty().WithMessage("ID zorunludur.");

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Grup adı zorunludur.")
            .MaximumLength(200).WithMessage("Grup adı 200 karakterden fazla olamaz.");

        RuleFor(v => v.Description)
            .MaximumLength(500).WithMessage("Açıklama 500 karakterden fazla olamaz.");

        RuleFor(v => v.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sıralama değeri 0'dan küçük olamaz.");

        RuleFor(v => v.Version)
            .NotEmpty().WithMessage("Versiyon zorunludur.")
            .MaximumLength(50).WithMessage("Versiyon 50 karakterden fazla olamaz.");
    }
}
