using System.Text.Json.Serialization;
using AsusPinMonitoring.Contracts.Common;
using AsusPinMonitoring.Contracts.Input;
using AsusPinMonitoring.Contracts.Output;

namespace AsusPinMonitoring.Service;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GpuSensorReading))]
[JsonSerializable(typeof(GpuSensorSnapshot))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(UpdateConfigRequest))]
[JsonSerializable(typeof(GetConfigResponse))]
internal partial class AppJsonContext : JsonSerializerContext
{
}