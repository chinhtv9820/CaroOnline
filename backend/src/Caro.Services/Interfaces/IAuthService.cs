using Caro.Core.Entities;

namespace Caro.Services;

public interface IAuthService
{
    Task<User> RegisterAsync(string username, string email, string password, string? displayName, CancellationToken ct = default);
    Task<(User user, string token)> LoginAsync(string usernameOrEmail, string password, CancellationToken ct = default);
    string GenerateJwtToken(User user);
}


