using Hems_Backend.Application.Common.Models;
//using Hems_Backend.Application.Dashboard.Queries.GetChargeDischarge;
//using Hems_Backend.Application.Dashboard.Queries.GetEnergyConsumption;
//using Hems_Backend.Application.Dashboard.Queries.GetEnergyData;
//using Hems_Backend.Application.Dashboard.Queries.GetEnergyProduction;
using Hems_Backend.Application.Dashboard.Queries.GetEnergyRevenue;
using Hems_Backend.Application.Dashboard.Queries.GetGridPerformance;
using Hems_Backend.Application.Dashboard.Queries.GetGridPerformanceSummary;
//using Hems_Backend.Application.Dashboard.Queries.GetHero;
using Hems_Backend.Application.Dashboard.Queries.GetLifetimeEnergy;
using Hems_Backend.Application.Dashboard.Queries.GetPowerMetrics;
using Hems_Backend.Application.Dashboard.Queries.GetSoc;
using Hems_Backend.Application.Dashboard.Queries.GetSoh;
using Hems_Backend.Application.Dashboard.Queries.GetTodayEnergySummary;
using Hems_Backend.Application.Devices.Queries.GetDeviceComponentsStatus;
using Hems_Backend.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hems_Backend.Web.Endpoints;

public class Dashboard : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(SOC, "Soc")
            .MapGet(SOH, "Soh")
            .MapGet(TodayEnergySummary, "TodayEnergySummary")
            .MapGet(GridPerformanceSummary, "GridPerformanceSummary")
            .MapGet(GridPerformance, "GridPerformance")
            .MapGet(EnergyRevenue, "EnergyRevenue")
            .MapGet(LifetimeEnergy, "LifetimeEnergy")
            .MapGet(PowerAndEnergyMetrics, "PowerAndEnergyMetrics")
            .MapGet(GetDeviceComponentsStatus, "DeviceComponentsStatus");
    }

    [Authorize]
    public async Task<SocResponseDto> SOC(
        ISender sender,
        [FromQuery] Guid deviceId)
    {
        var query = new GetSocQuery
        {
            DeviceId = deviceId
        };

        return await sender.Send(query);
    }

    [Authorize]
    public async Task<SohResponseDto> SOH(
        ISender sender,
        [FromQuery] Guid deviceId)
    {
        var query = new GetSohQuery
        {
            DeviceId = deviceId
        };

        return await sender.Send(query);
    }


    [Authorize]
    public async Task<DailyEnergySummaryDto> TodayEnergySummary(
    ISender sender,
    [FromQuery] Guid deviceId)
    {
        var query = new GetDailyEnergySummaryQuery
        {
            DeviceId = deviceId
        };

        return await sender.Send(query);
    }


    [Authorize]
    public async Task<DeviceWithDataResponseDto> GetDeviceComponentsStatus(
        ISender sender,
        [FromQuery] Guid deviceId)
    {
        var query = new GetDeviceComponentsStatusQuery
        {
            DeviceId = deviceId
        };

        return await sender.Send(query);
    }

    [Authorize]
    public async Task<PowerAndEnergyMetricsResponseDto> PowerAndEnergyMetrics(
        ISender sender,
        [FromQuery] Guid deviceId,
        [FromQuery] string period,
        [FromQuery] DateOnly? date = null,
        [FromQuery] int? year = null,
        [FromQuery] int? week = null,
        [FromQuery] int? month = null)
    {
        var query = new GetPowerAndEnergyMetricsQuery
        {
            DeviceId = deviceId,
            Period = period,
            Date = date,
            Year = year,
            Week = week,
            Month = month
        };

        return await sender.Send(query);
    }

   
    [Authorize]
    public async Task<GridPerformanceSummaryDto> GridPerformanceSummary(
        ISender sender,
        [FromQuery] Guid deviceId)
    {
        var query = new GetGridPerformanceSummaryQuery
        {
            DeviceId = deviceId
        };

        return await sender.Send(query);
    }

    [Authorize]
    public async Task<GridPerformanceResponseDto> GridPerformance(
        ISender sender,
        [FromQuery] Guid deviceId,
        [FromQuery] string period,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var query = new GetGridPerformanceQuery
        {
            DeviceId = deviceId,
            Period = period,
            StartDate = startDate,
            EndDate = endDate
        };

        return await sender.Send(query);
    }

    [Authorize]
    public async Task<EnergyRevenueDto> EnergyRevenue(
        ISender sender,
        [FromQuery] Guid deviceId,
        [FromQuery] string period,
        [FromQuery] DateOnly? date = null,
        [FromQuery] int? year = null,
        [FromQuery] int? week = null,
        [FromQuery] int? month = null)
    {
        var query = new GetEnergyRevenueQuery
        {
            DeviceId = deviceId,
            Period = period,
            Date = date,
            Year = year,
            Week = week,
            Month = month
        };

        return await sender.Send(query);
    }

    [Authorize]
    public async Task<LifetimeEnergyDto?> LifetimeEnergy(
        ISender sender,
        [FromQuery] Guid deviceId)
    {
        var query = new GetLifetimeEnergyQuery
        {
            DeviceId = deviceId
        };

        return await sender.Send(query);
    }
}
