
using Newtonsoft.Json;

namespace OrderFunctionApp;
public class CosmosOrder
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }
        
    public string OrderId { get; set; }
    public string ShippingAddress { get; set; }
    public string Items { get; set; }
    public decimal FinalPrice { get; set; }
    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}
