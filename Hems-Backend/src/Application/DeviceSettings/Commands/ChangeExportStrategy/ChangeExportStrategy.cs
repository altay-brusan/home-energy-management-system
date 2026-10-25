using System.Text.Json;
using Hems_Backend.Application.Common.Enums;
using Hems_Backend.Application.Common.Exceptions;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hems_Backend.Application.DeviceSettings.Commands.ChangeExportStrategy;

public record ChangeExportStrategyCommand : IRequest<ExportStrategyStateDto?>
{
    public Guid DeviceId { get; init; }
    public ExportStrategy ExportStrategy { get; init; }
}

public class ChangeExportStrategyCommandHandler : IRequestHandler<ChangeExportStrategyCommand, ExportStrategyStateDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IThingsBoardConnectorService _tbService;

    public ChangeExportStrategyCommandHandler(
        IApplicationDbContext context,
        IThingsBoardConnectorService tbService)
    {
        _context = context;
        _tbService = tbService;
    }

    public async Task<ExportStrategyStateDto?> Handle(ChangeExportStrategyCommand request, CancellationToken cancellationToken)
    {
        // Get the device from database
        var device = await _context.Devices
            .FirstOrDefaultAsync(d => d.Id == request.DeviceId, cancellationToken)
            ?? throw AppException.NotFound(
                MessageCodes.DEVICE_NOT_FOUND,
                new Dictionary<string, object> { ["deviceId"] = request.DeviceId });

        // Map enum to device export strategy code
        // Device expects: 0 = selling_first, 1 = zero_export_to_load, 2 = zero_export_to_ct
        var exportStrategyCode = request.ExportStrategy switch
        {
            ExportStrategy.SellFirst => 0,
            ExportStrategy.ZeroExportToLoad => 1,
            ExportStrategy.ZeroExportToCt => 2,
            _ => throw AppException.BadRequest(
                MessageCodes.INVALID_EXPORT_STRATEGY,
                new Dictionary<string, object> { ["exportStrategy"] = request.ExportStrategy.ToString() })
        };

        // Prepare RPC parameters with the export strategy code
        var parameters = new Dictionary<string, object>
        {
            ["exportStrategyCode"] = exportStrategyCode
        };

        // Send two-way RPC to ThingsBoard to set the export strategy
        // RPC method: setExportStrategy (register 244 on the device)
        // Use two-way to get confirmation response from device
        var rpcResponse = await _tbService.SendTwoWayRpcAsync(
            deviceId: device.ThingsBoardDeviceId,
            method: "setExportStrategy",
            parameters: parameters,
            timeoutMs: 10000,
            cancellationToken: cancellationToken
        );

        // Check if RPC call failed
        if (rpcResponse == null || !rpcResponse.Success || rpcResponse.Response == null)
        {
            return null;
        }

        // Parse the device response
        try
        {
            var json = JsonSerializer.Serialize(rpcResponse.Response);
            var deviceResponse = JsonSerializer.Deserialize<ExportStrategyStateDto>(json);
            return deviceResponse;
        }
        catch (JsonException)
        {
            // If deserialization fails, return null
            return null;
        }
    }
}
