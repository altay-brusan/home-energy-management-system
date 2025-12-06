using System.Text.Json;

// Simulate your DTOs
public class ComponentSummaryDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class InverterSummaryDto : ComponentSummaryDto
{
    public double? ActivePowerKw { get; set; }
}

public class BatterySummaryDto : ComponentSummaryDto
{
    public double? StateOfCharge { get; set; }
}

// Test WITHOUT polymorphism configuration
var components = new List<ComponentSummaryDto>
{
    new InverterSummaryDto { Id = Guid.NewGuid(), Type = "inverter", Name = "Test Inverter", ActivePowerKw = 5.5 },
    new BatterySummaryDto { Id = Guid.NewGuid(), Type = "battery", Name = "Test Battery", StateOfCharge = 85.0 }
};

var json = JsonSerializer.Serialize(components, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine("Serialized without polymorphism:");
Console.WriteLine(json);
Console.WriteLine("\nNotice: ActivePowerKw and StateOfCharge are MISSING!");
