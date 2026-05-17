namespace WireViewMonitor.Contracts.Output;

public class ErrorResponse
{
    public string Error { get; set; } = string.Empty;

    public static ErrorResponse DeviceNotAvailableOrDisconnected => new()
    {
        Error = "WireView Pro 2 device is either not available or not connected."
    };
}