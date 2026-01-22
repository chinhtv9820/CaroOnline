using Caro.Core.Entities;
using Caro.Infrastructure;
using Caro.Services;
using Caro.Tests.Helpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Caro.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly CaroDbContext _db;
    private readonly IConfiguration _config;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _db = TestHelper.CreateInMemoryDbContext();
        _config = TestHelper.CreateTestConfiguration();
        _authService = new AuthService(_db, _config);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var username = "testuser";
        var email = "test@example.com";
        var password = "password123";
        var displayName = "Test User";

        // Act
        var user = await _authService.RegisterAsync(username, email, password, displayName);

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().BeGreaterThan(0);
        user.Username.Should().Be(username);
        user.Email.Should().Be(email);
        user.DisplayName.Should().Be(displayName);
        user.PasswordHash.Should().NotBeNullOrEmpty();
        user.PasswordHash.Should().NotBe(password); // Should be hashed
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateUsername_ShouldThrowException()
    {
        // Arrange
        var username = "duplicateuser";
        var email1 = "test1@example.com";
        var email2 = "test2@example.com";
        var password = "password123";

        await _authService.RegisterAsync(username, email1, password, null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _authService.RegisterAsync(username, email2, password, null));
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ShouldThrowException()
    {
        // Arrange
        var username1 = "user1";
        var username2 = "user2";
        var email = "duplicate@example.com";
        var password = "password123";

        await _authService.RegisterAsync(username1, email, password, null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _authService.RegisterAsync(username2, email, password, null));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task RegisterAsync_WithInvalidUsername_ShouldThrowException(string username)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _authService.RegisterAsync(username, "test@example.com", "password123", null));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task RegisterAsync_WithInvalidEmail_ShouldThrowException(string email)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _authService.RegisterAsync("testuser", email, "password123", null));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("12345")] // Less than 6 characters
    public async Task RegisterAsync_WithInvalidPassword_ShouldThrowException(string password)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _authService.RegisterAsync("testuser", "test@example.com", password, null));
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@example.com")]
    [InlineData("test.example.com")]
    public async Task RegisterAsync_WithInvalidEmailFormat_ShouldThrowException(string email)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _authService.RegisterAsync("testuser", email, "password123", null));
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnUserAndToken()
    {
        // Arrange
        var username = "testuser";
        var email = "test@example.com";
        var password = "password123";
        
        await _authService.RegisterAsync(username, email, password, null);

        // Act
        var (user, token) = await _authService.LoginAsync(username, password);

        // Assert
        user.Should().NotBeNull();
        user.Username.Should().Be(username);
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_WithEmail_ShouldReturnUserAndToken()
    {
        // Arrange
        var username = "testuser";
        var email = "test@example.com";
        var password = "password123";
        
        await _authService.RegisterAsync(username, email, password, null);

        // Act
        var (user, token) = await _authService.LoginAsync(email, password);

        // Assert
        user.Should().NotBeNull();
        user.Username.Should().Be(username);
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowException()
    {
        // Arrange
        var username = "testuser";
        var email = "test@example.com";
        var password = "password123";
        
        await _authService.RegisterAsync(username, email, password, null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _authService.LoginAsync(username, "wrongpassword"));
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ShouldThrowException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _authService.LoginAsync("nonexistent", "password123"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task LoginAsync_WithInvalidUsernameOrEmail_ShouldThrowException(string usernameOrEmail)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _authService.LoginAsync(usernameOrEmail, "password123"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task LoginAsync_WithEmptyPassword_ShouldThrowException(string password)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _authService.LoginAsync("testuser", password));
    }

    [Fact]
    public void GenerateJwtToken_ShouldReturnValidToken()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var token = _authService.GenerateJwtToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        token.Split('.').Length.Should().Be(3); // JWT has 3 parts
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}

