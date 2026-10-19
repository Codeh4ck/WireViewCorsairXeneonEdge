namespace AsusWireviewPinMonitoring.Service.Wireview;

public sealed record WireViewSensorData(
    double[] PinVoltage, // V  (6 pins)
    double[] PinCurrent, // A  (6 pins)
    double[] PinPower, // W  (6 pins)
    double TempInC,
    double TempOutC,
    double ExtTemp1C,
    double ExtTemp2C,
    int PsuCapabilityW,
    ushort FaultStatus,
    ushort FaultLog)
{
    public double TotalCurrentA => PinCurrent.Sum();
    public double TotalPowerW => PinPower.Sum();
}