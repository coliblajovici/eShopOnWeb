using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using System;
using Azure.Messaging.ServiceBus;
using System.Collections.Generic;
using Azure.Core.Extensions;
using Microsoft.Extensions.Configuration;

namespace Microsoft.eShopWeb.ApplicationCore.Services;
public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;
    private readonly IConfiguration _configuration;

    private readonly ServiceBusSender _serviceBusSender;

    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        ServiceBusSender serviceBusSender,
        IUriComposer uriComposer,
        IConfiguration configuration
        )
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
        _serviceBusSender = serviceBusSender;
        _configuration = configuration;
    }

    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        var basketSpec = new BasketWithItemsSpecification(basketId);
        var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);

        Guard.Against.Null(basket, nameof(basket));
        Guard.Against.EmptyBasketOnCheckout(basket.Items);

        var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
        var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

        var items = basket.Items.Select(basketItem =>
        {
            var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
            var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
            var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
            return orderItem;
        }).ToList();

        var order = new Order(basket.BuyerId, shippingAddress, items);

        await _orderRepository.AddAsync(order);

        await PublishOrderAsync(order);

        await DeliverOrderAsync(order);
    }

    private async Task PublishOrderAsync(Order order)
    {
        var orderPlacedJson = CreateOrderCommandData(order);
       
        var body = Encoding.UTF8.GetBytes(orderPlacedJson);

        var message = new ServiceBusMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            Subject = "Order placed " + order.Id,
            Body = new BinaryData(body)
        };

        await _serviceBusSender.SendMessageAsync(message);
    }

    private static string CreateOrderCommandData(Order order)
    {
        var orderPlacedCommand = new OrderPlacedCommand()
        {
            OrderId = order.Id.ToString(),
            ShippingAddress = new Address(order.ShipToAddress.Street, order.ShipToAddress.City, order.ShipToAddress.State, order.ShipToAddress.Country, order.ShipToAddress.ZipCode),
            FinalPrice = order.Total(),
            OrderItemDetails = new List<OrderItemDetail>()
        };

        foreach (var item in order.OrderItems)
        {
            orderPlacedCommand.OrderItemDetails.Add(
                new OrderItemDetail(){ 
                    ItemId = item.ItemOrdered.CatalogItemId, 
                    Name = item.ItemOrdered.ProductName, 
                    Quantity = item.Units });
        };

        return JsonSerializer.Serialize(orderPlacedCommand, orderPlacedCommand.GetType());        
    }

    private async Task<string?> DeliverOrderAsync(Order order)
    {
        HttpClient httpClient = new HttpClient();
        
        string functionUrl = _configuration["DeliveryOrderProcessorFunctionApp"];

        DeliveryOrder deliveryOrder = new DeliveryOrder()
        {
            OrderId = order.Id.ToString(),
            ShippingAddress = new Address(order.ShipToAddress.Street, order.ShipToAddress.City, order.ShipToAddress.State, order.ShipToAddress.Country, order.ShipToAddress.ZipCode),
            FinalPrice = order.Total(),            
        };

        foreach (var item in order.OrderItems)
        {
            deliveryOrder.OrderItemDetails.Add(
                new OrderItemDetail()
                {
                    ItemId = item.ItemOrdered.CatalogItemId,
                    Name = item.ItemOrdered.ProductName,
                    Quantity = item.Units
                });
        };

        using StringContent jsonContent = new(JsonSerializer.Serialize(deliveryOrder));
        
        using HttpResponseMessage response = await httpClient.PostAsync(
            functionUrl,
            jsonContent);

        return response.Content.ReadAsStringAsync().ToString();
        
    }
}
