using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Domain.Entities;
using MediatR;
using System.Text.Json;
using DeviceDataEntity = Hems_Backend.Domain.Entities.DeviceData;

namespace Hems_Backend.Application.DeviceData.Commands.SaveDeviceData;

public record SaveDeviceDataCommand : IRequest<SaveDeviceDataResult>
{
    public Guid DeviceId { get; init; }
    public Guid ComponentId { get; init; }
    public string DataType { get; init; } = string.Empty;
    public JsonDocument Data { get; init; } = JsonDocument.Parse("{}");
    public DateTime RequestDate { get; init; }
}

public record SaveDeviceDataResult
{
    public bool IsSuccess { get; init; }
    public int DeviceDataId { get; init; }
    public string? ErrorMessage { get; init; }
}

public class SaveDeviceDataCommandHandler : IRequestHandler<SaveDeviceDataCommand, SaveDeviceDataResult>
{
    private readonly IApplicationDbContext _context;

    public SaveDeviceDataCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SaveDeviceDataResult> Handle(SaveDeviceDataCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var deviceData = new DeviceDataEntity
            {
                DeviceId = request.DeviceId,
                DataType = request.DataType,
                Data = request.Data,
                RequestDate = request.RequestDate,
                Created = DateTime.UtcNow,
                CreatedBy = "MobileDashboard" // Mobile dashboard'dan gelen veri
            };

            _context.DeviceData.Add(deviceData);
            await _context.SaveChangesAsync(cancellationToken);

            return new SaveDeviceDataResult
            {
                IsSuccess = true,
                DeviceDataId = deviceData.Id
            };
        }
        catch (Exception ex)
        {
            return new SaveDeviceDataResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
