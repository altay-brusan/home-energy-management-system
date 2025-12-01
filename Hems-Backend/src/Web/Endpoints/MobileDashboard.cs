using Hems_Backend.Application.Common.Models;
using Hems_Backend.Application.Dashboard.Queries.GetBattery;
using Hems_Backend.Application.Dashboard.Queries.GetBatteryComponent;
using Hems_Backend.Application.Dashboard.Queries.GetChargeDischarge;
using Hems_Backend.Application.Dashboard.Queries.GetEnergyConsumption;
using Hems_Backend.Application.Dashboard.Queries.GetEnergyData;
using Hems_Backend.Application.Dashboard.Queries.GetEnergyProduction;
using Hems_Backend.Application.Dashboard.Queries.GetHero;
using Hems_Backend.Application.Dashboard.Queries.GetInverterComponent;
using Hems_Backend.Application.Dashboard.Queries.GetPowerMetrics;
using Hems_Backend.Application.Dashboard.Queries.GetSoc;
using Hems_Backend.Application.Dashboard.Queries.GetSoh;
using Hems_Backend.Application.Dashboard.Queries.GetTodayEnergySummary;
using Hems_Backend.Application.Devices.Queries.GetComponentLiveData;
using Hems_Backend.Application.Devices.Queries.GetDeviceWithData;
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
            .MapGet(Hero, "hero")
            .MapGet(SOC, "Soc")
            .MapGet(SOH, "Soh")
            .MapGet(TodayEnergySummary, "TodayEnergySummary")
            .MapGet(GetDeviceWithData, "device/GetWithData")
            .MapGet(GetComponentLiveData, "device/component/getComponentLiveData")
            .MapGet(EnergyFlow, "graphs/EnergyFlow")
            .MapGet(EnergyProduction, "graphs/EnergyProduction")
            .MapGet(EnergyConsumption, "graphs/EnergyConsumption")
            .MapGet(PowerMetrics, "graphs/PowerMetrics")
            .MapGet(Battery, "graphs/Battery")
            .MapGet(ChargeDischarge, "graphs/ChargeDischarge")
            .MapGet(BatteryComponent, "graphs/Components/Battery")
            .MapGet(InverterComponent, "graphs/Components/Inverter");
    }

    [Authorize]
    public async Task<HeroResponseDto> Hero(
        ISender sender,
        [FromQuery] Guid deviceId)
    {
        var query = new GetHeroQuery
        {
            DeviceId = deviceId
        };

        return await sender.Send(query);
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
    public async Task<DeviceWithDataResponseDto> GetDeviceWithData(
        ISender sender,
        [FromQuery] Guid deviceId)
    {
        var query = new GetDeviceWithDataQuery
        {
            DeviceId = deviceId
        };

        return await sender.Send(query);
    }

    [Authorize]
    public async Task<GetComponentLiveDataResponse> GetComponentLiveData(
        ISender sender,
        [FromQuery] Guid deviceId,
        [FromQuery] Guid componentId)
    {
        var query = new GetComponentLiveDataQuery
        {
            DeviceId = deviceId,
            ComponentId = componentId
        };

        return await sender.Send(query);
    }

    [Authorize]
    public async Task<EnergyFlowResponseDto> EnergyFlow(
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
    public async Task<EnergyProductionResponseDto> EnergyProduction(
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
    public async Task<EnergyConsumptionResponseDto> EnergyConsumption(
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
    public async Task<PowerEnergyMetricsResponseDto> PowerMetrics(
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
    public async Task<BatteryResponseDto> Battery(
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
    public async Task<ChargeDischargeResponseDto> ChargeDischarge(
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

    [Authorize]
    public async Task<InverterComponentDto> InverterComponent(
        ISender sender,
        [FromQuery] Guid deviceId)
    {
        var query = new GetInverterComponentQuery
        {
            DeviceId = deviceId
        };

        return await sender.Send(query);
    }
}
