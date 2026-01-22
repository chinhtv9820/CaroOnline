using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace Caro.Services;

public class AiService : IAiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private const int BoardSize = 15;

    public AiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<AiMove> ChooseMoveAsync(int[,] board, int currentPlayer, AiDifficulty difficulty)
    {
        // 1. Chuyển đổi mảng 2 chiều [,] sang mảng jagged [][] để Python dễ đọc JSON
        int[][] jaggedBoard = new int[BoardSize][];
        for (int i = 0; i < BoardSize; i++)
        {
            jaggedBoard[i] = new int[BoardSize];
            for (int j = 0; j < BoardSize; j++)
            {
                jaggedBoard[i][j] = board[i, j];
            }
        }

        // 2. Tạo payload gửi đi
        var payload = new
        {
            board = jaggedBoard,
            currentPlayer = currentPlayer,
            difficulty = (int)difficulty
        };

        try
        {
            // 3. Gọi sang Python Service
            var client = _httpClientFactory.CreateClient("PythonAI");
            var response = await client.PostAsJsonAsync("/api/calculate-move", payload);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AiResponse>();
                if (result != null)
                {
                    return new AiMove(result.x, result.y);
                }
            }
            else 
            {
                Console.WriteLine($"Python AI Error: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            // Nếu Python server chưa bật hoặc lỗi, fallback về logic random đơn giản của C#
            Console.WriteLine($"Cannot connect to Python AI: {ex.Message}. Using C# fallback.");
        }

        // 4. Fallback (Dự phòng nếu Python lỗi): Random ô trống
        return GetRandomMove(board);
    }

    private AiMove GetRandomMove(int[,] board)
    {
        var empty = new List<AiMove>();
        for (int i = 0; i < BoardSize; i++)
            for (int j = 0; j < BoardSize; j++)
                if (board[i, j] == 0) empty.Add(new AiMove(i, j));

        if (empty.Count == 0) return new AiMove(-1, -1);
        var rand = new Random();
        return empty[rand.Next(empty.Count)];
    }

    // Class để hứng dữ liệu trả về từ Python
    private record AiResponse(int x, int y);
}
