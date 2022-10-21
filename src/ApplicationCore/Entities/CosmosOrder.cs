namespace Microsoft.eShopWeb.ApplicationCore.Entities;
public class CosmosOrder
{    
    public string Id { get; set; }    
    public string OrderId { get; set; }
    public string ShippingAddress { get; set; }
    public string Items { get; set; }
    public decimal FinalPrice { get; set; }    
}

