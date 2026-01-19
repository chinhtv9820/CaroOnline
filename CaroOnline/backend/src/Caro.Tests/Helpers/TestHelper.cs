using Caro.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Caro.Tests.Helpers;

public static class TestHelper
{
    public static CaroDbContext CreateInMemoryDbContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<CaroDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .Options;
        
        return new CaroDbContext(options);
    }

    public static IConfiguration CreateTestConfiguration()
    {
        var config = new Dictionary<string, string?>
        {
            { "Jwt:Key", "test_key_123456789012345678901234567890" },
            { "Jwt:Issuer", "CaroTest" },
            { "Jwt:Audience", "CaroClientsTest" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();
    }
}

