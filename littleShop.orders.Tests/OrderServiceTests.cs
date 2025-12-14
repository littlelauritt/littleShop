using littleShop.orders.Data;
using littleShop.orders.DTOs;
using littleShop.orders.Entities;
using littleShop.orders.Services;
using littleShop.orders.Shared;
using littleShop.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System.Net;
using System.Text.Json;

namespace littleShop.orders.Tests
{
    [TestFixture]
    public class OrderServiceTests
    {
        private Mock<OrdersDbContext> _mockDb;
        private Mock<DbSet<Order>> _mockOrdersSet;
        private Mock<IHttpClientFactory> _mockHttpFactory;
        private Mock<IPublishEndpoint> _mockPublish;
        private OrderService _service;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptions<OrdersDbContext>();
            _mockDb = new Mock<OrdersDbContext>(options);

            _mockOrdersSet = new Mock<DbSet<Order>>();
            _mockDb.Setup(m => m.Orders).Returns(_mockOrdersSet.Object);

            _mockPublish = new Mock<IPublishEndpoint>();

            // Mock HttpClient para simular respuestas del catálogo
            var handlerMock = new Mock<HttpMessageHandler>();

            // Respuesta para GET /api/v1/products/{id}
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent(JsonSerializer.Serialize(new
                   {
                       Id = 1,
                       Name = "Producto Test",
                       Price = 100m
                   }))
               });

            // Respuesta para POST /api/v1/products/{id}/reduce-stock
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent("{\"succeeded\": true}")
               });

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://test-catalog/")
            };

            _mockHttpFactory = new Mock<IHttpClientFactory>();
            _mockHttpFactory.Setup(_ => _.CreateClient("catalog-api")).Returns(httpClient);

            _service = new OrderService(_mockDb.Object, _mockHttpFactory.Object, _mockPublish.Object);
        }

        [Test]
        public async Task CreateOrderAsync_WithValidStock_ShouldSaveOrder_AndPublishEvent()
        {
            // Arrange
            var userId = "user1";
            var email = "test@test.com";

            var items = new List<OrderItemDto>
            {
                new OrderItemDto(1, 1, 100m)
            };

            var request = new CreateOrderRequest(items, "Calle Falsa 123");

            _mockOrdersSet.Setup(m => m.Add(It.IsAny<Order>()));

            // Act
            var result = await _service.CreateOrderAsync(userId, email, request);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data!.TotalAmount, Is.EqualTo(100m));
            Assert.That(result.Data.Status, Is.EqualTo("Pending"));

            _mockOrdersSet.Verify(m => m.Add(It.Is<Order>(o =>
                o.UserId == userId &&
                o.CustomerEmail == email &&
                o.TotalAmount == 100m)),
                Times.Once);

            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Once);

            // Dar tiempo para que se publique el evento
            await Task.Delay(100);

            _mockPublish.Verify(x => x.Publish(
                It.IsAny<OrderCreatedEvent>(),
                default),
                Times.Once);
        }

        [Test]
        public async Task CreateOrderAsync_WithEmptyItems_ShouldFail()
        {
            // Arrange
            var userId = "user1";
            var email = "test@test.com";
            var request = new CreateOrderRequest(new List<OrderItemDto>(), "Calle Falsa 123");

            // Act
            var result = await _service.CreateOrderAsync(userId, email, request);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors[0], Does.Contain("al menos un producto"));

            _mockOrdersSet.Verify(m => m.Add(It.IsAny<Order>()), Times.Never);
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Never);
        }

        [Test]
        public async Task CreateOrderAsync_WithEmptyShippingAddress_ShouldFail()
        {
            // Arrange
            var userId = "user1";
            var email = "test@test.com";
            var items = new List<OrderItemDto> { new OrderItemDto(1, 1, 100m) };
            var request = new CreateOrderRequest(items, "");

            // Act
            var result = await _service.CreateOrderAsync(userId, email, request);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors[0], Does.Contain("dirección de envío"));

            _mockOrdersSet.Verify(m => m.Add(It.IsAny<Order>()), Times.Never);
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Never);
        }

        [Test]
        public async Task GetMyOrdersAsync_ShouldReturnUserOrders()
        {
            // Arrange
            var userId = "user1";
            var orders = new List<Order>
            {
                new Order
                {
                    Id = 1,
                    UserId = userId,
                    CustomerEmail = "test@test.com",
                    TotalAmount = 100m,
                    Status = OrderStatus.Pending,
                    ShippingAddress = "Test Address",
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = 1, ProductName = "Test Product", Quantity = 1, UnitPrice = 100m }
                    }
                },
                new Order
                {
                    Id = 2,
                    UserId = userId,
                    CustomerEmail = "test@test.com",
                    TotalAmount = 200m,
                    Status = OrderStatus.Shipped,
                    ShippingAddress = "Test Address 2",
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = 2, ProductName = "Test Product 2", Quantity = 2, UnitPrice = 100m }
                    }
                }
            }.AsQueryable();

            var mockSet = CreateMockDbSet(orders);
            _mockDb.Setup(m => m.Orders).Returns(mockSet.Object);

            // Act
            var result = await _service.GetMyOrdersAsync(userId);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data!.Count, Is.EqualTo(2));
            Assert.That(result.Data[0].UserId, Is.EqualTo(userId));
        }

        [Test]
        public async Task CancelOrderAsync_WhenOrderIsPending_ShouldCancelSuccessfully()
        {
            // Arrange
            var orderId = 1;
            var userId = "user1";
            var orders = new List<Order>
            {
                new Order
                {
                    Id = orderId,
                    UserId = userId,
                    CustomerEmail = "test@test.com",
                    Status = OrderStatus.Pending,
                    TotalAmount = 100m,
                    ShippingAddress = "Test",
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = 1, ProductName = "Test", Quantity = 1, UnitPrice = 100m }
                    }
                }
            }.AsQueryable();

            var mockSet = CreateMockDbSet(orders);
            _mockDb.Setup(m => m.Orders).Returns(mockSet.Object);

            // Act
            var result = await _service.CancelOrderAsync(orderId, userId);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            Assert.That(orders.First().Status, Is.EqualTo(OrderStatus.Cancelled));
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Once);

            // Verificar que se publicó el evento
            await Task.Delay(100);
            _mockPublish.Verify(x => x.Publish(It.IsAny<OrderCancelledEvent>(), default), Times.Once);
        }

        [Test]
        public async Task CancelOrderAsync_WhenOrderIsShipped_ShouldFail()
        {
            // Arrange
            var orderId = 1;
            var userId = "user1";
            var orders = new List<Order>
            {
                new Order
                {
                    Id = orderId,
                    UserId = userId,
                    CustomerEmail = "test@test.com",
                    Status = OrderStatus.Shipped,
                    TotalAmount = 100m,
                    ShippingAddress = "Test",
                    Items = new List<OrderItem>()
                }
            }.AsQueryable();

            var mockSet = CreateMockDbSet(orders);
            _mockDb.Setup(m => m.Orders).Returns(mockSet.Object);

            // Act
            var result = await _service.CancelOrderAsync(orderId, userId);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Null);
            Assert.That(result.Errors.Length, Is.GreaterThan(0));
            Assert.That(result.Errors[0], Does.Contain("pendientes").IgnoreCase);
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Never);
        }

        [Test]
        public async Task CancelOrderAsync_WhenOrderNotFound_ShouldFail()
        {
            // Arrange
            var orderId = 999;
            var userId = "user1";
            var orders = new List<Order>().AsQueryable();

            var mockSet = CreateMockDbSet(orders);
            _mockDb.Setup(m => m.Orders).Returns(mockSet.Object);

            // Act
            var result = await _service.CancelOrderAsync(orderId, userId);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Null);
            Assert.That(result.Errors[0], Does.Contain("no encontrado").IgnoreCase);
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Never);
        }

        [Test]
        public async Task ShipOrderAsync_WhenOrderIsPending_ShouldShipSuccessfully()
        {
            // Arrange
            var orderId = 1;
            var order = new Order
            {
                Id = orderId,
                UserId = "user1",
                CustomerEmail = "test@test.com",
                Status = OrderStatus.Pending,
                TotalAmount = 100m,
                ShippingAddress = "Test",
                Items = new List<OrderItem>()
            };

            _mockOrdersSet.Setup(m => m.FindAsync(orderId))
                .ReturnsAsync(order);

            // Act
            var result = await _service.ShipOrderAsync(orderId);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(order.Status, Is.EqualTo(OrderStatus.Shipped));
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        [Test]
        public async Task ShipOrderAsync_WhenOrderIsCancelled_ShouldFail()
        {
            // Arrange
            var orderId = 1;
            var order = new Order
            {
                Id = orderId,
                UserId = "user1",
                CustomerEmail = "test@test.com",
                Status = OrderStatus.Cancelled,
                TotalAmount = 100m,
                ShippingAddress = "Test",
                Items = new List<OrderItem>()
            };

            _mockOrdersSet.Setup(m => m.FindAsync(orderId))
                .ReturnsAsync(order);

            // Act
            var result = await _service.ShipOrderAsync(orderId);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(order.Status, Is.EqualTo(OrderStatus.Cancelled)); // No cambió
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Never);
        }

        [Test]
        public async Task CancelOrderAdminAsync_WhenOrderIsPending_ShouldCancelSuccessfully()
        {
            // Arrange
            var orderId = 1;
            var orders = new List<Order>
            {
                new Order
                {
                    Id = orderId,
                    UserId = "user1",
                    CustomerEmail = "test@test.com",
                    Status = OrderStatus.Pending,
                    TotalAmount = 100m,
                    ShippingAddress = "Test",
                    Items = new List<OrderItem>()
                }
            }.AsQueryable();

            var mockSet = CreateMockDbSet(orders);
            _mockDb.Setup(m => m.Orders).Returns(mockSet.Object);

            // Act
            var result = await _service.CancelOrderAdminAsync(orderId);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(orders.First().Status, Is.EqualTo(OrderStatus.Cancelled));
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        [Test]
        public async Task CancelOrderAdminAsync_WhenOrderIsShipped_ShouldFail()
        {
            // Arrange
            var orderId = 1;
            var orders = new List<Order>
            {
                new Order
                {
                    Id = orderId,
                    UserId = "user1",
                    CustomerEmail = "test@test.com",
                    Status = OrderStatus.Shipped,
                    TotalAmount = 100m,
                    ShippingAddress = "Test",
                    Items = new List<OrderItem>()
                }
            }.AsQueryable();

            var mockSet = CreateMockDbSet(orders);
            _mockDb.Setup(m => m.Orders).Returns(mockSet.Object);

            // Act
            var result = await _service.CancelOrderAdminAsync(orderId);

            // Assert
            Assert.That(result, Is.False);
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Never);
        }

        // Helper method para crear un mock de DbSet que soporte LINQ
        private Mock<DbSet<T>> CreateMockDbSet<T>(IQueryable<T> data) where T : class
        {
            var mockSet = new Mock<DbSet<T>>();
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
            return mockSet;
        }
    }
}