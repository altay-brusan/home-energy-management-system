namespace Hems_Backend.Domain.Enums;

public enum DeviceType
{
    Invertor = 1,
    BMS = 2,
    Battery = 3, // Alias for BMS to support existing code
    MPPS = 4 // MPPT Solar charge controller
}


