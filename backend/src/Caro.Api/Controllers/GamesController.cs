using Caro.Core.Entities;
using Caro.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Caro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
/// <summary>
/// Controller cung cấp API cho các thao tác game (PvE):
/// - Lấy danh sách game, chi tiết, moves
/// - Tạo game mới (PvE)
/// - Thực hiện nước đi & gọi AI
/// - Đầu hàng
/// </summary>
public class GamesController : ControllerBase
{
    private readonly IGameService _gameService;
    private readonly IAiService _aiService;

    public GamesController(IGameService gameService, IAiService aiService)
    {
        _gameService = gameService;
        _aiService = aiService;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var games = await _gameService.GetGamesAsync(ct);
        return Ok(games.Select(g => new { g.Id, g.Mode, g.Result, g.CreatedAt, g.FinishedAt }));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct)
    {
        var game = await _gameService.GetGameAsync(id, ct);
        if (game == null) return NotFound();
        
        // Include moves for frontend
        var moves = await _gameService.GetMovesAsync(id, ct);
        
        return Ok(new
        {
            game.Id,
            game.Mode,
            game.P1UserId,
            game.P2UserId,
            game.CurrentTurn,
            game.FinishedAt,
            game.Result,
            game.TimeControlSeconds,
            game.RoomOwnerId,
            moves = moves.Select(m => new { m.Id, m.Player, m.X, m.Y, m.MoveNumber, m.CreatedAt })
        });
    }

    [HttpGet("{id}/moves")]
    public async Task<IActionResult> Moves(int id, CancellationToken ct)
    {
        var moves = await _gameService.GetMovesAsync(id, ct);
        return Ok(moves);
    }

    /// <summary>
    /// DTO cho request đánh một nước (X,Y). Player null mặc định = 1 (người chơi)
    /// </summary>
    public record MakeMoveRequest(int X, int Y, int? Player);

    [HttpPost("{id}/moves")]
    public async Task<IActionResult> MakeMove(int id, [FromBody] MakeMoveRequest req, CancellationToken ct)
    {
        try
        {
            var game = await _gameService.GetGameAsync(id, ct);
            if (game == null) return NotFound("Game not found");

            // Handle AI move request (x=-1, y=-1)
            // Frontend gửi ( -1, -1 ) khi người chơi yêu cầu AI đánh trong chế độ PvE
            if (req.X == -1 && req.Y == -1 && game.Mode == "PvE" && game.PveDifficulty.HasValue)
            {
                // Get all moves to build board state
                var moves = await _gameService.GetMovesAsync(id, ct);
                var board = BuildBoardFromMoves(moves);
                
                // AI luôn là player 2 trong PvE
                var aiMove = await _aiService.ChooseMoveAsync(board, 2, (AiDifficulty)game.PveDifficulty.Value);
                
                // Make AI move
                var move = await _gameService.MakeMoveAsync(id, 2, aiMove.X, aiMove.Y, ct);
                
                // Check if game ended
                var updatedGame = await _gameService.GetGameAsync(id, ct);
                bool gameFinished = updatedGame?.FinishedAt != null;
                int? winner = null;
                if (gameFinished && !string.IsNullOrEmpty(updatedGame?.Result))
                {
                    // In PvE: P1_WIN = player wins (1), P2_WIN = AI wins (2)
                    winner = updatedGame.Result.StartsWith("P1") ? 1 : 2;
                }

                return Ok(new
                {
                    move = new { move.Id, move.Player, move.X, move.Y, move.MoveNumber, move.CreatedAt },
                    gameFinished,
                    winner,
                    currentTurn = updatedGame?.CurrentTurn
                });
            }

            // Human player move
            int player = req.Player ?? 1;
            var humanMove = await _gameService.MakeMoveAsync(id, player, req.X, req.Y, ct);
            
            // Check if game ended
            var gameAfterMove = await _gameService.GetGameAsync(id, ct);
            bool finished = gameAfterMove?.FinishedAt != null;
            int? winPlayer = null;
            if (finished && !string.IsNullOrEmpty(gameAfterMove?.Result))
            {
                // In PvE: P1_WIN = player wins (1), P2_WIN = AI wins (2)
                winPlayer = gameAfterMove.Result.StartsWith("P1") ? 1 : 2;
            }

            return Ok(new
            {
                move = new { humanMove.Id, humanMove.Player, humanMove.X, humanMove.Y, humanMove.MoveNumber, humanMove.CreatedAt },
                gameFinished = finished,
                winner = winPlayer,
                currentTurn = gameAfterMove?.CurrentTurn
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Xây dựng lại ma trận bàn cờ từ danh sách moves để truyền cho AI
    /// </summary>
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

    public record CreateRequest(string Mode, int? P1UserId, int? P2UserId, int? PveDifficulty, int TimeControlSeconds);

    /// <summary>
    /// Tạo game PvE mới. PvP được tạo thông qua SignalR Hub.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest req, CancellationToken ct)
    {
        try
        {
            // PvE mode doesn't require authentication
            // PvP mode should be created via SignalR hub (which requires auth)
            if (req.Mode == "PvP")
            {
                return Unauthorized("PvP games must be created via SignalR hub");
            }
            
            var game = await _gameService.CreateGameAsync(new CreateGameOptions(req.Mode, req.P1UserId, req.P2UserId, req.PveDifficulty, req.TimeControlSeconds), ct);
            return Ok(game);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Người chơi đầu hàng trong PvE (player 1). PvP xử lý qua SignalR.
    /// </summary>
    [HttpPost("{id}/resign")]
    public async Task<IActionResult> Resign(int id, CancellationToken ct)
    {
        try
        {
            var game = await _gameService.GetGameAsync(id, ct);
            if (game == null) return NotFound("Game not found");
            
            if (game.FinishedAt != null)
            {
                return BadRequest("Game is already finished");
            }
            
            // In PvE mode, player 1 is the human player
            // Resigning means player 1 loses, so player 2 (AI) wins
            int resigningPlayer = 1; // Always player 1 in PvE
            
            await _gameService.ResignAsync(id, resigningPlayer, ct);
            
            var updatedGame = await _gameService.GetGameAsync(id, ct);
            int? winner = null;
            if (updatedGame != null && !string.IsNullOrEmpty(updatedGame.Result))
            {
                // In PvE: P1_WIN_RESIGN means player resigned, so AI (P2) wins
                winner = updatedGame.Result.StartsWith("P1") ? 1 : 2;
            }
            
            return Ok(new
            {
                gameFinished = true,
                winner,
                result = updatedGame?.Result
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace });
        }
    }
}


