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

namespace Microsoft.eShopWeb.ApplicationCore.Services;
public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;

    private readonly ServiceBusSender _serviceBusSender;

    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        ServiceBusSender serviceBusSender,
        IUriComposer uriComposer)
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
        _serviceBusSender = serviceBusSender;
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
    }

    public async Task PublishOrderAsync(Order order)
    {
        var orderPlacedEvent = new OrderPlacedEvent()
        {
            OrderId = order.Id.ToString(),
            ShippingAddress = new Address(order.ShipToAddress.Street,order.ShipToAddress.City, order.ShipToAddress.State, order.ShipToAddress.Country, order.ShipToAddress.ZipCode),
            FinalPrice = order.Total(),
            Items = new List<string>(order.OrderItems.Select(p => p.ItemOrdered.ProductName))
        };

        var eventName = orderPlacedEvent.GetType().Name;
        var jsonMessage = JsonSerializer.Serialize(orderPlacedEvent, orderPlacedEvent.GetType());
        var body = Encoding.UTF8.GetBytes(jsonMessage);

        var message = new ServiceBusMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            Subject = eventName,
            Body = new BinaryData(body)
        };

        await _serviceBusSender.SendMessageAsync(message);
    }

    public async Task<string?> TriggerOrderReserveAsync(Order order)
    {
        HttpClient httpClient = new HttpClient();

        //Move to appsettings
        string functionUrl = @"https://ordermod3.azurewebsites.net/api/OrderItemsReserver?code=Ayk2WQ0m7rKaKi6SuqDdTZJRDKJWDpk1upEWqSgWD5wMAzFuY6wwyw==";

        CosmosOrder cosmosOrder = new CosmosOrder()
        {
            OrderId = order.Id.ToString(),
            ShippingAddress = order.ShipToAddress.ToString(),
            FinalPrice = order.Total(),
            Items = order.OrderItems.Select(p => p.ItemOrdered.ProductName).First()
        };

        using StringContent jsonContent = new(JsonSerializer.Serialize(cosmosOrder));
        
        using HttpResponseMessage response = await httpClient.PostAsync(
            functionUrl,
            jsonContent);

        return response.Content.ReadAsStringAsync().ToString();
        
    }
}
