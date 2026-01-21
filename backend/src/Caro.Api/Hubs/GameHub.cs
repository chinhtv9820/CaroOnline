using Caro.Services;
using Caro.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Caro.Api.Services;

namespace Caro.Api.Hubs;

/// <summary>
/// SignalR Hub quản lý toàn bộ logic realtime cho PvP:
/// - Lobby (hiển thị người online, tạo phòng, invite/join)
/// - Phòng chơi (move, chat, countdown)
/// - Thách đấu / Join request / Kick / Resign
/// - Đồng bộ trạng thái cho tất cả người chơi
/// </summary>
[Authorize]
public class GameHub : Hub
{
    private readonly IGameService _gameService;
    private readonly IAiService _aiService;
    private readonly PresenceService _presence;
    private readonly ChallengeService _challenges;
    private readonly GameTimerService _timers;

    public GameHub(IGameService gameService, IAiService aiService, PresenceService presence, ChallengeService challenges, GameTimerService timers)
    {
        _gameService = gameService;
        _aiService = aiService;
        _presence = presence;
        _challenges = challenges;
        _timers = timers;
    }

    /// <summary>
    /// Lấy userId từ JWT claims (sub/nameidentifier/userId).
    /// Có log hỗ trợ debug khi claim không tồn tại hoặc parse lỗi.
    /// </summary>
    private int GetUserId()
    {
        if (Context.User == null)
        {
            Console.WriteLine("[GetUserId] Context.User is null");
            return 0;
        }
        
        // Try multiple claim types
        var sub = Context.User.FindFirstValue("sub") 
               ?? Context.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
               ?? Context.User.FindFirstValue("userId");
        
        if (string.IsNullOrEmpty(sub))
        {
            Console.WriteLine($"[GetUserId] No 'sub' claim found. Available claims: {string.Join(", ", Context.User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
            return 0;
        }
        
        if (int.TryParse(sub, out var id))
        {
            Console.WriteLine($"[GetUserId] Found userId: {id} from claim 'sub'");
            return id;
        }
        
        Console.WriteLine($"[GetUserId] Failed to parse userId from claim 'sub': {sub}");
        return 0;
    }

    /// <summary>
    /// Khi client kết nối: thêm vào Presence, join group lobby và broadcast số người online.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        Console.WriteLine($"[OnConnectedAsync] User {userId} connected. ConnectionId: {Context.ConnectionId}");
        Console.WriteLine($"[OnConnectedAsync] Context.User is null: {Context.User == null}");
        if (Context.User != null)
        {
            Console.WriteLine($"[OnConnectedAsync] Context.User.Identity.Name: {Context.User.Identity?.Name}");
            Console.WriteLine($"[OnConnectedAsync] Claims: {string.Join(", ", Context.User.Claims.Select(c => $"{c.Type}={c.Value}"))}");
        }
        
        if (userId > 0)
        {
            _presence.OnConnected(userId, Context.ConnectionId);
            var onlineUserIds = _presence.GetOnlineUserIds();
            
            Console.WriteLine($"[OnConnectedAsync] User {userId} added to presence. Online users: {string.Join(", ", onlineUserIds)}");
            
            // Add to lobby group first
            await Groups.AddToGroupAsync(Context.ConnectionId, "lobby");
            
            await Clients.Group("lobby").SendAsync("LobbyUpdate", new { usersOnline = onlineUserIds.Length });
            await Clients.Group("lobby").SendAsync("OnlineUsersList", onlineUserIds);
        }
        else
        {
            Console.WriteLine($"[OnConnectedAsync] Warning: userId is 0. User might not be authenticated.");
        }
        
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Khi client disconnect: xóa khỏi Presence, xử lý resign nếu còn trong game, broadcast online list mới.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        _presence.OnDisconnected(Context.ConnectionId);
        
        // Handle disconnect in active games
        if (userId > 0)
        {
            await HandlePlayerDisconnect(userId);
        }
        
        var onlineUserIds = _presence.GetOnlineUserIds();
        await Clients.Group("lobby").SendAsync("LobbyUpdate", new { usersOnline = onlineUserIds.Length });
        await Clients.Group("lobby").SendAsync("OnlineUsersList", onlineUserIds);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Khi player disconnect giữa trận: tự động resign và thông báo game kết thúc vì disconnect.
    /// </summary>
    private async Task HandlePlayerDisconnect(int userId)
    {
        // Find active games for this user
        var activeGames = await _gameService.GetGamesAsync();
        var userGames = activeGames.Where(g => 
            (g.P1UserId == userId || g.P2UserId == userId) && 
            g.FinishedAt == null
        ).ToList();

        foreach (var game in userGames)
        {
            // Mark game as finished due to disconnect
            await _gameService.ResignAsync(game.Id, game.P1UserId == userId ? 1 : 2);
            _timers.Stop(game.Id);
            
            var group = $"game-{game.Id}";
            await Clients.Group(group).SendAsync("GameEnded", new { 
                gameId = game.Id, 
                result = game.P1UserId == userId ? "P2_WIN_DISCONNECT" : "P1_WIN_DISCONNECT",
                disconnectedPlayer = userId
            });
        }
    }

    /// <summary>
    /// Client gọi để tham gia lobby: thêm vào group "lobby", gửi số người online + danh sách user hiện tại.
    /// </summary>
    public async Task JoinLobby()
    {
        var userId = GetUserId();
        Console.WriteLine($"[JoinLobby] Called by user {userId}, ConnectionId: {Context.ConnectionId}");
        
        // Ensure user is in presence (in case OnConnectedAsync hasn't finished)
        if (userId > 0)
        {
            _presence.OnConnected(userId, Context.ConnectionId);
            Console.WriteLine($"[JoinLobby] User {userId} added to presence");
        }
        else
        {
            Console.WriteLine($"[JoinLobby] WARNING: userId is 0, user not authenticated!");
        }
        
        await Groups.AddToGroupAsync(Context.ConnectionId, "lobby");
        var onlineUserIds = _presence.GetOnlineUserIds();
        
        // Log for debugging
        Console.WriteLine($"[JoinLobby] User {userId} joined lobby. Online users: [{string.Join(", ", onlineUserIds)}] (count: {onlineUserIds.Length})");
        
        await Clients.Caller.SendAsync("LobbyUpdate", new { usersOnline = onlineUserIds.Length });
        // Send online users list
        await Clients.Caller.SendAsync("OnlineUsersList", onlineUserIds);
        Console.WriteLine($"[JoinLobby] Sent OnlineUsersList to caller: [{string.Join(", ", onlineUserIds)}]");
    }

    /// <summary>
    /// Tham gia game PvP: nếu phòng chưa đủ người thì gửi join request tới chủ phòng,
    /// nếu đã là thành viên thì chỉ join group game.
    /// </summary>
    public async Task JoinGame(int gameId)
    {
        var group = $"game-{gameId}";
        var userId = GetUserId();
        var game = await _gameService.GetGameAsync(gameId) ?? throw new HubException("Game not found");
        
        // Check if game is full
        if (game.P1UserId.HasValue && game.P2UserId.HasValue)
            throw new HubException("Game is full");
        
        // If user is already in game, just join the group
        if (game.P1UserId == userId || game.P2UserId == userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
            await Clients.Group(group).SendAsync("PlayerJoined", new { gameId, userId });
            return;
        }
        
        // If game has owner and user is not owner, send join request
        if (game.RoomOwnerId.HasValue && game.RoomOwnerId != userId)
        {
            // Send join request to room owner
            var ownerConnections = _presence.GetConnectionsForUser(game.RoomOwnerId.Value);
            
            Console.WriteLine($"[JoinGame] User {userId} trying to join game {gameId}. Owner: {game.RoomOwnerId.Value}, Owner connections: {ownerConnections.Count}");
            
            if (ownerConnections.Count == 0)
            {
                // Try to find owner in all connections (maybe they're connected but not in presence)
                var allOnlineUsers = _presence.GetOnlineUserIds();
                Console.WriteLine($"[JoinGame] Owner {game.RoomOwnerId.Value} not found in presence. Online users: {string.Join(", ", allOnlineUsers)}");
                
                // If owner is P1UserId and they're online, they should be in presence
                // But if not, we'll allow direct join (maybe owner disconnected)
                // For now, throw error but with more info
                throw new HubException($"Room owner (User {game.RoomOwnerId.Value}) is offline. Please wait for them to come back online.");
            }
            
            foreach (var cid in ownerConnections)
            {
                await Clients.Client(cid).SendAsync("JoinRequest", new { 
                    gameId, 
                    userId, 
                    username = $"User {userId}" // In real app, fetch from DB
                });
            }
            
            Console.WriteLine($"[JoinGame] Join request sent to owner {game.RoomOwnerId.Value}");
            
            // Start countdown (8 seconds)
            _ = StartJoinRequestCountdown(gameId, userId, game.RoomOwnerId.Value);
            return;
        }
        
        // Direct join if no owner or user is owner
        try
        {
            await _gameService.EnsurePlayerAsync(gameId, userId);
        }
        catch (Exception ex)
        {
            throw new HubException(ex.Message);
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        await Clients.Group(group).SendAsync("PlayerJoined", new { gameId, userId });
    }
    
    private async Task StartJoinRequestCountdown(int gameId, int requestingUserId, int ownerId)
    {
        for (int i = 8; i > 0; i--) // 8 seconds countdown
        {
            await Task.Delay(1000);
            
            // Check if request was accepted/rejected
            var game = await _gameService.GetGameAsync(gameId);
            if (game == null || game.P2UserId == requestingUserId)
                return; // Request accepted or game deleted
            
            // Send countdown update to requester
            var requesterConnections = _presence.GetConnectionsForUser(requestingUserId);
            foreach (var cid in requesterConnections)
            {
                await Clients.Client(cid).SendAsync("JoinRequestCountdown", new { 
                    gameId, 
                    remainingSeconds = i 
                });
            }
        }
        
        // Timeout - notify requester
        var timeoutConnections = _presence.GetConnectionsForUser(requestingUserId);
        foreach (var cid in timeoutConnections)
        {
            await Clients.Client(cid).SendAsync("JoinRequestTimeout", new { gameId });
        }
    }
    
    /// <summary>
    /// Chủ phòng chấp nhận yêu cầu vào phòng của user khác → gán làm P2, join group game và bắt đầu nếu đủ người.
    /// </summary>
    public async Task AcceptJoinRequest(int gameId, int requestingUserId)
    {
        var userId = GetUserId();
        var game = await _gameService.GetGameAsync(gameId) ?? throw new HubException("Game not found");
        
        // Check if user is room owner
        if (game.RoomOwnerId != userId)
            throw new HubException("Only room owner can accept join requests");
        
        // Check if game is full
        if (game.P1UserId.HasValue && game.P2UserId.HasValue)
            throw new HubException("Game is full");
        
        // Assign player
        await _gameService.EnsurePlayerAsync(gameId, requestingUserId);
        
        // Add to game group
        var group = $"game-{gameId}";
        var requesterConnections = _presence.GetConnectionsForUser(requestingUserId);
        foreach (var cid in requesterConnections)
        {
            await Groups.AddToGroupAsync(cid, group);
        }
        
        // Notify both players
        await Clients.Group(group).SendAsync("PlayerJoined", new { gameId, userId = requestingUserId });
        
        // If game is now full (2 players), start the game
        var updatedGame = await _gameService.GetGameAsync(gameId);
        if (updatedGame?.P1UserId.HasValue == true && updatedGame?.P2UserId.HasValue == true)
        {
            _timers.Start(gameId, updatedGame.TimeControlSeconds, updatedGame.CurrentTurn);
            await Clients.Group(group).SendAsync("GameStarted", new { gameId = updatedGame.Id, mode = updatedGame.Mode });
        }
    }
    
    /// <summary>
    /// Chủ phòng từ chối yêu cầu vào phòng → gửi thông báo cho người yêu cầu.
    /// </summary>
    public async Task RejectJoinRequest(int gameId, int requestingUserId)
    {
        var userId = GetUserId();
        var game = await _gameService.GetGameAsync(gameId) ?? throw new HubException("Game not found");
        
        // Check if user is room owner
        if (game.RoomOwnerId != userId)
            throw new HubException("Only room owner can reject join requests");
        
        // Notify requester
        var requesterConnections = _presence.GetConnectionsForUser(requestingUserId);
        foreach (var cid in requesterConnections)
        {
            await Clients.Client(cid).SendAsync("JoinRequestRejected", new { gameId });
        }
    }

    /// <summary>
    /// Gửi lời mời chơi trực tiếp (challenge) đến user khác, có timeout đếm ngược.
    /// </summary>
    public async Task SendChallenge(int targetUserId, int timeoutSeconds = 10)
    {
        var fromId = GetUserId();
        if (fromId == targetUserId) throw new HubException("Cannot challenge yourself");
        
        var challenge = _challenges.Create(fromId, targetUserId, timeoutSeconds);
        var connections = _presence.GetConnectionsForUser(targetUserId);
        
        if (connections.Count == 0) throw new HubException("Target user is offline");
        
        foreach (var cid in connections)
        {
            await Clients.Client(cid).SendAsync("ChallengeReceived", new { 
                id = challenge.Id, 
                fromUserId = fromId,
                expiresAt = challenge.ExpiresAt
            });
        }
        await Clients.Caller.SendAsync("ChallengeSent", new { id = challenge.Id, toUserId = targetUserId });
        
        // Start countdown timer
        _ = StartChallengeCountdown(challenge.Id, timeoutSeconds);
    }

    private async Task StartChallengeCountdown(Guid challengeId, int timeoutSeconds)
    {
        for (int i = timeoutSeconds; i > 0; i--)
        {
            await Task.Delay(1000);
            var challenge = _challenges.Get(challengeId);
            if (challenge == null) return; // Challenge was accepted/rejected
            
            var connections = _presence.GetConnectionsForUser(challenge.ToUserId);
            foreach (var cid in connections)
            {
                await Clients.Client(cid).SendAsync("ChallengeCountdown", new { 
                    challengeId = challengeId, 
                    remainingSeconds = i 
                });
            }
        }
        
        // Timeout - notify both users
        var expiredChallenge = _challenges.Get(challengeId);
        if (expiredChallenge != null)
        {
            _challenges.Remove(challengeId);
            var fromConnections = _presence.GetConnectionsForUser(expiredChallenge.FromUserId);
            var toConnections = _presence.GetConnectionsForUser(expiredChallenge.ToUserId);
            
            foreach (var cid in fromConnections)
            {
                await Clients.Client(cid).SendAsync("ChallengeTimeout", new { challengeId = challengeId });
            }
            foreach (var cid in toConnections)
            {
                await Clients.Client(cid).SendAsync("ChallengeTimeout", new { challengeId = challengeId });
            }
        }
    }

    /// <summary>
    /// Người nhận challenge chấp nhận → tự động tạo game PvP mới, join cả hai vào phòng và bắt đầu trận.
    /// </summary>
    public async Task AcceptChallenge(string challengeId)
    {
        if (!Guid.TryParse(challengeId, out var id)) throw new HubException("Invalid challengeId");
        var challenge = _challenges.Get(id) ?? throw new HubException("Challenge not found or expired");
        var me = GetUserId();
        if (challenge.ToUserId != me) throw new HubException("Not target user");

        _challenges.Remove(id);
        var game = await _gameService.CreateGameAsync(new CreateGameOptions("PvP", challenge.FromUserId, challenge.ToUserId, null, 60));

        // Add both to game group
        var group = $"game-{game.Id}";
        foreach (var uid in new[] { challenge.FromUserId, challenge.ToUserId })
        {
            foreach (var cid in _presence.GetConnectionsForUser(uid))
            {
                await Groups.AddToGroupAsync(cid, group);
            }
        }

        // Start timer
        _timers.Start(game.Id, game.TimeControlSeconds, game.CurrentTurn);

        await Clients.Group(group).SendAsync("GameStarted", new { gameId = game.Id, mode = game.Mode });
        await Clients.Clients(_presence.GetConnectionsForUser(challenge.FromUserId)).SendAsync("ChallengeAccepted", challenge.Id, new { gameId = game.Id });
    }

    /// <summary>
    /// Người nhận challenge từ chối → thông báo lại cho người gửi, hủy countdown.
    /// </summary>
    public async Task RejectChallenge(string challengeId)
    {
        if (!Guid.TryParse(challengeId, out var id)) throw new HubException("Invalid challengeId");
        var challenge = _challenges.Get(id) ?? throw new HubException("Challenge not found");
        var me = GetUserId();
        if (challenge.ToUserId != me) throw new HubException("Not target user");

        _challenges.Remove(id);
        
        // Notify challenger
        var fromConnections = _presence.GetConnectionsForUser(challenge.FromUserId);
        foreach (var cid in fromConnections)
        {
            await Clients.Client(cid).SendAsync("ChallengeRejected", new { challengeId = id, rejectedBy = me });
        }
        
        await Clients.Caller.SendAsync("ChallengeRejected", new { challengeId = id });
    }

    /// <summary>
    /// Tạo game mới qua SignalR:
    /// - PvP: chỉ gửi event "RoomCreated" (chờ người thứ 2).
    /// - PvE: khởi động timer và gửi "GameStarted" ngay.
    /// </summary>
    public async Task<object> CreateGame(string mode, int? p1UserId, int? p2UserId, int? pveDifficulty, int timeControlSeconds)
    {
        var userId = GetUserId();
        
        // Ensure user is in presence (room owner)
        if (userId > 0)
        {
            _presence.OnConnected(userId, Context.ConnectionId);
            Console.WriteLine($"[CreateGame] User {userId} (room owner) ensured in presence");
        }
        
        var game = await _gameService.CreateGameAsync(new CreateGameOptions(mode, p1UserId, p2UserId, pveDifficulty, timeControlSeconds));
        var group = $"game-{game.Id}";
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        
        Console.WriteLine($"[CreateGame] Game {game.Id} created. RoomOwnerId: {game.RoomOwnerId}, P1UserId: {game.P1UserId}");
        
        // For PvP: Only start game when 2 players are present
        // For PvE: Start immediately
        if (mode == "PvE" && game.P1UserId.HasValue)
        {
            _timers.Start(game.Id, game.TimeControlSeconds, game.CurrentTurn);
            await Clients.Caller.SendAsync("GameStarted", new { gameId = game.Id, mode = game.Mode });
        }
        else if (mode == "PvP")
        {
            // For PvP, just notify that room is created, don't start game yet
            // Game will start when 2nd player joins (via AcceptJoinRequest or direct join)
            await Clients.Caller.SendAsync("RoomCreated", new { gameId = game.Id, mode = game.Mode });
        }
        
        return new { game.Id };
    }

    /// <summary>
    /// Thực hiện nước đi trong PvP: validate, lưu DB, broadcast "MoveMade".
    /// Nếu là PvE thì xử lý riêng ở controller.
    /// </summary>
    public async Task MakeMove(int gameId, int x, int y)
    {
        try
        {
            var userId = GetUserId();
            var player = await _gameService.EnsurePlayerAsync(gameId, userId);
            var game = await _gameService.GetGameAsync(gameId) ?? throw new HubException("Game not found");
            var move = await _gameService.MakeMoveAsync(gameId, player, x, y);
            
            // Refresh game to check if it's finished (win detected)
            var updatedGame = await _gameService.GetGameAsync(gameId);
            bool isWin = updatedGame?.FinishedAt != null;
            
            // Get updated game to get currentTurn
            var gameAfterMove = await _gameService.GetGameAsync(gameId);
            
            await Clients.Group($"game-{gameId}").SendAsync("MoveMade", new { 
                gameId, 
                player = move.Player, 
                x = move.X, 
                y = move.Y, 
                moveNumber = move.MoveNumber,
                currentTurn = gameAfterMove?.CurrentTurn ?? (move.Player == 1 ? 2 : 1)
            });
            
            if (isWin)
            {
                var winner = updatedGame!.Result!.StartsWith("P1") ? 1 : 2;
                await Clients.Group($"game-{gameId}").SendAsync("GameEnded", new { gameId, result = updatedGame.Result, winner });
                _timers.Stop(gameId);
                return;
            }
            
            _timers.SwitchTurn(gameId, move.Player == 1 ? 2 : 1);
            
            // Nếu là PvE mode và vừa player 1 move xong, tự động make move cho AI (player 2)
            if (game.Mode == "PvE" && game.PveDifficulty.HasValue && move.Player == 1)
            {
                // Đợi ngắn để client update UI (giảm xuống 100ms)
                await Task.Delay(100);
                
                // Lấy game với moves mới nhất để build board state
                var gameWithMoves = await GetGameWithMovesForAi(gameId);
                if (gameWithMoves == null || gameWithMoves.FinishedAt != null) return;
                
                // Build board state từ moves
                var board = BuildBoardFromMoves(gameWithMoves.Moves);
                
                // AI là player 2, difficulty từ game
                var aiMove = await _aiService.ChooseMoveAsync(board, 2, (AiDifficulty)game.PveDifficulty.Value);
                
                // Make AI move
                var aiMoveResult = await _gameService.MakeMoveAsync(gameId, 2, aiMove.X, aiMove.Y);
                
                // Check if AI won
                var aiGameAfterMove = await _gameService.GetGameAsync(gameId);
                bool aiWin = aiGameAfterMove?.FinishedAt != null;
                
                await Clients.Group($"game-{gameId}").SendAsync("MoveMade", new { gameId, player = aiMoveResult.Player, x = aiMoveResult.X, y = aiMoveResult.Y, moveNumber = aiMoveResult.MoveNumber });
                
                if (aiWin)
                {
                    var winner = aiGameAfterMove!.Result!.StartsWith("P1") ? 1 : 2;
                    await Clients.Group($"game-{gameId}").SendAsync("GameEnded", new { gameId, result = aiGameAfterMove.Result, winner });
                    _timers.Stop(gameId);
                    return;
                }
                
                _timers.SwitchTurn(gameId, 1);
            }
        }
        catch (DbUpdateException dbex)
        {
            var msg = dbex.InnerException?.Message ?? dbex.Message;
            throw new HubException($"DB error: {msg}");
        }
        catch (InvalidOperationException ex)
        {
            // Wrap InvalidOperationException thành HubException để SignalR trả về đúng cho client
            throw new HubException(ex.Message);
        }
        catch (Exception ex)
        {
            // Log other exceptions and re-throw as HubException
            throw new HubException($"Error making move: {ex.Message}");
        }
    }
    
    private async Task<Game?> GetGameWithMovesForAi(int gameId)
    {
        var game = await _gameService.GetGameAsync(gameId);
        if (game == null) return null;
        
        // Get moves và set vào game object để AI có thể tính toán
        var moves = await _gameService.GetMovesAsync(gameId);
        return new Game
        {
            Id = game.Id,
            Mode = game.Mode,
            PveDifficulty = game.PveDifficulty,
            FinishedAt = game.FinishedAt,
            Moves = moves
        };
    }
    
    private static int[,] BuildBoardFromMoves(List<Move> moves)
    {
        const int size = 15;
        var board = new int[size, size];
        foreach (var move in moves)
        {
            board[move.X, move.Y] = move.Player;
        }
        return board;
    }

    /// <summary>
    /// Người chơi PvP đầu hàng → cập nhật DB, dừng timer, broadcast "GameEnded".
    /// </summary>
    public async Task Resign(int gameId)
    {
        var game = await _gameService.GetGameAsync(gameId) ?? throw new HubException("Game not found");
        await _gameService.ResignAsync(gameId, game.CurrentTurn);
        await Clients.Group($"game-{gameId}").SendAsync("GameEnded", new { gameId, result = "resign" });
        _timers.Stop(gameId);
    }

    /// <summary>
    /// Gửi tin nhắn chat realtime trong game (PvP).
    /// </summary>
    public async Task SendChatMessage(int gameId, string content)
    {
        if (string.IsNullOrWhiteSpace(content) || content.Length > 500)
            throw new HubException("Invalid message content");
        
        var userId = GetUserId();
        var player = await _gameService.EnsurePlayerAsync(gameId, userId);
        
        var message = await _gameService.SendMessageAsync(gameId, userId, content);
        
        await Clients.Group($"game-{gameId}").SendAsync("ChatMessage", new
        {
            gameId,
            senderId = userId,
            content = message.Content,
            timestamp = message.Timestamp
        });
    }

    /// <summary>
    /// Chủ phòng kick người chơi thứ 2 khỏi phòng, thông báo tới cả room và lobby.
    /// </summary>
    public async Task KickPlayer(int gameId, int targetUserId)
    {
        var userId = GetUserId();
        var game = await _gameService.GetGameAsync(gameId) ?? throw new HubException("Game not found");
        
        // Only room owner can kick
        if (game.RoomOwnerId != userId) throw new HubException("Only room owner can kick players");
        
        // Cannot kick yourself
        if (targetUserId == userId) throw new HubException("Cannot kick yourself");
        
        // Check if target is in game
        if (game.P1UserId != targetUserId && game.P2UserId != targetUserId)
            throw new HubException("Target user is not in this game");
        
        // Resign for kicked player
        int kickedPlayer = game.P1UserId == targetUserId ? 1 : 2;
        await _gameService.ResignAsync(gameId, kickedPlayer);
        _timers.Stop(gameId);
        
        var group = $"game-{gameId}";
        await Clients.Group(group).SendAsync("PlayerKicked", new { gameId, kickedUserId = targetUserId, kickedBy = userId });
        await Clients.Group(group).SendAsync("GameEnded", new { 
            gameId, 
            result = kickedPlayer == 1 ? "P2_WIN_KICK" : "P1_WIN_KICK",
            kickedPlayer = targetUserId
        });
    }

    public Task Ping()
    {
        return Clients.Caller.SendAsync("Pong");
    }
}


