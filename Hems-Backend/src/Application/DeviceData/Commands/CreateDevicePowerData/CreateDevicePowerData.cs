using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Constants;
using Hems_Backend.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceData.Commands.CreateDevicePowerData;

public record CreateDevicePowerDataCommand : IRequest<int>
{
    public string DataType { get; init; } = string.Empty;
    public Guid DeviceId { get; init; }
    public DateTime DataDateTime { get; init; }
    public int? Version { get; init; }
    
    // PV Power (W)
    public double? PvTotalPowerW { get; init; }
    public double? PvMppt1PowerW { get; init; }
    public double? PvMppt2PowerW { get; init; }
    public double? PvMppt3PowerW { get; init; }
    public double? PvMppt4PowerW { get; init; }
    
    // Grid Power (W) - Positive = import, Negative = export
    public double? GridTotalPowerW { get; init; }
    
    // Load Power (W)
    public double? LoadTotalPowerW { get; init; }
    
    // Battery Power (W) - Positive = discharge, Negative = charge
    public double? BatteryPowerW { get; init; }
    
    // Generator Power (W)
    public double? GeneratorTotalPowerW { get; init; }
}

public class CreateDevicePowerDataCommandHandler : IRequestHandler<CreateDevicePowerDataCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateDevicePowerDataCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateDevicePowerDataCommand request, CancellationToken cancellationToken)
    {
        // Check if device exists (DeviceId is actually ThingsBoardDeviceId)
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.ThingsBoardDeviceId == request.DeviceId && !d.IsDeleted, cancellationToken);

        if (device == null)
        {
            throw new ArgumentException($"Device with ThingsBoardDeviceId {request.DeviceId} not found.");
        }

        // Check for existing record with same device, datetime and data type
        var existingRecord = await _context.DevicePowerData
            .FirstOrDefaultAsync(x => 
                x.ThingsBoardDeviceId == request.DeviceId && 
                x.DataDateTime == request.DataDateTime && 
                x.DataType == request.DataType, 
                cancellationToken);

        if (existingRecord != null)
        {
            // Update existing record
            existingRecord.Version = request.Version;
            existingRecord.PvTotalPowerW = request.PvTotalPowerW;
            existingRecord.PvMppt1PowerW = request.PvMppt1PowerW;
            existingRecord.PvMppt2PowerW = request.PvMppt2PowerW;
            existingRecord.PvMppt3PowerW = request.PvMppt3PowerW;
            existingRecord.PvMppt4PowerW = request.PvMppt4PowerW;
            existingRecord.GridTotalPowerW = request.GridTotalPowerW;
            existingRecord.LoadTotalPowerW = request.LoadTotalPowerW;
            existingRecord.BatteryPowerW = request.BatteryPowerW;
            existingRecord.GeneratorTotalPowerW = request.GeneratorTotalPowerW;
            existingRecord.LastModified = DateTime.UtcNow;
            existingRecord.LastModifiedBy = "DeviceSimulator";
            
            await _context.SaveChangesAsync(cancellationToken);
            return existingRecord.Id;
        }
        else
        {
            // Create new record
            var powerData = new DevicePowerData
            {
                ThingsBoardDeviceId = request.DeviceId,
                DataDateTime = request.DataDateTime,
                DataType = request.DataType,
                MessageType = MessageTypes.Telemetry,
                PhaseType = PhaseTypes.SinglePhase,
                Version = request.Version,
                
                PvTotalPowerW = request.PvTotalPowerW,
                PvMppt1PowerW = request.PvMppt1PowerW,
                PvMppt2PowerW = request.PvMppt2PowerW,
                PvMppt3PowerW = request.PvMppt3PowerW,
                PvMppt4PowerW = request.PvMppt4PowerW,
                GridTotalPowerW = request.GridTotalPowerW,
                LoadTotalPowerW = request.LoadTotalPowerW,
                BatteryPowerW = request.BatteryPowerW,
                GeneratorTotalPowerW = request.GeneratorTotalPowerW,
                
                Created = DateTime.UtcNow,
                CreatedBy = "DeviceSimulator"
            };

            _context.DevicePowerData.Add(powerData);
            await _context.SaveChangesAsync(cancellationToken);
            
            return powerData.Id;
        }
    }
}
