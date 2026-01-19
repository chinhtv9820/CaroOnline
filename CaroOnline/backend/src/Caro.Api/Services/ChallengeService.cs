using System.Collections.Concurrent;

namespace Caro.Api.Services;

public record Challenge(Guid Id, int FromUserId, int ToUserId, DateTime CreatedAt, DateTime? ExpiresAt);

public class ChallengeService
{
    private readonly ConcurrentDictionary<Guid, Challenge> _challenges = new();
    private readonly Timer _cleanupTimer;

    public ChallengeService()
    {
        // Cleanup expired challenges every 30 seconds
        _cleanupTimer = new Timer(CleanupExpired, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public Challenge Create(int fromUserId, int toUserId, int timeoutSeconds = 10)
    {
        var c = new Challenge(
            Guid.NewGuid(), 
            fromUserId, 
            toUserId, 
            DateTime.UtcNow,
            DateTime.UtcNow.AddSeconds(timeoutSeconds)
        );
        _challenges[c.Id] = c;
        return c;
    }

    public Challenge? Get(Guid id)
    {
        _challenges.TryGetValue(id, out var c);
        if (c != null && c.ExpiresAt.HasValue && c.ExpiresAt.Value < DateTime.UtcNow)
        {
            _challenges.TryRemove(id, out _);
            return null;
        }
        return c;
    }

    public bool Remove(Guid id) => _challenges.TryRemove(id, out _);

    public List<Challenge> GetChallengesForUser(int userId)
    {
        return _challenges.Values
            .Where(c => c.ToUserId == userId && (!c.ExpiresAt.HasValue || c.ExpiresAt.Value > DateTime.UtcNow))
            .ToList();
    }

    private void CleanupExpired(object? state)
    {
        var expired = _challenges
            .Where(kvp => kvp.Value.ExpiresAt.HasValue && kvp.Value.ExpiresAt.Value < DateTime.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();
        
        foreach (var id in expired)
        {
            _challenges.TryRemove(id, out _);
        }
    }
}


