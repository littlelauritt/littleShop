using littleShop.orders.Data;
using littleShop.orders.DTOs;
using littleShop.orders.Entities;
using littleShop.orders.Services;
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

            // Setup HttpClient Mock
            var handlerMock = new Mock<HttpMessageHandler>();

            // Mock GET product
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"id\":1, \"name\":\"Producto Test\", \"price\":50.0}")
                });

            // Mock POST reduce-stock
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("{}") });

            var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("http://test-catalog/") };
            _mockHttpFactory = new Mock<IHttpClientFactory>();
            _mockHttpFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

            _service = new OrderService(_mockDb.Object, _mockHttpFactory.Object, _mockPublish.Object);
        }

        [Test]
        public async Task CreateOrderAsync_WithValidStock_ShouldSaveOrder_AndPublishEvent()
        {
            // Arrange
            var userId = "user1";
            var email = "test@test.com";

            // Usamos el constructor (Id, Cantidad, Precio) que definiste en tu Service
            var items = new List<OrderItemDto>
            {
                new OrderItemDto(1, 2, 50m) // Total 100
            };

            var request = new CreateOrderRequest(items, "Calle Falsa 123");
            _mockOrdersSet.Setup(m => m.Add(It.IsAny<Order>()));

            // Act
            var result = await _service.CreateOrderAsync(userId, email, request);

            // Assert
            Assert.That(result.Succeeded, Is.True);

            // ✅ HE QUITADO LA LÍNEA DE TotalAmount QUE DABA ERROR PARA NO TOCAR TUS DTOs
            // Solo comprobamos que el estado sea el correcto
            Assert.That(result.Data!.Status, Is.EqualTo("Pending"));

            // Verificar que se guardó en BD correctamente
            _mockOrdersSet.Verify(m => m.Add(It.Is<Order>(o =>
                o.UserId == userId &&
                o.TotalAmount == 100m &&
                o.Status == OrderStatus.Pending
            )), Times.Once);

            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Once);
            _mockPublish.Verify(x => x.Publish(It.IsAny<OrderCreatedEvent>(), default), Times.Once);
        }

        [Test]
        public async Task ShipOrderAsync_ShouldUpdateStatus()
        {
            // Arrange
            var orderId = 1;
            var order = new Order
            {
                Id = orderId,
                UserId = "user1",
                CustomerEmail = "test@test.com",
                Status = OrderStatus.Confirmed
            };

            _mockOrdersSet.Setup(m => m.FindAsync(orderId)).ReturnsAsync(order);

            // Act
            var result = await _service.ShipOrderAsync(orderId);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(order.Status, Is.EqualTo(OrderStatus.Shipped));
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Once);

            // Verifica que se envía el tracking number
            _mockPublish.Verify(x => x.Publish(It.Is<OrderShippedEvent>(e =>
                !string.IsNullOrEmpty(e.TrackingNumber)
            ), default), Times.Once);
        }

        [Test]
        public async Task ShipOrderAsync_WhenStatusInvalid_ShouldFail()
        {
            // Arrange
            var orderId = 1;
            var order = new Order
            {
                Id = orderId,
                UserId = "user1",
                CustomerEmail = "test@test.com",
                Status = OrderStatus.Cancelled
            };

            _mockOrdersSet.Setup(m => m.FindAsync(orderId)).ReturnsAsync(order);

            // Act
            var result = await _service.ShipOrderAsync(orderId);

            // Assert
            Assert.That(result, Is.False);
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Never);
        }
    }
}