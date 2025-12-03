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

        // DEBUG: Exposes GetPowerAndEnergyMetricsQuery via HTTP for debugging only.
        // Do not use in production environments.
        group.MapGet("/powerMetrics", async (
            ISender sender,
            Guid deviceId,
            string? period,
            DateOnly? date,
            int? year,
            int? week,
            int? month,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPowerAndEnergyMetricsQuery
            {
                DeviceId = deviceId,
                Period = period ?? "day",
                Date = date,
                Year = year,
                Week = week,
                Month = month
            };

            var result = await sender.Send(query, cancellationToken);

            return Results.Ok(result);
        })
        .WithName("GetPowerMetrics")
        .Produces<PowerAndEnergyMetricsResponseDto>(StatusCodes.Status200OK)
        .WithMetadata(new System.ObsoleteAttribute("Debug-only endpoint. Do not use in production."))
        .RequireAuthorization();
    }
}

