using System.Runtime.CompilerServices;
using Hems_Backend.Application.PowerFlow.Common.Models;
using Hems_Backend.Application.Common.Interfaces;
using Hems_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Hems_Backend.Web.Services;

namespace Hems_Backend.Web.Endpoints;

public static class PowerFlowStreamingEndpoints
{
    public static void MapPowerFlowStreamingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithTags("PowerFlow");

        group.MapGet("/powerFlow/stream", (
            IPowerFlowService powerFlowService,
            IApplicationDbContext context,
            IUser user,
            Guid deviceId,
            int intervalSeconds,
            CancellationToken cancellationToken) => StreamPowerFlowEndpoint(powerFlowService, context, user, deviceId, intervalSeconds, cancellationToken))
        .WithName("StreamPowerFlow")
        .Produces<PowerFlowDto>(StatusCodes.Status200OK)
        .RequireAuthorization();
    }

    private static async IAsyncEnumerable<PowerFlowDto> StreamPowerFlowEndpoint(
        IPowerFlowService powerFlowService,
        IApplicationDbContext context,
        IUser user,
        Guid deviceId,
        int intervalSeconds,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {

        // Clamp interval to a sane range
        if (intervalSeconds <= 0) intervalSeconds = 5;
        if (intervalSeconds < 2) intervalSeconds = 2;
        if (intervalSeconds > 60) intervalSeconds = 60;

        var interval = TimeSpan.FromSeconds(intervalSeconds);

        while (!cancellationToken.IsCancellationRequested)
        {
            PowerFlowDto? dto = null;
            try
            {
                dto = await powerFlowService.GetPowerFlowAsync(deviceId, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PowerFlow streaming endpoint - error getting PowerFlow for device {deviceId}: {ex}");
                yield break;
            }

            if (dto != null)
            {
                yield return dto;
            }

            try
            {
                await Task.Delay(interval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
        }
    }

}
