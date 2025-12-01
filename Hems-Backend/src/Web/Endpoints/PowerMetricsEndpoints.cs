using Hems_Backend.Application.Dashboard.Queries.GetPowerMetrics;
using Hems_Backend.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Hems_Backend.Web.Endpoints;

/// <summary>
/// DEBUG ONLY endpoints for development and troubleshooting.
/// These are exposed so developers can call application queries directly from Swagger
/// for quick verification. Do NOT use these endpoints in production.
/// </summary>
public static class PowerMetricsEndpoints
{
    public static void MapPowerMetricsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api").WithTags("Dashboard");

        // DEBUG: Exposes GetPowerMetricsQuery via HTTP for debugging only.
        // Do not use in production environments.
        group.MapGet("/powerMetrics", async (
            ISender sender,
            Guid deviceId,
            string? period,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPowerMetricsQuery
            {
                DeviceId = deviceId,
                Period = period ?? "daily"
            };

            var result = await sender.Send(query, cancellationToken);

            return Results.Ok(result);
        })
        .WithName("GetPowerMetrics")
        .Produces<PowerMetricsResponseDto>(StatusCodes.Status200OK)
        .WithMetadata(new System.ObsoleteAttribute("Debug-only endpoint. Do not use in production."))
        .RequireAuthorization();
    }
}

