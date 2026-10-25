using FluentValidation;

namespace Hems_Backend.Application.Dashboard.Queries.GetPowerMetrics;

public class GetPowerAndEnergyMetricsQueryValidator : AbstractValidator<GetPowerAndEnergyMetricsQuery>
{
    public GetPowerAndEnergyMetricsQueryValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty()
            .WithMessage("DeviceId is required");

        RuleFor(x => x.Period)
            .NotEmpty()
            .WithMessage("Period is required")
            .Must(p => p.ToLower() == "day" || p.ToLower() == "week" || p.ToLower() == "month" || p.ToLower() == "year")
            .WithMessage("Period must be one of: day, week, month, year");

        // Validation for period=day
        When(x => x.Period.ToLower() == "day", () =>
        {
            RuleFor(x => x.Date)
                .NotEmpty()
                .WithMessage("Date is required when period is 'day'")
                .Must(date => date.HasValue && date.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Date cannot be in the future");
        });

        // Validation for period=week
        When(x => x.Period.ToLower() == "week", () =>
        {
            RuleFor(x => x.Year)
                .NotEmpty()
                .WithMessage("Year is required when period is 'week'")
                .GreaterThan(2000)
                .LessThanOrEqualTo(DateTime.UtcNow.Year)
                .WithMessage("Year must be between 2000 and current year");

            RuleFor(x => x.Week)
                .NotEmpty()
                .WithMessage("Week is required when period is 'week'")
                .InclusiveBetween(1, 53)
                .WithMessage("Week must be between 1 and 53");
        });

        // Validation for period=month
        When(x => x.Period.ToLower() == "month", () =>
        {
            RuleFor(x => x.Year)
                .NotEmpty()
                .WithMessage("Year is required when period is 'month'")
                .GreaterThan(2000)
                .LessThanOrEqualTo(DateTime.UtcNow.Year)
                .WithMessage("Year must be between 2000 and current year");

            RuleFor(x => x.Month)
                .NotEmpty()
                .WithMessage("Month is required when period is 'month'")
                .InclusiveBetween(1, 12)
                .WithMessage("Month must be between 1 and 12");
        });

        // Validation for period=year
        When(x => x.Period.ToLower() == "year", () =>
        {
            RuleFor(x => x.Year)
                .NotEmpty()
                .WithMessage("Year is required when period is 'year'")
                .GreaterThan(2000)
                .LessThanOrEqualTo(DateTime.UtcNow.Year)
                .WithMessage("Year must be between 2000 and current year");
        });
    }
}
