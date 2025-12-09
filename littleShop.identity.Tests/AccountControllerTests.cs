using FluentValidation;
using FluentValidation.Results;
using littleShop.identity.Controllers;
using littleShop.identity.Models;
using littleShop.identity.Services;
using littleShop.Shared.Events;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System.Security.Claims;

using ValidationResult = FluentValidation.Results.ValidationResult;

namespace littleShop.identity.Tests
{
    [TestFixture]
    public class AccountControllerTests
    {
        private Mock<UserManager<IdentityUser>> _mockUserManager;
        private Mock<IJwtService> _mockJwtService;
        private Mock<IPublishEndpoint> _mockPublishEndpoint;
        private Mock<IValidator<RegisterRequest>> _mockRegisterValidator;

        private AccountController _controller = null!;

        [SetUp]
        public void Setup()
        {
            var store = new Mock<IUserStore<IdentityUser>>();
            _mockUserManager = new Mock<UserManager<IdentityUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _mockJwtService = new Mock<IJwtService>();
            _mockPublishEndpoint = new Mock<IPublishEndpoint>();
            _mockRegisterValidator = new Mock<IValidator<RegisterRequest>>();

            _controller = new AccountController(
                _mockUserManager.Object,
                _mockJwtService.Object,
                _mockPublishEndpoint.Object,
                _mockRegisterValidator.Object
            );
        }

        [Test]
        public async Task Register_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var request = new RegisterRequest { Email = "test@test.com", Password = "Pass123!" };

            _mockRegisterValidator
                .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(new List<ValidationFailure>()));

            _mockUserManager
                .Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager
                .Setup(x => x.GenerateEmailConfirmationTokenAsync(It.IsAny<IdentityUser>()))
                .ReturnsAsync("token-email-dummy");

            // Act
            var result = await _controller.Register(request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());

            _mockPublishEndpoint.Verify(x => x.Publish(It.IsAny<UserCreatedEvent>(), default), Times.Once);
        }

        [Test]
        public async Task Login_WithValidCredentials_ShouldReturnToken()
        {
            // Arrange
            var req = new LoginRequest { Email = "user@test.com", Password = "Pass123!" };
            var user = new IdentityUser { Id = "uid-1", Email = req.Email, EmailConfirmed = true };

            _mockUserManager.Setup(x => x.FindByEmailAsync(req.Email)).ReturnsAsync(user);
            _mockUserManager.Setup(x => x.IsEmailConfirmedAsync(user)).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.CheckPasswordAsync(user, req.Password)).ReturnsAsync(true);
            _mockUserManager.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

            var authResponse = new AuthResponse
            {
                Token = "fake-jwt",
                RefreshToken = "fake-refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            };

            _mockJwtService
                .Setup(x => x.GenerateJwtAsync(user.Id, user.Email!, "User"))
                .ReturnsAsync(authResponse);

            // Act
            var result = await _controller.Login(req);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;

            var responseData = okResult!.Value as AuthResponse;
            Assert.That(responseData, Is.Not.Null);
            Assert.That(responseData!.Token, Is.EqualTo("fake-jwt"));
        }
    }
}