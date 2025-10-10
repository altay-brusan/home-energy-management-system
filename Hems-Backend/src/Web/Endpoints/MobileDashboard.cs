using Hems_Backend.Application.Dashboard.Queries.GetEnergyData;
using Hems_Backend.Application.Dashboard.Queries.GetEnergyProduction;
using Hems_Backend.Application.Dashboard.Queries.GetEnergyConsumption;
using Hems_Backend.Application.Dashboard.Queries.GetPowerMetrics;
using Hems_Backend.Application.Dashboard.Queries.GetBattery;
using Hems_Backend.Application.Dashboard.Queries.GetChargeDischarge;
using Hems_Backend.Application.Dashboard.Queries.GetBatteryComponent;
using Hems_Backend.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hems_Backend.Web.Endpoints;

public class MobileDashboard : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(EnergyFlow, "graphs/EnergyFlow")
            .MapGet(EnergyProduction, "graphs/EnergyProduction")
            .MapGet(EnergyConsumption, "graphs/EnergyConsumption")
            .MapGet(PowerMetrics, "graphs/PowerMetrics")
            .MapGet(Battery, "graphs/Battery")
            .MapGet(ChargeDischarge, "graphs/ChargeDischarge")
            .MapGet(BatteryComponent, "graphs/Components/Battery");
    }

    [Authorize]
    public async Task<EnergyDataDto> EnergyFlow(
        ISender sender,
        [FromQuery] Guid deviceId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var query = new GetEnergyDataQuery
        {
            DeviceId = deviceId,
            StartDate = startDate,
            EndDate = endDate
        };

        return await sender.Send(query);
    }

    [Authorize]
    public async Task<EnergyProductionDto> EnergyProduction(
        ISender sender,
        [FromQuery] Guid deviceId,
        [FromQuery] string period)
    {
        var query = new GetEnergyProductionQuery
        {
            DeviceId = deviceId,
            Period = period
        };

        return await sender.Send(query);
    }

    [Authorize]
    public async Task<EnergyConsumptionDto> EnergyConsumption(
        ISender sender,
        [FromQuery] Guid deviceId,
        [FromQuery] string period)
    {
        var query = new GetEnergyConsumptionQuery
        {
            DeviceId = deviceId,
            Period = period
        };

        return await sender.Send(query);
    }

    [Authorize]
    public async Task<PowerMetricsDto> PowerMetrics(
        ISender sender,
        [FromQuery] Guid deviceId,
        [FromQuery] string period)
    {
        var query = new GetPowerMetricsQuery
        {
            DeviceId = deviceId,
            Period = period
        };

        return await sender.Send(query);
    }

    [Authorize]
    public async Task<BatteryDto> Battery(
        ISender sender,
        [FromQuery] Guid deviceId)
    {
        var query = new GetBatteryQuery
        {
            DeviceId = deviceId
        };

        return await sender.Send(query);
    }

    [Authorize]
    public async Task<ChargeDischargeDto> ChargeDischarge(
        ISender sender,
        [FromQuery] Guid deviceId)
    {
        var query = new GetChargeDischargeQuery
        {
            DeviceId = deviceId
        };

        return await sender.Send(query);
    }

    [Authorize]
    public async Task<BatteryComponentDto> BatteryComponent(
        ISender sender,
        [FromQuery] Guid deviceId)
    {
        var query = new GetBatteryComponentQuery
        {
            DeviceId = deviceId
        };

        return await sender.Send(query);
    }
}