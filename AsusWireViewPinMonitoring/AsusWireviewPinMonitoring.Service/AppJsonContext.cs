using System.Text.Json.Serialization;
using AsusWireviewPinMonitoring.Contracts.Common;
using AsusWireviewPinMonitoring.Contracts.Input;
using AsusWireviewPinMonitoring.Contracts.Output;

namespace AsusWireviewPinMonitoring.Service;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GpuSensorReading))]
[JsonSerializable(typeof(GpuSensorSnapshot))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(UpdateConfigRequest))]
[JsonSerializable(typeof(ClearFaultsRequest))]
[JsonSerializable(typeof(GetConfigResponse))]
[JsonSerializable(typeof(WireViewConfig))]
internal partial class AppJsonContext : JsonSerializerContext
{
}