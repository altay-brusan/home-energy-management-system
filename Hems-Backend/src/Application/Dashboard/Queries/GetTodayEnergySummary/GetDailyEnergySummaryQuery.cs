using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hems_Backend.Application.Common.Models;

namespace Hems_Backend.Application.Dashboard.Queries.GetTodayEnergySummary;

public class GetDailyEnergySummaryQuery: IRequest<DailyEnergySummaryDto>
{
    public Guid DeviceId { get; init; }
}
