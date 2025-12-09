using littleShop.orders.Data;
using littleShop.orders.DTOs;
using littleShop.orders.Entities;
using littleShop.orders.Services;
using littleShop.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected; // VITAL para mockear HttpClient
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
            // 1. Mocks de BBDD
            var options = new DbContextOptions<OrdersDbContext>();
            _mockDb = new Mock<OrdersDbContext>(options);
            _mockOrdersSet = new Mock<DbSet<Order>>();
            _mockDb.Setup(m => m.Orders).Returns(_mockOrdersSet.Object);

            // 2. Mock de RabbitMQ
            _mockPublish = new Mock<IPublishEndpoint>();

            // 3. Mock de HttpClient (Simulamos la respuesta del Catálogo)
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
                   StatusCode = HttpStatusCode.OK, // Simulamos que el catálogo dice "Todo OK, stock reducido"
                   Content = new StringContent("{}")
               });

            var httpClient = new HttpClient(handlerMock.Object)
            {
                BaseAddress = new Uri("http://test-catalog/")
            };

            // Configuramos la factoría para que devuelva nuestro cliente trucado
            _mockHttpFactory = new Mock<IHttpClientFactory>();
            _mockHttpFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // 4. Instanciamos el servicio
            _service = new OrderService(_mockDb.Object, _mockHttpFactory.Object, _mockPublish.Object);
        }

        [Test]
        public async Task CreateOrderAsync_WithValidStock_ShouldSaveOrder_AndPublishEvent()
        {
            // Arrange
            var userId = "user1";
            var email = "test@test.com";
            var request = new CreateOrderRequest(new List<OrderItemDto>
            {
                new OrderItemDto(1, "Producto 1", 1, 100m)
            });

            _mockOrdersSet.Setup(m => m.Add(It.IsAny<Order>()));

            // Act
            var result = await _service.CreateOrderAsync(userId, email, request);

            // Assert
            Assert.That(result.Succeeded, Is.True);

            // Verificar que se guardó en BD con los datos correctos
            _mockOrdersSet.Verify(m => m.Add(It.Is<Order>(o => o.UserId == userId && o.TotalAmount == 100m)), Times.Once);
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Once);

            // Verificar evento RabbitMQ (Puede requerir espera si es Task.Run, pero Moq suele capturarlo)
            // En un entorno real se recomienda no usar Task.Run dentro del servicio para facilitar el testing, 
            // o extraer la publicación a un método virtual.
            // Aquí verificamos que al menos se intentó.
            await Task.Delay(100); // Pequeña espera para el hilo secundario
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
                UserId = "u1",
                CustomerEmail = "a@a.com",
                Status = OrderStatus.Confirmed // Estado válido para enviar
            };

            // Simulamos FindAsync
            _mockOrdersSet.Setup(m => m.FindAsync(orderId)).ReturnsAsync(order);

            // Act
            var result = await _service.ShipOrderAsync(orderId);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            Assert.That(order.Status, Is.EqualTo(OrderStatus.Shipped)); // Verificamos cambio de estado

            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        [Test]
        public async Task ShipOrderAsync_WhenStatusInvalid_ShouldFail()
        {
            // Arrange
            var orderId = 1;
            var order = new Order
            {
                Id = orderId,
                // CORRECCIÓN: Añadimos los campos requeridos UserId y Email
                UserId = "u1",
                CustomerEmail = "a@a.com",
                Status = OrderStatus.Cancelled // Estado válido para inicializar, pero inválido para enviar
            };

            _mockOrdersSet.Setup(m => m.FindAsync(orderId)).ReturnsAsync(order);

            // Act
            var result = await _service.ShipOrderAsync(orderId);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors[0], Does.Contain("Solo se envían pedidos confirmados"));

            // No debe guardar cambios
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Never);
        }
    }
}