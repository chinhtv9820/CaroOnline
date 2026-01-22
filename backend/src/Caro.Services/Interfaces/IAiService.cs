namespace Caro.Services;

public enum AiDifficulty
{
    Easy = 1,
    Normal = 2,
    Hard = 3,
    Ultimate = 4
}

public record AiMove(int X, int Y);

public interface IAiService
{
    // Thêm 'Task' vào kiểu trả về để hỗ trợ async
    Task<AiMove> ChooseMoveAsync(int[,] board, int currentPlayer, AiDifficulty difficulty);
}
