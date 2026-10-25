using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hems_Backend.Application.Common.Models;

public class DailyEnergySummaryDto
{
    public Guid DeviceId { get; init; }
    public DateTime DeviceLocalTime { get; init; }

    public double? GridImport { get; init; }
    public double? GridExport { get; init; }
    public double? Consumption { get; init; }
    public double? Production { get; init; }

}
