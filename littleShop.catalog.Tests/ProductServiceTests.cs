using littleShop.catalog.Data;
using littleShop.catalog.DTOs;
using littleShop.catalog.Entities;
using littleShop.catalog.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace littleShop.catalog.Tests
{
    [TestFixture]
    public class ProductServiceTests
    {
        private Mock<CatalogDbContext> _mockDb;
        private Mock<DbSet<Product>> _mockSet;
        private ProductService _service;

        [SetUp]
        public void Setup()
        {
            // 1. Mockeamos el DbContext
            var options = new DbContextOptions<CatalogDbContext>();
            _mockDb = new Mock<CatalogDbContext>(options);

            // 2. Mockeamos el DbSet (la tabla Products)
            _mockSet = new Mock<DbSet<Product>>();

            // 3. Conectamos: Cuando pidan db.Products, devolvemos el mock
            _mockDb.Setup(m => m.Products).Returns(_mockSet.Object);

            // 4. Creamos el servicio
            _service = new ProductService(_mockDb.Object);
        }

        [Test]
        public async Task CreateAsync_ShouldAddProductAndSaveChanges()
        {
            // Arrange
            var request = new CreateProductRequest("Portátil", "Gaming", 1000m, 10);

            // Configuramos el Add para que no falle (aunque Moq ya lo hace por defecto con Loose mocks)
            _mockSet.Setup(m => m.Add(It.IsAny<Product>()));

            // Act
            var result = await _service.CreateAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data!.Name, Is.EqualTo("Portátil"));

            // Verificamos que se llamó a Add() una vez con los datos correctos
            _mockSet.Verify(m => m.Add(It.Is<Product>(p => p.Name == "Portátil" && p.Price == 1000m)), Times.Once);

            // Verificamos que se guardaron los cambios en la BD
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        [Test]
        public async Task ReduceStockAsync_WhenProductExistsAndStockIsEnough_ShouldReduce()
        {
            // Arrange
            var productId = 1;
            // Creamos un producto "falso" que devolverá la base de datos
            var product = new Product { Id = productId, Name = "Test", Price = 10, Stock = 100 };

            // Simulamos que FindAsync devuelve este producto
            _mockSet.Setup(m => m.FindAsync(productId)).ReturnsAsync(product);

            // Act
            var result = await _service.ReduceStockAsync(productId, 10); // Quitamos 10

            // Assert
            Assert.That(result.Succeeded, Is.True);
            Assert.That(product.Stock, Is.EqualTo(90)); // 100 - 10 = 90

            // Verificamos que se guardó
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        [Test]
        public async Task ReduceStockAsync_WhenStockIsNotEnough_ShouldReturnFailure()
        {
            // Arrange
            var productId = 1;
            var product = new Product { Id = productId, Name = "Test", Stock = 5 }; // Solo hay 5

            _mockSet.Setup(m => m.FindAsync(productId)).ReturnsAsync(product);

            // Act
            var result = await _service.ReduceStockAsync(productId, 10); // Pedimos 10

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors[0], Does.Contain("No hay suficiente stock"));

            // Verificamos que el stock NO cambió
            Assert.That(product.Stock, Is.EqualTo(5));

            // Verificamos que NO se guardaron cambios
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Never);
        }

        [Test]
        public async Task UpdateAsync_ShouldModifyFieldsAndSave()
        {
            // Arrange
            var productId = 1;
            var product = new Product { Id = productId, Name = "Viejo", Price = 10, Stock = 10 };
            var updateReq = new UpdateProductRequest("Nuevo", "Desc Nueva", 20, 20);

            _mockSet.Setup(m => m.FindAsync(productId)).ReturnsAsync(product);

            // Act
            var result = await _service.UpdateAsync(productId, updateReq);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            Assert.That(product.Name, Is.EqualTo("Nuevo")); // Se actualizó el objeto
            Assert.That(product.Price, Is.EqualTo(20));

            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        [Test]
        public async Task DeleteAsync_ShouldRemoveAndSave()
        {
            // Arrange
            var productId = 1;
            var product = new Product { Id = productId, Name = "Borrar" };

            _mockSet.Setup(m => m.FindAsync(productId)).ReturnsAsync(product);

            // Act
            var result = await _service.DeleteAsync(productId);

            // Assert
            Assert.That(result.Succeeded, Is.True);

            _mockSet.Verify(m => m.Remove(product), Times.Once);
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }
    }
}