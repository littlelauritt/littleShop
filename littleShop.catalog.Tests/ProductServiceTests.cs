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
            var options = new DbContextOptions<CatalogDbContext>();
            _mockDb = new Mock<CatalogDbContext>(options);
            _mockSet = new Mock<DbSet<Product>>();

            _mockDb.Setup(m => m.Products).Returns(_mockSet.Object);
            _service = new ProductService(_mockDb.Object);
        }

        [Test]
        public async Task CreateAsync_ShouldAddProductAndSaveChanges()
        {
       
            var request = new CreateProductRequest("Portátil", "Gaming", 1000m, 10, null);

            _mockSet.Setup(m => m.Add(It.IsAny<Product>()));

            var result = await _service.CreateAsync(request);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data!.Name, Is.EqualTo("Portátil"));

            _mockSet.Verify(m => m.Add(It.Is<Product>(p => p.Name == "Portátil" && p.Price == 1000m)), Times.Once);
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        [Test]
        public async Task ReduceStockAsync_WhenProductExistsAndStockIsEnough_ShouldReduce()
        {
            var productId = 1;
            var product = new Product { Id = productId, Name = "Test", Price = 10, Stock = 100 };

            _mockSet.Setup(m => m.FindAsync(productId)).ReturnsAsync(product);

            var result = await _service.ReduceStockAsync(productId, 10);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(product.Stock, Is.EqualTo(90));
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        [Test]
        public async Task ReduceStockAsync_WhenStockIsNotEnough_ShouldReturnFailure()
        {
            var productId = 1;
            var product = new Product { Id = productId, Name = "Test", Stock = 5 };

            _mockSet.Setup(m => m.FindAsync(productId)).ReturnsAsync(product);

            var result = await _service.ReduceStockAsync(productId, 10);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors[0], Does.Contain("No hay suficiente stock"));
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Never);
        }

        [Test]
        public async Task UpdateAsync_ShouldModifyFieldsAndSave()
        {
            var productId = 1;
            var product = new Product { Id = productId, Name = "Viejo", Price = 10, Stock = 10 };

            var updateReq = new UpdateProductRequest("Nuevo", "Desc Nueva", 20, 20, null);

            _mockSet.Setup(m => m.FindAsync(productId)).ReturnsAsync(product);

            var result = await _service.UpdateAsync(productId, updateReq);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(product.Name, Is.EqualTo("Nuevo"));
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }

        [Test]
        public async Task DeleteAsync_ShouldRemoveAndSave()
        {
            var productId = 1;
            var product = new Product { Id = productId, Name = "Borrar" };

            _mockSet.Setup(m => m.FindAsync(productId)).ReturnsAsync(product);

            var result = await _service.DeleteAsync(productId);

            Assert.That(result.Succeeded, Is.True);
            _mockSet.Verify(m => m.Remove(product), Times.Once);
            _mockDb.Verify(m => m.SaveChangesAsync(default), Times.Once);
        }
    }
}