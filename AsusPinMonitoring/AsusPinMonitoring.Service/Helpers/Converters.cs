using AsusPinMonitoring.Contracts.Common;
using AsusPinMonitoring.Contracts.Input;
using AsusPinMonitoring.Contracts.Output;

namespace AsusPinMonitoring.Service.Helpers;

internal static class Converters
{
    public static Configuration ToConfiguration(this UpdateConfigRequest request) =>
        new()
        {
            PollIntervalMs = request.PollIntervalMs,
            ImbalanceAmperageThreshold = request.ImbalanceAmperageThreshold,
            ImbalancePercentageThreshold = request.ImbalancePercentageThreshold,
            ImbalanceDurationThresholdMs = request.ImbalanceDurationThresholdMs,
            WireOvercurrentThresholdMs = request.WireOvercurrentThresholdMs,
            WireOvercurrentAmperageThreshold = request.WireOvercurrentAmperageThreshold
        };

    public static GetConfigResponse ToResponse(this Configuration configuration) =>
        new()
        {
            PollIntervalMs = configuration.PollIntervalMs,
            ImbalanceAmperageThreshold = configuration.ImbalanceAmperageThreshold,
            ImbalancePercentageThreshold = configuration.ImbalancePercentageThreshold,
            ImbalanceDurationThresholdMs = configuration.ImbalanceDurationThresholdMs,
            WireOvercurrentThresholdMs = configuration.WireOvercurrentThresholdMs,
            WireOvercurrentAmperageThreshold = configuration.WireOvercurrentAmperageThreshold
        };
}