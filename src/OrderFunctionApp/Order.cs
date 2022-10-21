
using System.Collections.Generic;
using System;

public class Order
{
#pragma warning disable CS8618 // Required by Entity Framework
    private Order() { }
    public string BuyerId { get;  set; }
    public DateTimeOffset OrderDate { get;  set; } = DateTimeOffset.Now;
//    public Address ShipToAddress { get;  set; }
  //  private readonly List<OrderItem> OrderItems = new List<OrderItem>();
       
    
}
