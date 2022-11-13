using System.Collections.Generic;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;

namespace Microsoft.eShopWeb.ApplicationCore.Entities;
public class OrderPlacedEvent
{    
    public string OrderId { get; set; }
    public Address ShippingAddress { get; set; }
    public List<string> Items { get; set; }
    public decimal FinalPrice { get; set; }    
}

