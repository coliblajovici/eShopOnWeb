﻿using System.Collections.Generic;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;

namespace Microsoft.eShopWeb.ApplicationCore.Entities;
public class OrderPlacedCommand
{    
    public string OrderId { get; set; }
    public Address ShippingAddress { get; set; }
    public List<OrderItemDetail> OrderItemDetails { get; set; }
    public decimal FinalPrice { get; set; }    
}
