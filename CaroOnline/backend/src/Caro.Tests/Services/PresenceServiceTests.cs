using Caro.Api.Services;
using FluentAssertions;

namespace Caro.Tests.Services;

public class PresenceServiceTests
{
    private readonly PresenceService _presenceService;

    public PresenceServiceTests()
    {
        _presenceService = new PresenceService();
    }

    [Fact]
    public void OnConnected_ShouldAddUserToPresence()
    {
        // Arrange
        var userId = 1;
        var connectionId = "conn1";

        // Act
        _presenceService.OnConnected(userId, connectionId);

        // Assert
        var onlineUsers = _presenceService.GetOnlineUserIds();
        onlineUsers.Should().Contain(userId);
    }

    [Fact]
    public void OnDisconnected_ShouldRemoveUserFromPresence()
    {
        // Arrange
        var userId = 1;
        var connectionId = "conn1";
        _presenceService.OnConnected(userId, connectionId);

        // Act
        _presenceService.OnDisconnected(connectionId);

        // Assert
        var onlineUsers = _presenceService.GetOnlineUserIds();
        onlineUsers.Should().NotContain(userId);
    }

    [Fact]
    public void OnConnected_WithMultipleConnections_ShouldKeepUserOnline()
    {
        // Arrange
        var userId = 1;
        var connectionId1 = "conn1";
        var connectionId2 = "conn2";

        // Act
        _presenceService.OnConnected(userId, connectionId1);
        _presenceService.OnConnected(userId, connectionId2);
        _presenceService.OnDisconnected(connectionId1);

        // Assert
        var onlineUsers = _presenceService.GetOnlineUserIds();
        onlineUsers.Should().Contain(userId); // Still online via conn2
    }

    [Fact]
    public void GetConnectionsForUser_ShouldReturnAllConnections()
    {
        // Arrange
        var userId = 1;
        var connectionId1 = "conn1";
        var connectionId2 = "conn2";

        _presenceService.OnConnected(userId, connectionId1);
        _presenceService.OnConnected(userId, connectionId2);

        // Act
        var connections = _presenceService.GetConnectionsForUser(userId);

        // Assert
        connections.Should().HaveCount(2);
        connections.Should().Contain(connectionId1);
        connections.Should().Contain(connectionId2);
    }

    [Fact]
    public void GetConnectionsForUser_WithNonExistentUser_ShouldReturnEmpty()
    {
        // Act
        var connections = _presenceService.GetConnectionsForUser(999);

        // Assert
        connections.Should().BeEmpty();
    }

    [Fact]
    public void GetOnlineUserIds_ShouldReturnAllOnlineUsers()
    {
        // Arrange
        _presenceService.OnConnected(1, "conn1");
        _presenceService.OnConnected(2, "conn2");
        _presenceService.OnConnected(3, "conn3");

        // Act
        var onlineUsers = _presenceService.GetOnlineUserIds();

        // Assert
        onlineUsers.Should().HaveCount(3);
        onlineUsers.Should().Contain(1);
        onlineUsers.Should().Contain(2);
        onlineUsers.Should().Contain(3);
    }
}

