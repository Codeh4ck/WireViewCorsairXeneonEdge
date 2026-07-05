using System.Collections.Frozen;
using LibreHardwareMonitor.Hardware;

namespace AsusPinMonitoring.Service.Helpers;

internal static class Maps
{
    public static readonly FrozenDictionary<(string SensorName, SensorType SensorType), string> FriendlyNames = new Dictionary<(string SensorName, SensorType SensorType), string>()
    {
        [("GPU Core", SensorType.Temperature)] = "GPU Core Temp",
        [("GPU Core", SensorType.Clock)] = "GPU Core Clock",
        [("GPU Memory", SensorType.Clock)] = "GPU Memory Clock",
        [("GPU Fan 1", SensorType.Fan)] = "Fan 1 Speed",
        [("GPU Fan 2", SensorType.Fan)] = "Fan 2 Speed",
        [("GPU Core", SensorType.Load)] = "GPU Usage",
        [("GPU Memory Junction", SensorType.Temperature)] = "VRAM Temp",
        [("GPU Memory Total", SensorType.SmallData)] = "VRAM Total",
        [("GPU Memory Free", SensorType.SmallData)] = "VRAM Free",
        [("GPU Memory Used", SensorType.SmallData)] = "VRAM Used",
        [("GPU Memory", SensorType.Load)] = "VRAM Usage",
        [("GPU Package", SensorType.Power)] = "GPU Power Draw",
        [("12VHPWR Pin 1", SensorType.Voltage)] = "Pin 1",
        [("12VHPWR Pin 2", SensorType.Voltage)] = "Pin 2",
        [("12VHPWR Pin 3", SensorType.Voltage)] = "Pin 3",
        [("12VHPWR Pin 4", SensorType.Voltage)] = "Pin 4",
        [("12VHPWR Pin 5", SensorType.Voltage)] = "Pin 5",
        [("12VHPWR Pin 6", SensorType.Voltage)] = "Pin 6",
        [("12VHPWR Pin 1", SensorType.Current)] = "Pin 1",
        [("12VHPWR Pin 2", SensorType.Current)] = "Pin 2",
        [("12VHPWR Pin 3", SensorType.Current)] = "Pin 3",
        [("12VHPWR Pin 4", SensorType.Current)] = "Pin 4",
        [("12VHPWR Pin 5", SensorType.Current)] = "Pin 5",
        [("12VHPWR Pin 6", SensorType.Current)] = "Pin 6",
        [("12VHPWR Pin 1", SensorType.Power)] = "Pin 1",
        [("12VHPWR Pin 2", SensorType.Power)] = "Pin 2",
        [("12VHPWR Pin 3", SensorType.Power)] = "Pin 3",
        [("12VHPWR Pin 4", SensorType.Power)] = "Pin 4",
        [("12VHPWR Pin 5", SensorType.Power)] = "Pin 5",
        [("12VHPWR Pin 6", SensorType.Power)] = "Pin 6",
        [("12VHPWR Connector", SensorType.Power)] = "Total Power",
        [("12VHPWR Connector", SensorType.Current)] = "Total Current",
    }.ToFrozenDictionary();
}