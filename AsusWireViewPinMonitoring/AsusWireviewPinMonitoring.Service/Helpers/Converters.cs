using AsusWireviewPinMonitoring.Contracts.Common;
using AsusWireviewPinMonitoring.Contracts.Input;
using AsusWireviewPinMonitoring.Contracts.Output;

namespace AsusWireviewPinMonitoring.Service.Helpers;

internal static class Converters
{
    public static Configuration ToConfiguration(this UpdateConfigRequest request) =>
        new()
        {
            PollIntervalMs = request.PollIntervalMs,
        };

    public static GetConfigResponse ToResponse(this Configuration configuration) =>
        new()
        {
            PollIntervalMs = configuration.PollIntervalMs,
        };
}