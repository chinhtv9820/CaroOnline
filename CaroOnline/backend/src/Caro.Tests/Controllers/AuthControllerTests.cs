using Caro.Api.Controllers;
using Caro.Core.Entities;
using Caro.Services;
using Caro.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Caro.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var request = new AuthController.RegisterRequest("testuser", "test@example.com", "password123", "Test User");
        var user = new User
        {
            Id = 1,
            Username = request.Username,
            Email = request.Email,
            DisplayName = request.DisplayName,
            CreatedAt = DateTime.UtcNow
        };
        var token = "test_token";

        _authServiceMock.Setup(x => x.RegisterAsync(request.Username, request.Email, request.Password, request.DisplayName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _authServiceMock.Setup(x => x.GenerateJwtToken(user))
            .Returns(token);

        // Act
        var result = await _controller.Register(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_WithMissingUsername_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new AuthController.RegisterRequest("", "test@example.com", "password123", null);

        // Act
        var result = await _controller.Register(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ShouldReturnConflict()
    {
        // Arrange
        var request = new AuthController.RegisterRequest("testuser", "test@example.com", "password123", null);
        
        _authServiceMock.Setup(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Username already exists"));

        // Act
        var result = await _controller.Register(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOk()
    {
        // Arrange
        var request = new AuthController.LoginRequest("testuser", "password123");
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };
        var token = "test_token";

        _authServiceMock.Setup(x => x.LoginAsync(request.UsernameOrEmail, request.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync((user, token));

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new AuthController.LoginRequest("testuser", "wrongpassword");
        
        _authServiceMock.Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Invalid credentials"));

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_WithMissingUsername_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new AuthController.LoginRequest("", "password123");

        // Act
        var result = await _controller.Login(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}

