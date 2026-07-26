namespace AzureFunctionDemo.Models;

public class Event
{
    public string id { get; set; } = string.Empty;

    public string eventType { get; set; } = string.Empty;

    public string eventName { get; set; } = string.Empty;

    public string location { get; set; } = string.Empty;

    public string status { get; set; } = string.Empty;
}