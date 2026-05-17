using System.Text.Json.Serialization;
using WireViewMonitor.Contracts.Common;
using WireViewMonitor.Contracts.Input;
using WireViewMonitor.Contracts.Output;

namespace WireViewMonitor.Service;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SensorResponse))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(ConfigResponse))]
[JsonSerializable(typeof(ConfigRequest))]
[JsonSerializable(typeof(ClearFaultsRequest))]
[JsonSerializable(typeof(DeviceConfig))]
internal partial class AppJsonContext : JsonSerializerContext
{
}