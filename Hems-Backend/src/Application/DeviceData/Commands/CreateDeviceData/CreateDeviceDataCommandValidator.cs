using System.Text.Json;
using FluentValidation;
using Hems_Backend.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceData.Commands.CreateDeviceData;

public class CreateDeviceDataCommandValidator : AbstractValidator<CreateDeviceDataCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateDeviceDataCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.DataType)
            .NotEmpty()
            .MaximumLength(100)
            .WithMessage("DataType is required and must not exceed 100 characters.");

        RuleFor(v => v.DeviceId)
            .NotEmpty()
            .MustAsync(BeValidDevice)
            .WithMessage("Device must exist and be active.");

        RuleFor(v => v.RequestDate)
            .NotEmpty()
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("RequestDate cannot be more than 5 minutes in the future.");

        RuleFor(v => v.Data)
            .NotNull()
            .Must(BeValidJsonSize)
            .WithMessage("Data must be valid JSON and not exceed 1MB.");
    }

    private async Task<bool> BeValidDevice(Guid deviceId, CancellationToken cancellationToken)
    {
        return await _context.Devices
            .Include(d => d.SetupDevice!)
                .ThenInclude(sd => sd.Setup!)
                .ThenInclude(s => s.Site!)
            .AnyAsync(d => d.Id == deviceId && !d.IsDeleted, cancellationToken);
    }

    private static bool BeValidJsonSize(JsonDocument data)
    {
        if (data == null) return false;
        
        var jsonString = data.RootElement.GetRawText();
        return jsonString.Length <= 1024 * 1024; // 1MB limit
    }
}

