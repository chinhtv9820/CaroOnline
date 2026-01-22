using Caro.Core.Entities;
using Caro.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Caro.Services;

/// <summary>
/// Service xử lý toàn bộ business logic liên quan tới ván cờ:
/// - Tạo ván mới (PvE/PvP)
/// - Xử lý nước đi, kiểm tra thắng
/// - Gửi/nhận tin nhắn trong game
/// - Quản lý player tham gia
/// </summary>
public class GameService : IGameService
{
    private const int BoardSize = 15; // Kích thước bàn cờ chuẩn 15x15
    private readonly CaroDbContext _db; // DbContext để thao tác dữ liệu

    public GameService(CaroDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Tạo một game mới dựa trên options được truyền từ frontend
    /// </summary>
    /// <param name="options">Thông tin cấu hình game (mode, người chơi, thời gian, độ khó)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Game entity vừa được tạo</returns>
    public async Task<Game> CreateGameAsync(CreateGameOptions options, CancellationToken ct = default)
    {
        var game = new Game
        {
            CreatedAt = DateTime.UtcNow,
            Mode = options.Mode,
            P1UserId = options.P1UserId,
            P2UserId = options.P2UserId,
            PveDifficulty = options.PveDifficulty,
            TimeControlSeconds = options.TimeControlSeconds,
            CurrentTurn = 1,
            IsAi = options.Mode == "PvE",
            AiLevel = options.PveDifficulty,
            // Chỉ set RoomOwnerId cho PvP để xác định chủ phòng
            RoomOwnerId = options.Mode == "PvP" ? options.P1UserId : null
        };
        _db.Games.Add(game);
        await _db.SaveChangesAsync(ct);
        return game;
    }

    /// <summary>
    /// Xử lý nước đi của người chơi, cập nhật trạng thái ván cờ và kiểm tra thắng thua
    /// </summary>
    /// <param name="gameId">ID của game</param>
    /// <param name="player">Người chơi (1 hoặc 2)</param>
    /// <param name="x">Tọa độ X (0-14)</param>
    /// <param name="y">Tọa độ Y (0-14)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Move vừa được tạo</returns>
    public async Task<Move> MakeMoveAsync(int gameId, int player, int x, int y, CancellationToken ct = default)
    {
        var game = await _db.Games.Include(g => g.Moves).FirstOrDefaultAsync(g => g.Id == gameId, ct)
                   ?? throw new InvalidOperationException("Game not found");

        ValidateMove(game, player, x, y); // Kiểm tra hợp lệ trước khi lưu

        var move = new Move
        {
            GameId = gameId,
            Player = player,
            X = x,
            Y = y,
            MoveNumber = game.Moves.Count + 1,
            CreatedAt = DateTime.UtcNow
        };

        _db.Moves.Add(move);
        game.Moves.Add(move);
        
        // Nếu là nước đi đầu tiên -> set thời gian bắt đầu game
        if (game.StartTime == null)
        {
            game.StartTime = DateTime.UtcNow;
        }
        
        // Kiểm tra thắng thua sau khi đặt quân
        var winResult = CheckWin(game.Moves, player, x, y);
        if (winResult.HasWon)
        {
            game.FinishedAt = DateTime.UtcNow;
            game.WinnerId = player == 1 ? game.P1UserId : game.P2UserId;
            game.Result = player == 1 ? "P1_WIN" : "P2_WIN";
            game.WinType = winResult.WinType;
        }
        else
        {
            game.CurrentTurn = player == 1 ? 2 : 1;
        }

        await _db.SaveChangesAsync(ct);
        return move;
    }

    private record WinCheckResult(bool HasWon, string WinType);
    
    /// <summary>
    /// Thuật toán kiểm tra thắng thua cho một nước đi cuối cùng
    /// Tối ưu bằng cách chỉ duyệt quanh tọa độ vừa đánh
    /// </summary>
    /// <param name="moves">Danh sách nước đi của ván cờ</param>
    /// <param name="player">Người vừa đánh</param>
    /// <param name="lastX">Tọa độ X của nước đi cuối</param>
    /// <param name="lastY">Tọa độ Y của nước đi cuối</param>
    /// <returns>Kết quả thắng/thua và loại chiến thắng</returns>
    private static WinCheckResult CheckWin(List<Move> moves, int player, int lastX, int lastY)
    {
        // Xây dựng lại ma trận bàn cờ từ danh sách moves
        var board = new int[BoardSize, BoardSize];
        var opponent = player == 1 ? 2 : 1;
        
        foreach (var move in moves)
        {
            board[move.X, move.Y] = move.Player;
        }
        
        // Kiểm tra 4 hướng: ngang, dọc, chéo chính, chéo phụ
        var directions = new[] { (0, 1), (1, 0), (1, 1), (1, -1) };
        
        foreach (var (dx, dy) in directions)
        {
            int count = 1; // Mặc định đã có quân ở vị trí lastX,lastY
            bool leftBlocked = false, rightBlocked = false;
            
            // Quét theo hướng thuận (right)
            int rightCount = 0;
            for (int i = 1; i < 6; i++)
            {
                int x = lastX + dx * i;
                int y = lastY + dy * i;
                if (x < 0 || x >= BoardSize || y < 0 || y >= BoardSize)
                {
                    rightBlocked = true;
                    break;
                }
                if (board[x, y] == player)
                {
                    count++;
                    rightCount++;
                }
                else if (board[x, y] == opponent)
                {
                    rightBlocked = true;
                    break;
                }
                else
                {
                    break; // Empty cell
                }
            }
            
            // Quét theo hướng ngược (left)
            int leftCount = 0;
            for (int i = 1; i < 6; i++)
            {
                int x = lastX - dx * i;
                int y = lastY - dy * i;
                if (x < 0 || x >= BoardSize || y < 0 || y >= BoardSize)
                {
                    leftBlocked = true;
                    break;
                }
                if (board[x, y] == player)
                {
                    count++;
                    leftCount++;
                }
                else if (board[x, y] == opponent)
                {
                    leftBlocked = true;
                    break;
                }
                else
                {
                    break; // Empty cell
                }
            }
            
            // Điều kiện thắng 5 quân liên tiếp
            if (count >= 5)
            {
                return new WinCheckResult(true, "FIVE_IN_ROW");
            }
            
            // Điều kiện thắng nhanh: 4 quân liên tiếp và 2 đầu trống
            if (count == 4 && !leftBlocked && !rightBlocked)
            {
                // Kiểm tra hai đầu đều là ô trống
                int leftX = lastX - dx * (leftCount + 1);
                int leftY = lastY - dy * (leftCount + 1);
                int rightX = lastX + dx * (rightCount + 1);
                int rightY = lastY + dy * (rightCount + 1);
                
                bool leftEmpty = leftX >= 0 && leftX < BoardSize && leftY >= 0 && leftY < BoardSize && board[leftX, leftY] == 0;
                bool rightEmpty = rightX >= 0 && rightX < BoardSize && rightY >= 0 && rightY < BoardSize && board[rightX, rightY] == 0;
                
                if (leftEmpty && rightEmpty)
                {
                    return new WinCheckResult(true, "FOUR_TWO_END");
                }
            }
        }
        
        return new WinCheckResult(false, "");
    }

    /// <summary>
    /// Xử lý người chơi đầu hàng
    /// </summary>
    /// <param name="gameId">ID game</param>
    /// <param name="player">Người đầu hàng (1 hoặc 2)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>true nếu thành công</returns>
    public async Task<bool> ResignAsync(int gameId, int player, CancellationToken ct = default)
    {
        var game = await _db.Games.FirstOrDefaultAsync(g => g.Id == gameId, ct)
                   ?? throw new InvalidOperationException("Game not found");
        game.FinishedAt = DateTime.UtcNow;
        game.WinnerId = player == 1 ? game.P2UserId : game.P1UserId;
        game.Result = player == 1 ? "P2_WIN_RESIGN" : "P1_WIN_RESIGN";
        game.WinType = "RESIGN";
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Lấy thông tin game (không tracking để đọc)
    /// </summary>
    public Task<Game?> GetGameAsync(int id, CancellationToken ct = default)
    {
        return _db.Games.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, ct);
    }

    /// <summary>
    /// Lấy danh sách game mới nhất (tối đa 100 game) để hiển thị
    /// </summary>
    public Task<List<Game>> GetGamesAsync(CancellationToken ct = default)
    {
        return _db.Games.AsNoTracking().OrderByDescending(g => g.Id).Take(100).ToListAsync(ct);
    }

    /// <summary>
    /// Lấy danh sách nước đi của một game (để render lại ván cờ)
    /// </summary>
    public Task<List<Move>> GetMovesAsync(int gameId, CancellationToken ct = default)
    {
        return _db.Moves.AsNoTracking().Where(m => m.GameId == gameId).OrderBy(m => m.MoveNumber).ToListAsync(ct);
    }

    /// <summary>
    /// Đảm bảo user thuộc game (P1 hoặc P2). Nếu game PvP chưa có P2 thì tự động gán
    /// </summary>
    /// <param name="gameId">ID game</param>
    /// <param name="userId">ID user</param>
    /// <returns>1 nếu là P1, 2 nếu là P2</returns>
    public async Task<int> EnsurePlayerAsync(int gameId, int userId, CancellationToken ct = default)
    {
        var game = await _db.Games.FirstOrDefaultAsync(g => g.Id == gameId, ct)
                   ?? throw new InvalidOperationException("Game not found");
        
        if (game.P1UserId == userId) return 1;
        if (game.P2UserId == userId) return 2;
        
        // Assign as P2 if available
        if (game.P2UserId == null && game.Mode == "PvP")
        {
            game.P2UserId = userId;
            await _db.SaveChangesAsync(ct);
            return 2;
        }
        
        throw new InvalidOperationException("Cannot join this game");
    }

    /// <summary>
    /// Gửi tin nhắn trong game (chat realtime)
    /// </summary>
    public async Task<Message> SendMessageAsync(int gameId, int senderId, string content, CancellationToken ct = default)
    {
        var message = new Message
        {
            GameId = gameId,
            SenderId = senderId,
            Content = content,
            Timestamp = DateTime.UtcNow
        };
        _db.Messages.Add(message);
        await _db.SaveChangesAsync(ct);
        return message;
    }

    /// <summary>
    /// Lấy danh sách tin nhắn trong game để hiển thị historii
    /// </summary>
    public Task<List<Message>> GetMessagesAsync(int gameId, CancellationToken ct = default)
    {
        return _db.Messages.AsNoTracking()
            .Where(m => m.GameId == gameId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Validate nước đi: game kết thúc chưa, đúng lượt không, tọa độ hợp lệ không, ô đã có quân chưa
    /// </summary>
    private static void ValidateMove(Game game, int player, int x, int y)
    {
        if (game.FinishedAt != null) throw new InvalidOperationException("Game already finished");
        if (player != game.CurrentTurn) throw new InvalidOperationException("Not your turn");
        if (x < 0 || y < 0 || x >= BoardSize || y >= BoardSize) throw new InvalidOperationException("Out of bounds");

        var occupied = game.Moves.Any(m => m.X == x && m.Y == y);
        if (occupied) throw new InvalidOperationException("Cell already occupied");
    }
}


