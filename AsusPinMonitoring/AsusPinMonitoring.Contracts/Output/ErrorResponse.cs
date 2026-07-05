namespace AsusPinMonitoring.Contracts.Output;

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;

    public static ErrorResponse SensorSnapshotNotAvailable => new()
    {
        Error = "Sensor snapshot not available."
    };
}