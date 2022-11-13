namespace Microsoft.eShopWeb.ApplicationCore.ServiceBus;

public class EventBusConfiguration
{
    public string ConnectionString { get; set; }
    public string TopicName { get; set; }
    public string Subscription { get; set; }
}
