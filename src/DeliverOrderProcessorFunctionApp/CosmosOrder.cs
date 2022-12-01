
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace OrderFunctionApp;
public class CosmosOrder
{
    [JsonProperty(PropertyName = "id")]
    public string Id { get; set; }

    public string OrderId { get; set; }
    public Address ShippingAddress { get; set; }
    public List<OrderItemDetail> OrderItemDetails { get; set; }
    public decimal FinalPrice { get; set; }

    public CosmosOrder()
    {
        OrderItemDetails= new List<OrderItemDetail>();
    }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}
