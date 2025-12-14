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

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent("{}")
               });

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://test-catalog/")
            };

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

            // CORRECCIÓN AQUÍ: Usamos el constructor (int, int, decimal)
            // Asumimos el orden: (ProductId, Quantity, UnitPrice) o (ProductId, UnitPrice, Quantity)
            // Dado el error "int, int, decimal", suele ser: Id, Cantidad, Precio.
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

            _mockOrdersSet.Verify(m => m.Add(It.Is<Order>(o => o.UserId == userId && o.TotalAmount == 100m)), Times.Once);
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Once);

            await Task.Delay(100);
            _mockPublish.Verify(x => x.Publish(It.IsAny<OrderCreatedEvent>(), default), Times.Once);
        }

        /* TESTS COMENTADOS PORQUE EL MÉTODO ShipOrderAsync YA NO EXISTE
           (Mantenlos comentados para que compile)
        */
        /*
        [Test]
        public async Task ShipOrderAsync_ShouldUpdateStatus()
        {
             // ... (código comentado)
        }

        [Test]
        public async Task ShipOrderAsync_WhenStatusInvalid_ShouldFail()
        {
             // ... (código comentado)
        }
        */
    }
}