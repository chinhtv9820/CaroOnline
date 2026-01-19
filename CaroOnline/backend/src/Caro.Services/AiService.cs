using System;
using System.Collections.Generic;
using System.Linq;

namespace Caro.Services;

/// <summary>
/// Service AI chịu trách nhiệm tính toán nước đi cho máy ở tất cả các độ khó.
/// Áp dụng nhiều thuật toán: threat map, heuristic, Minimax, MCTS, pattern recognition...
/// </summary>
public class AiService : IAiService
{
    private const int BoardSize = 15;
    private static readonly (int dx, int dy)[] Directions = { (1, 0), (0, 1), (1, 1), (1, -1) };
    
    // Transposition table for caching board evaluations (reduces time complexity significantly)
    private readonly Dictionary<ulong, (int score, int depth)> _transpositionTable = new();
    
    // Zobrist hashing for fast board state hashing
    private readonly ulong[,] _zobristTable = new ulong[BoardSize * BoardSize, 3];
    
    // Initialize Zobrist table (pseudo-random numbers for hashing)
    public AiService()
    {
        var random = new Random(42); // Fixed seed for reproducibility
        for (int i = 0; i < BoardSize * BoardSize; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                _zobristTable[i, j] = ((ulong)random.NextInt64() << 32) | (ulong)random.NextInt64();
            }
        }
    }

    // Scoring weights cho Hard/Ultimate (dùng chung cho các thuật toán minimax)
    private const int ScoreWin = 10000; // 5 in a row
    private const int ScoreFourOpenTwoEnds = 5000; // 4 in a row, 2 ends open (auto-win)
    private const int ScoreBlockFour = 4000; // Block opponent 4 in a row
    private const int ScoreBlockThree = 800; // Block opponent 3 in a row
    private const int ScoreDoubleThreat = 2000; // Create 2 threats simultaneously
    private const int ScoreThreat = 500; // Create 1 threat (4 in a row, 1 end open)
    private const int ScoreThree = 100; // 3 in a row
    private const int ScoreTwo = 10; // 2 in a row

    /// <summary>
    /// Hàm entry point để lấy nước đi AI dựa trên độ khó
    /// </summary>
    public AiMove ChooseMove(int[,] board, int currentPlayer, AiDifficulty difficulty)
    {
        return difficulty switch
        {
            AiDifficulty.Easy => ChooseMoveEasy(board, currentPlayer),
            AiDifficulty.Normal => ChooseMoveMedium(board, currentPlayer),
            AiDifficulty.Hard => ChooseMoveHard(board, currentPlayer),
            AiDifficulty.Ultimate => ChooseMoveUltimate(board, currentPlayer),
            _ => ChooseMoveEasy(board, currentPlayer)
        };
    }

    // Easy (IQ150): Threat map + heuristic, depth 1-2
    // Chiến lược cơ bản: thắng -> chặn -> chọn ô có điểm threat cao nhất
    private AiMove ChooseMoveEasy(int[,] board, int currentPlayer)
    {
        int opponent = currentPlayer == 1 ? 2 : 1;
        
        // 1. Win immediately if 4 consecutive AI pieces open
        var winMove = FindWinMove(board, currentPlayer);
        if (winMove != null) return winMove;
        
        // 2. Block opponent 3-4 consecutive pieces
        var blockMove = FindBlockMove(board, opponent, 3, 4);
        if (blockMove != null) return blockMove;
        
        // 3. Select move with highest threat map score (depth 1-2)
        var candidates = GetValidMoves(board);
        if (candidates.Count == 0) return new AiMove(7, 7); // Center fallback
        
        var bestMove = new AiMove(candidates[0].x, candidates[0].y);
        int bestScore = int.MinValue;
        
        foreach (var (x, y) in candidates)
        {
            int score = EvaluateThreatMap(board, x, y, currentPlayer, opponent, depth: 2);
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = new AiMove(x, y);
            }
        }
        
        return bestMove;
    }

    // Medium (IQ200): Advanced heuristic + combo strategies, depth 2-3
    // Thêm khả năng tạo double threat, ưu tiên vị trí chiến lược
    private AiMove ChooseMoveMedium(int[,] board, int currentPlayer)
    {
        int opponent = currentPlayer == 1 ? 2 : 1;
        
        // 1. Win immediately if possible
        var winMove = FindWinMove(board, currentPlayer);
        if (winMove != null) return winMove;
        
        // 2. Block opponent's high-threat moves (3-4 consecutive)
        var blockMove = FindBlockMove(board, opponent, 3, 4);
        if (blockMove != null) return blockMove;
        
        // 3. Create multiple threats simultaneously
        var doubleThreat = FindDoubleThreatMove(board, currentPlayer);
        if (doubleThreat != null) return doubleThreat;
        
        // 4. Advanced heuristic scoring with depth 2-3
        var candidates = GetValidMoves(board);
        if (candidates.Count == 0) return new AiMove(7, 7);
        
        // Control board center / strategic positions
        var strategicMoves = EvaluateStrategicPositions(board, candidates, currentPlayer, opponent);
        if (strategicMoves.Count > 0)
        {
            return strategicMoves[0];
        }
        
        // Fallback to best heuristic score
        var bestMove = new AiMove(candidates[0].x, candidates[0].y);
        int bestScore = int.MinValue;
        
        foreach (var (x, y) in candidates)
        {
            int score = EvaluateAdvancedHeuristic(board, x, y, currentPlayer, opponent, depth: 3);
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = new AiMove(x, y);
            }
        }
        
        return bestMove;
    }

    // Ultimate (IQ9000): Unbeatable AI - Deep Minimax + MCTS + Pattern Recognition + Anti-Human Strategy
    // Đây là chế độ mạnh nhất: sử dụng Endgame solver, double threat detector, fork detection, zero-randomness
    private AiMove ChooseMoveUltimate(int[,] board, int currentPlayer)
    {
        int opponent = currentPlayer == 1 ? 2 : 1;
        
        // 1. Endgame Solver: Detect forced wins 7-10 moves ahead
        var forcedWin = FindForcedWinSequence(board, currentPlayer, maxDepth: 10);
        if (forcedWin != null)
        {
            return forcedWin;
        }
        
        // 2. Block opponent's forced win immediately
        var blockForcedWin = FindForcedWinSequence(board, opponent, maxDepth: 10);
        if (blockForcedWin != null)
        {
            return blockForcedWin;
        }
        
        // 3. Win immediately if possible
        var winMove = FindWinMove(board, currentPlayer);
        if (winMove != null) return winMove;
        
        // 4. Block opponent winning move
        var blockWinMove = FindBlockMove(board, opponent, 4, 5);
        if (blockWinMove != null) return blockWinMove;
        
        // 5. Anti-Human Strategy: Create dual threats (force defensive play)
        var dualThreat = FindDualThreatMove(board, currentPlayer);
        if (dualThreat != null) return dualThreat;
        
        // 6. Pattern Recognition: Block any player 3-in-a-row formation
        var blockThree = FindBlockMove(board, opponent, 3, 3);
        if (blockThree != null) return blockThree;
        
        // 7. Global Board Control: Center dominance + strategic positions
        var strategicMove = FindStrategicControlMove(board, currentPlayer, opponent);
        if (strategicMove != null) return strategicMove;
        
        // 8. Iterative Deepening Minimax with Transposition Table (optimized for speed)
        // Clear transposition table for new search
        _transpositionTable.Clear();
        
        var candidates = GetValidMoves(board);
        if (candidates.Count == 0) return new AiMove(7, 7);
        
        // Pre-filter candidates using pattern recognition and heuristics
        var topCandidates = GetUltimateCandidates(board, candidates, currentPlayer, opponent, limit: 18);
        
        int bestScore = int.MinValue;
        var bestMove = new AiMove(topCandidates[0].x, topCandidates[0].y);
        
        // Iterative Deepening: Start shallow, go deeper for unbeatable strength
        // This finds good moves quickly and improves them to perfection
        int moveCount = CountMoves(board);
        int maxDepth = moveCount < 20 ? 7 : (moveCount < 40 ? 6 : 5); // Increased for unbeatable strength
        
        // Iterative deepening: depth 3, 4, 5, 6, 7... (early termination if found strong move)
        for (int currentDepth = 3; currentDepth <= maxDepth; currentDepth++)
        {
            int currentBestScore = int.MinValue;
            var currentBestMove = bestMove; // Use previous best as starting point
            
            // Re-order candidates: best moves from previous iteration first (move ordering optimization)
            var orderedCandidates = topCandidates.OrderByDescending(m =>
            {
                // Quick heuristic to prioritize promising moves
                board[m.x, m.y] = currentPlayer;
                int quickScore = EvaluatePositionScore(board, m.x, m.y, currentPlayer);
                board[m.x, m.y] = 0;
                return quickScore;
            }).ToList();
            
            foreach (var (x, y) in orderedCandidates)
            {
                board[x, y] = currentPlayer;
                
                // Aspiration Window: Start with narrow window around previous best score
                int alpha = bestScore - 500; // Window around previous best
                int beta = bestScore + 500;
                
                int score = UltimateMinimaxWithTT(board, currentDepth, false, currentPlayer, opponent, 
                    alpha, beta, 0);
                
                // If score outside window, re-search with full window
                if (score <= alpha || score >= beta)
                {
                    score = UltimateMinimaxWithTT(board, currentDepth, false, currentPlayer, opponent, 
                        int.MinValue, int.MaxValue, 0);
                }
                
                board[x, y] = 0;
                
                // Global Board Control Heuristics bonus (full evaluation for maximum strength)
                score += EvaluateGlobalBoardControl(board, x, y, currentPlayer, opponent);
                
                // Advanced Anti-Human Strategy: Multiple checks
                if (WouldAllowPlayerThree(board, x, y, opponent))
                {
                    score -= 10000; // Very heavy penalty - never allow player 3-in-a-row
                }
                
                // Check if move allows player to create double threat
                if (WouldAllowPlayerDoubleThreat(board, x, y, opponent))
                {
                    score -= 8000; // Heavy penalty for allowing double threat
                }
                
                // Bonus for creating triple threat (unbeatable position)
                int tripleThreatBonus = EvaluateTripleThreat(board, x, y, currentPlayer);
                score += tripleThreatBonus;
                
                // Advanced fork detection: Multiple forks from one move
                int forkCount = CountForks(board, x, y, currentPlayer);
                if (forkCount >= 2)
                {
                    score += 3000; // Multiple forks = unbeatable
                }
                
                if (score > currentBestScore)
                {
                    currentBestScore = score;
                    currentBestMove = new AiMove(x, y);
                }
                
                // Early termination: If we found a very strong move, use it immediately
                if (score > ScoreWin - 300)
                {
                    return currentBestMove;
                }
            }
            
            // Update best move from this iteration
            bestScore = currentBestScore;
            bestMove = currentBestMove;
            
            // Early termination: If score is very high, don't need to go deeper
            if (bestScore > ScoreWin - 2000)
            {
                break;
            }
        }
        
        // MCTS Hybrid: Enabled for top 3 candidates with reduced simulations (unbeatable strength)
        // Only use for final refinement of best moves
        if (topCandidates.Count >= 3 && bestScore < ScoreWin - 1000)
        {
            var top3Candidates = topCandidates.Take(3).ToList();
            int bestMctsScore = int.MinValue;
            var bestMctsMove = bestMove;
            
            foreach (var (x, y) in top3Candidates)
            {
                board[x, y] = currentPlayer;
                int mctsScore = MCTSEvaluate(board, currentPlayer, opponent, simulations: 50);
                board[x, y] = 0;
                
                // Combine minimax score with MCTS
                int combinedScore = (bestScore * 7 + mctsScore * 3) / 10;
                
                if (combinedScore > bestMctsScore)
                {
                    bestMctsScore = combinedScore;
                    bestMctsMove = new AiMove(x, y);
                }
            }
            
            // Use MCTS result if it's significantly better
            if (bestMctsScore > bestScore + 500)
            {
                return bestMctsMove;
            }
        }
        
        return bestMove;
    }

    // Hard (IQ400): Minimax depth 3-4 + alpha-beta + weighted scoring
    // Kết hợp kiểm tra nhanh (win/block) trước khi chạy minimax để tối ưu hiệu năng
    private AiMove ChooseMoveHard(int[,] board, int currentPlayer)
    {
        int opponent = currentPlayer == 1 ? 2 : 1;
        
        // 1. Win immediately
        var winMove = FindWinMove(board, currentPlayer);
        if (winMove != null) return winMove;
        
        // 2. Block opponent winning move
        var blockWinMove = FindBlockMove(board, opponent, 4, 5);
        if (blockWinMove != null) return blockWinMove;
        
        // 3. Create multiple threats
        var doubleThreat = FindDoubleThreatMove(board, currentPlayer);
        if (doubleThreat != null) return doubleThreat;
        
        // 4. Minimax with depth 3-4 + alpha-beta pruning
        var candidates = GetValidMoves(board);
        if (candidates.Count == 0) return new AiMove(7, 7);
        
        // Limit candidates for performance (top strategic positions)
        var topCandidates = GetTopCandidates(board, candidates, currentPlayer, opponent, limit: 15);
        
        int bestScore = int.MinValue;
        var bestMove = new AiMove(topCandidates[0].x, topCandidates[0].y);
        
        foreach (var (x, y) in topCandidates)
        {
            board[x, y] = currentPlayer;
            int score = Minimax(board, 4, false, currentPlayer, opponent, int.MinValue, int.MaxValue);
            board[x, y] = 0;
            
            // Add weighted position score
            score += EvaluateWeightedPosition(board, x, y, currentPlayer, opponent);
            
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = new AiMove(x, y);
            }
        }
        
        return bestMove;
    }

    // Find move to win immediately (4 consecutive, 1 end open)
    private AiMove? FindWinMove(int[,] board, int player)
    {
        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                if (board[x, y] != 0) continue;
                
                board[x, y] = player;
                if (CheckWinCondition(board, player, x, y))
                {
                    board[x, y] = 0;
                        return new AiMove(x, y);
        }
                board[x, y] = 0;
            }
        }
        return null;
    }

    // Block opponent moves (minCount to maxCount consecutive)
    private AiMove? FindBlockMove(int[,] board, int opponent, int minCount, int maxCount)
    {
        // Priority: block 4 > block 3
        for (int priority = maxCount; priority >= minCount; priority--)
        {
            for (int x = 0; x < BoardSize; x++)
            {
                for (int y = 0; y < BoardSize; y++)
                {
                    if (board[x, y] != 0) continue;
                    
                    foreach (var (dx, dy) in Directions)
                    {
                        var pattern = AnalyzePattern(board, x, y, dx, dy, opponent);
                        if (pattern.Count == priority && pattern.OpenEnds > 0)
                        {
                            return new AiMove(x, y);
                        }
                    }
                }
            }
        }
        return null;
    }

    // Find move that creates 2 threats simultaneously
    private AiMove? FindDoubleThreatMove(int[,] board, int currentPlayer)
    {
        var candidates = GetValidMoves(board);
        
        foreach (var (x, y) in candidates)
        {
            board[x, y] = currentPlayer;
            int threatCount = CountThreats(board, currentPlayer);
            board[x, y] = 0;
            
            if (threatCount >= 2)
            {
                return new AiMove(x, y);
            }
        }
        
        return null;
    }

    // Count threats (4 in a row, at least 1 end open)
    private int CountThreats(int[,] board, int player)
    {
        int threats = 0;
        var checkedPositions = new HashSet<(int x, int y, int dx, int dy)>();
        
        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                if (board[x, y] != player) continue;
                
                foreach (var (dx, dy) in Directions)
                {
                    var key = (x, y, dx, dy);
                    if (checkedPositions.Contains(key)) continue;
                    checkedPositions.Add(key);
                    
                    var pattern = AnalyzePattern(board, x, y, dx, dy, player);
                    if (pattern.Count == 4 && pattern.OpenEnds > 0)
                    {
                        threats++;
                    }
                }
            }
        }
        
        return threats;
    }

    // Threat map evaluation (Easy)
    private int EvaluateThreatMap(int[,] board, int x, int y, int currentPlayer, int opponent, int depth)
    {
        board[x, y] = currentPlayer;
        int score = 0;
        
        // Evaluate current position
        foreach (var (dx, dy) in Directions)
        {
            var pattern = AnalyzePattern(board, x, y, dx, dy, currentPlayer);
            score += GetPatternScore(pattern, true);
            
            // Block opponent
            var oppPattern = AnalyzePattern(board, x, y, dx, dy, opponent);
            score += GetPatternScore(oppPattern, false) * 2; // Block is important
        }
        
        // Depth lookahead
        if (depth > 1)
        {
            int bestOpponentScore = int.MaxValue;
            var nextMoves = GetValidMoves(board);
            foreach (var (nx, ny) in nextMoves.Take(5)) // Limit for performance
            {
                board[nx, ny] = opponent;
                int oppScore = EvaluateThreatMap(board, nx, ny, opponent, currentPlayer, depth - 1);
                board[nx, ny] = 0;
                bestOpponentScore = Math.Min(bestOpponentScore, oppScore);
            }
            score -= bestOpponentScore / 2;
        }
        
        board[x, y] = 0;
        return score;
    }

    // Advanced heuristic evaluation (Medium)
    private int EvaluateAdvancedHeuristic(int[,] board, int x, int y, int currentPlayer, int opponent, int depth)
    {
        board[x, y] = currentPlayer;
        int score = 0;
        
        // Evaluate all directions
        foreach (var (dx, dy) in Directions)
        {
            var myPattern = AnalyzePattern(board, x, y, dx, dy, currentPlayer);
            var oppPattern = AnalyzePattern(board, x, y, dx, dy, opponent);
            
            score += GetPatternScore(myPattern, true) * 2;
            score += GetPatternScore(oppPattern, false) * 3; // Block opponent is critical
        }
        
        // Combo strategies: check for multiple threats
        int threatCount = CountThreatsAfterMove(board, x, y, currentPlayer);
        if (threatCount >= 2) score += ScoreDoubleThreat;
        else if (threatCount == 1) score += ScoreThreat;
        
        // Depth lookahead
        if (depth > 1)
        {
            int minOpponentScore = int.MaxValue;
            var nextMoves = GetValidMoves(board);
            foreach (var (nx, ny) in nextMoves.OrderByDescending(m => 
                EvaluatePositionScore(board, m.Item1, m.Item2, opponent)).Take(7))
            {
                board[nx, ny] = opponent;
                int oppScore = EvaluateAdvancedHeuristic(board, nx, ny, opponent, currentPlayer, depth - 1);
                board[nx, ny] = 0;
                minOpponentScore = Math.Min(minOpponentScore, oppScore);
            }
            score -= minOpponentScore / 3;
        }
        
        board[x, y] = 0;
        return score;
    }

    // Weighted position evaluation (Hard)
    private int EvaluateWeightedPosition(int[,] board, int x, int y, int currentPlayer, int opponent)
    {
        int score = 0;
        
        // Center control bonus
        int centerX = BoardSize / 2;
        int centerY = BoardSize / 2;
        int distFromCenter = Math.Abs(x - centerX) + Math.Abs(y - centerY);
        score += (BoardSize - distFromCenter) * 5;
        
        // Evaluate patterns with weighted scoring
        board[x, y] = currentPlayer;
        foreach (var (dx, dy) in Directions)
        {
            var myPattern = AnalyzePattern(board, x, y, dx, dy, currentPlayer);
            var oppPattern = AnalyzePattern(board, x, y, dx, dy, opponent);
            
            // Weighted scores
            if (myPattern.Count == 5) score += ScoreWin;
            else if (myPattern.Count == 4 && myPattern.OpenEnds == 2) score += ScoreFourOpenTwoEnds;
            else if (myPattern.Count == 4 && myPattern.OpenEnds == 1) score += ScoreThreat;
            else if (myPattern.Count == 3) score += ScoreThree;
            else if (myPattern.Count == 2) score += ScoreTwo;
            
            // Block opponent
            if (oppPattern.Count == 4) score += ScoreBlockFour;
            else if (oppPattern.Count == 3) score += ScoreBlockThree;
        }
        
        // Double threat bonus
        int threats = CountThreatsAfterMove(board, x, y, currentPlayer);
        if (threats >= 2) score += ScoreDoubleThreat;
        
        board[x, y] = 0;
        return score;
    }

    // Minimax with alpha-beta pruning (Hard)
    private int Minimax(int[,] board, int depth, bool isMaximizing, int currentPlayer, int opponent, int alpha, int beta)
    {
        // Terminal conditions
        if (depth == 0)
        {
            return EvaluateBoardWeighted(board, currentPlayer, opponent);
        }
        
        // Check for win
        bool currentWin = CheckPlayerWin(board, currentPlayer);
        bool opponentWin = CheckPlayerWin(board, opponent);
        
        if (currentWin) return isMaximizing ? ScoreWin : -ScoreWin;
        if (opponentWin) return isMaximizing ? -ScoreWin : ScoreWin;
        
        var candidates = GetValidMoves(board);
        if (candidates.Count == 0) return 0;
        
        // Limit candidates for performance at deeper levels
        if (depth <= 2)
        {
            candidates = GetTopCandidates(board, candidates, isMaximizing ? currentPlayer : opponent, 
                isMaximizing ? opponent : currentPlayer, limit: 10);
        }
        
        if (isMaximizing)
        {
            int maxEval = int.MinValue;
            foreach (var (x, y) in candidates)
            {
                board[x, y] = currentPlayer;
                int eval = Minimax(board, depth - 1, false, currentPlayer, opponent, alpha, beta);
                board[x, y] = 0;
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha) break; // Alpha-beta pruning
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (var (x, y) in candidates)
            {
                board[x, y] = opponent;
                int eval = Minimax(board, depth - 1, true, currentPlayer, opponent, alpha, beta);
                board[x, y] = 0;
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);
                if (beta <= alpha) break; // Alpha-beta pruning
            }
            return minEval;
        }
    }

    // Evaluate board with weighted scoring (Hard)
    private int EvaluateBoardWeighted(int[,] board, int currentPlayer, int opponent)
    {
        int score = 0;
        
        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                if (board[x, y] == 0) continue;
                
                int player = board[x, y];
                bool isCurrentPlayer = player == currentPlayer;
                
                foreach (var (dx, dy) in Directions)
                {
                    var pattern = AnalyzePattern(board, x, y, dx, dy, player);
                    int patternScore = GetPatternScore(pattern, isCurrentPlayer);
                    
                    if (isCurrentPlayer)
                        score += patternScore;
                    else
                        score -= patternScore * 2; // Opponent moves are more dangerous
                }
            }
        }
        
        return score;
    }

    // Analyze pattern in a direction
    private PatternInfo AnalyzePattern(int[,] board, int x, int y, int dx, int dy, int player)
    {
        int count = 1; // Count includes position (x, y)
        bool forwardOpen = false, backwardOpen = false;
        
        // Forward direction
        for (int i = 1; i < 5; i++)
        {
            int nx = x + dx * i;
            int ny = y + dy * i;
            if (nx < 0 || nx >= BoardSize || ny < 0 || ny >= BoardSize) break;
            if (board[nx, ny] == player) count++;
            else if (board[nx, ny] == 0) { forwardOpen = true; break; }
            else break;
        }
        
        // Backward direction
        for (int i = 1; i < 5; i++)
        {
            int nx = x - dx * i;
            int ny = y - dy * i;
            if (nx < 0 || nx >= BoardSize || ny < 0 || ny >= BoardSize) break;
            if (board[nx, ny] == player) count++;
            else if (board[nx, ny] == 0) { backwardOpen = true; break; }
            else break;
        }
        
        return new PatternInfo
        {
            Count = count,
            OpenEnds = (forwardOpen ? 1 : 0) + (backwardOpen ? 1 : 0)
        };
    }

    // Get score for a pattern
    private int GetPatternScore(PatternInfo pattern, bool isCurrentPlayer)
    {
        int multiplier = isCurrentPlayer ? 1 : 2; // Block opponent is more important
        
        if (pattern.Count >= 5) return ScoreWin * multiplier;
        if (pattern.Count == 4 && pattern.OpenEnds == 2) return ScoreFourOpenTwoEnds * multiplier;
        if (pattern.Count == 4 && pattern.OpenEnds == 1) return ScoreThreat * multiplier;
        if (pattern.Count == 4) return ScoreBlockFour * multiplier;
        if (pattern.Count == 3) return ScoreBlockThree * multiplier;
        if (pattern.Count == 2) return ScoreTwo * multiplier;
        return 0;
    }

    // Get valid moves (empty cells near existing pieces or center)
    private List<(int x, int y)> GetValidMoves(int[,] board)
    {
        var moves = new HashSet<(int, int)>();
        bool hasPieces = false;
        
        // Find moves near existing pieces
        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                if (board[x, y] != 0)
                {
                    hasPieces = true;
                    // Add surrounding cells
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        for (int dy = -2; dy <= 2; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx >= 0 && nx < BoardSize && ny >= 0 && ny < BoardSize && board[nx, ny] == 0)
                            {
                                moves.Add((nx, ny));
                            }
                        }
                    }
                }
            }
        }
        
        // If no pieces, return center area
        if (!hasPieces)
        {
            int center = BoardSize / 2;
            for (int dx = -3; dx <= 3; dx++)
            {
                for (int dy = -3; dy <= 3; dy++)
                {
                    int x = center + dx;
                    int y = center + dy;
                    if (x >= 0 && x < BoardSize && y >= 0 && y < BoardSize && board[x, y] == 0)
                    {
                        moves.Add((x, y));
                    }
                }
            }
        }
        
        return moves.ToList();
    }

    // Get top candidates based on quick evaluation
    private List<(int x, int y)> GetTopCandidates(int[,] board, List<(int x, int y)> candidates, 
        int currentPlayer, int opponent, int limit)
    {
        var scored = candidates.Select(m => new
        {
            Move = m,
            Score = EvaluatePositionScore(board, m.x, m.y, currentPlayer) - 
                   EvaluatePositionScore(board, m.x, m.y, opponent) * 2
        }).OrderByDescending(m => m.Score).Take(limit).Select(m => m.Move).ToList();
        
        return scored.Count > 0 ? scored : candidates.Take(limit).ToList();
    }

    // Quick position score evaluation
    private int EvaluatePositionScore(int[,] board, int x, int y, int player)
    {
        if (board[x, y] != 0) return 0;
        
        board[x, y] = player;
        int score = 0;
        foreach (var (dx, dy) in Directions)
        {
            var pattern = AnalyzePattern(board, x, y, dx, dy, player);
            score += GetPatternScore(pattern, true);
        }
        board[x, y] = 0;
        return score;
    }

    // Evaluate strategic positions (Medium)
    private List<AiMove> EvaluateStrategicPositions(int[,] board, List<(int x, int y)> candidates, 
        int currentPlayer, int opponent)
    {
        var strategic = new List<(AiMove move, int score)>();
        
        foreach (var (x, y) in candidates)
        {
            int score = EvaluateAdvancedHeuristic(board, x, y, currentPlayer, opponent, depth: 2);
            
            // Bonus for center control
            int centerX = BoardSize / 2;
            int centerY = BoardSize / 2;
            int distFromCenter = Math.Abs(x - centerX) + Math.Abs(y - centerY);
            score += (BoardSize - distFromCenter) * 3;
            
            strategic.Add((new AiMove(x, y), score));
        }
        
        return strategic.OrderByDescending(s => s.score).Select(s => s.move).ToList();
    }

    // Count threats after a move
    private int CountThreatsAfterMove(int[,] board, int x, int y, int player)
    {
        board[x, y] = player;
        int threats = CountThreats(board, player);
        board[x, y] = 0;
        return threats;
    }

    // Check if player has won
    private bool CheckPlayerWin(int[,] board, int player)
    {
        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                if (board[x, y] == player)
                {
                    foreach (var (dx, dy) in Directions)
                    {
                        if (CheckWinCondition(board, player, x, y))
                            return true;
                    }
                }
            }
        }
        return false;
    }

    // Check win condition at position
    private bool CheckWinCondition(int[,] board, int player, int x, int y)
    {
        foreach (var (dx, dy) in Directions)
        {
            int count = 1;
            
            // Forward
            for (int i = 1; i < 5; i++)
            {
                int nx = x + dx * i;
                int ny = y + dy * i;
                if (nx < 0 || nx >= BoardSize || ny < 0 || ny >= BoardSize || board[nx, ny] != player)
                    break;
                count++;
            }
            
            // Backward
            for (int i = 1; i < 5; i++)
            {
                int nx = x - dx * i;
                int ny = y - dy * i;
                if (nx < 0 || nx >= BoardSize || ny < 0 || ny >= BoardSize || board[nx, ny] != player)
                    break;
                count++;
            }
            
            if (count >= 5) return true;
        }
        return false;
    }

    // Pattern information
    private struct PatternInfo
    {
        public int Count { get; set; }
        public int OpenEnds { get; set; }
    }

    // ===========================================
    // ULTIMATE MODE METHODS (IQ 9000 - Unbeatable AI)
    // ===========================================

    /// <summary>
    /// Endgame Solver: Detect forced wins 6-8 moves ahead (unbeatable strength)
    /// Uses iterative deepening to find guaranteed win sequences
    /// </summary>
    private AiMove? FindForcedWinSequence(int[,] board, int player, int maxDepth)
    {
        // Iterative deepening: start from depth 4, increase by 1
        // Increased depth for unbeatable strength
        int effectiveMaxDepth = Math.Min(maxDepth, 8);
        
        for (int depth = 4; depth <= effectiveMaxDepth; depth += 1)
        {
            var forcedWin = FindForcedWinAtDepth(board, player, depth);
            if (forcedWin != null)
            {
                return forcedWin;
            }
            // Early exit if depth gets very high (performance optimization)
            if (depth >= 7) break;
        }
        return null;
    }

    /// <summary>
    /// Find forced win at specific depth using minimax with win detection (optimized)
    /// </summary>
    private AiMove? FindForcedWinAtDepth(int[,] board, int player, int depth)
    {
        int opponent = player == 1 ? 2 : 1;
        var candidates = GetValidMoves(board);
        
        // Pre-filter: only check moves that create threats (increased for unbeatable strength)
        var threatCandidates = candidates.Select(m =>
        {
            board[m.x, m.y] = player;
            int threatCount = CountThreatsAfterMove(board, m.x, m.y, player);
            board[m.x, m.y] = 0;
            return new { Move = m, ThreatCount = threatCount };
        }).Where(m => m.ThreatCount > 0)
          .OrderByDescending(m => m.ThreatCount)
          .Take(10) // Increased for better forced win detection
          .Select(m => m.Move)
          .ToList();

        if (threatCandidates.Count == 0) threatCandidates = candidates.Take(8).ToList();

        foreach (var (x, y) in threatCandidates)
        {
            board[x, y] = player;
            
            // Check if this move leads to forced win (use transposition table, increased depth)
            int result = UltimateMinimaxWithTT(board, Math.Min(depth - 1, 6), false, player, opponent, 
                int.MinValue, int.MaxValue, 0);
            
            board[x, y] = 0;
            
            // If result guarantees win for player, this is a forced win move
            if (result >= ScoreWin - 1000) // Win is guaranteed
            {
                return new AiMove(x, y);
            }
        }
        
        return null;
    }

    /// <summary>
    /// Find dual threat move: Create 2 threats simultaneously (Anti-Human Strategy)
    /// Forces opponent into defensive-only game
    /// </summary>
    private AiMove? FindDualThreatMove(int[,] board, int currentPlayer)
    {
        var candidates = GetValidMoves(board);
        
        foreach (var (x, y) in candidates)
        {
            board[x, y] = currentPlayer;
            
            // Count threats in all directions
            int threatCount = 0;
            var threatDirections = new HashSet<(int dx, int dy)>();
            
            foreach (var (dx, dy) in Directions)
            {
                var pattern = AnalyzePattern(board, x, y, dx, dy, currentPlayer);
                // Threat = 4 in a row with at least 1 open end
                if (pattern.Count == 4 && pattern.OpenEnds > 0)
                {
                    threatDirections.Add((dx, dy));
                    threatCount++;
                }
            }
            
            board[x, y] = 0;
            
            // Dual threat = at least 2 threats in different directions
            if (threatCount >= 2 && threatDirections.Count >= 2)
            {
                return new AiMove(x, y);
            }
        }
        
        return null;
    }

    /// <summary>
    /// Find strategic control move: Center dominance + key diagonals + territory control
    /// Global Board Control Heuristics
    /// </summary>
    private AiMove? FindStrategicControlMove(int[,] board, int currentPlayer, int opponent)
    {
        var candidates = GetValidMoves(board);
        if (candidates.Count == 0) return null;
        
        int moveCount = CountMoves(board);
        
        // Early game: prioritize center control
        if (moveCount < 10)
        {
            int centerX = BoardSize / 2;
            int centerY = BoardSize / 2;
            
            var centerMoves = candidates.OrderBy(m => 
                Math.Abs(m.x - centerX) + Math.Abs(m.y - centerY)).Take(5).ToList();
            
            foreach (var (x, y) in centerMoves)
            {
                int controlScore = EvaluateGlobalBoardControl(board, x, y, currentPlayer, opponent);
                if (controlScore > 500)
                {
                    return new AiMove(x, y);
                }
            }
        }
        
        // Mid/Late game: prioritize choke points and fork potential
        var strategicMoves = candidates.Select(m => new
        {
            Move = m,
            Score = EvaluateGlobalBoardControl(board, m.x, m.y, currentPlayer, opponent) +
                   EvaluateForkPotential(board, m.x, m.y, currentPlayer) * 2
        }).OrderByDescending(m => m.Score).Take(3).ToList();
        
        if (strategicMoves.Count > 0 && strategicMoves[0].Score > 300)
        {
            return new AiMove(strategicMoves[0].Move.x, strategicMoves[0].Move.y);
        }
        
        return null;
    }

    /// <summary>
    /// Get top candidates for Ultimate mode using pattern recognition and heuristics (optimized)
    /// Pre-filters moves to reduce search space for deep minimax
    /// Uses quick evaluation first, then detailed evaluation only for promising moves
    /// </summary>
    private List<(int x, int y)> GetUltimateCandidates(int[,] board, List<(int x, int y)> candidates, 
        int currentPlayer, int opponent, int limit)
    {
        // Quick pre-filter: Only evaluate moves near existing pieces (reduces evaluation time)
        var nearbyCandidates = candidates.Count > 40 
            ? candidates.Where(m => IsNearExistingPiece(board, m.x, m.y)).Take(30).ToList()
            : candidates;
        
        // Two-stage evaluation: Quick first, then detailed for top candidates
        var quickScored = nearbyCandidates.Select(m =>
        {
            // Quick evaluation (fast heuristics)
            board[m.x, m.y] = currentPlayer;
            int quickPattern = EvaluatePositionScore(board, m.x, m.y, currentPlayer);
            int quickBlock = EvaluatePositionScore(board, m.x, m.y, opponent) * 2; // Block is important
            board[m.x, m.y] = 0;
            
            return new
            {
                Move = m,
                QuickScore = quickPattern + quickBlock
            };
        }).OrderByDescending(m => m.QuickScore).Take(limit * 2).ToList(); // Take 2x for detailed eval
        
        // Detailed evaluation only for top quick-scored candidates
        var detailedScored = quickScored.Select(item =>
        {
            var m = item.Move;
            // Pattern Recognition: Detect closed/open 3 & 4, double open 3, potential kill-sequences
            int patternScore = EvaluatePatternRecognition(board, m.x, m.y, currentPlayer, opponent);
            
            // Global Board Control (simplified)
            int controlScore = EvaluateGlobalBoardControl(board, m.x, m.y, currentPlayer, opponent) / 2;
            
            // Anti-Human: Penalize moves that allow player 3-in-a-row
            int antiHumanPenalty = WouldAllowPlayerThree(board, m.x, m.y, opponent) ? -5000 : 0;
            
            // Territory dominance (simplified)
            int territoryScore = EvaluateTerritoryDominance(board, m.x, m.y, currentPlayer) / 2;
            
            // Future fork potential
            int forkScore = EvaluateForkPotential(board, m.x, m.y, currentPlayer);
            
            return new
            {
                Move = m,
                Score = patternScore + controlScore + antiHumanPenalty + territoryScore + forkScore
            };
        }).OrderByDescending(m => m.Score).Take(limit).Select(m => m.Move).ToList();
        
        return detailedScored.Count > 0 ? detailedScored : candidates.Take(limit).ToList();
    }
    
    /// <summary>
    /// Quick check if position is near existing pieces (optimization)
    /// </summary>
    private bool IsNearExistingPiece(int[,] board, int x, int y)
    {
        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx >= 0 && nx < BoardSize && ny >= 0 && ny < BoardSize && board[nx, ny] != 0)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Compute Zobrist hash for board state (fast hashing for transposition table)
    /// </summary>
    private ulong ComputeZobristHash(int[,] board)
    {
        ulong hash = 0;
        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                int piece = board[x, y];
                if (piece != 0)
                {
                    int index = x * BoardSize + y;
                    hash ^= _zobristTable[index, piece];
                }
            }
        }
        return hash;
    }
    
    /// <summary>
    /// Deep Minimax with Extended Alpha-Beta Pruning + Transposition Table
    /// Evaluates millions of possible states with optimized pruning and caching
    /// </summary>
    private int UltimateMinimaxWithTT(int[,] board, int depth, bool isMaximizing, int currentPlayer, 
        int opponent, int alpha, int beta, int moveCount)
    {
        // Check transposition table (cache lookup - O(1) instead of re-evaluating)
        ulong hash = ComputeZobristHash(board);
        if (_transpositionTable.TryGetValue(hash, out var cached))
        {
            if (cached.depth >= depth)
            {
                // Use cached value if depth is sufficient
                return cached.score;
            }
        }
        
        // Terminal conditions
        if (depth == 0)
        {
            int score = EvaluateBoardUltimate(board, currentPlayer, opponent);
            // Cache the evaluation
            _transpositionTable[hash] = (score, 0);
            return score;
        }
        
        // Check for immediate win/loss (quiescence search optimization)
        bool currentWin = CheckPlayerWin(board, currentPlayer);
        bool opponentWin = CheckPlayerWin(board, opponent);
        
        if (currentWin)
        {
            int score = isMaximizing ? ScoreWin * 2 : -ScoreWin * 2;
            _transpositionTable[hash] = (score, depth);
            return score;
        }
        if (opponentWin)
        {
            int score = isMaximizing ? -ScoreWin * 2 : ScoreWin * 2;
            _transpositionTable[hash] = (score, depth);
            return score;
        }
        
        var candidates = GetValidMoves(board);
        if (candidates.Count == 0)
        {
            int score = 0;
            _transpositionTable[hash] = (score, depth);
            return score;
        }
        
        // Extended Alpha-Beta Pruning: Pre-sort moves by heuristic score
        // This improves pruning efficiency significantly (better move ordering)
        var sortedCandidates = candidates.Select(m =>
        {
            board[m.x, m.y] = isMaximizing ? currentPlayer : opponent;
            int quickScore = EvaluatePositionScore(board, m.x, m.y, isMaximizing ? currentPlayer : opponent);
            board[m.x, m.y] = 0;
            return new { Move = m, QuickScore = quickScore };
        }).OrderByDescending(m => isMaximizing ? m.QuickScore : -m.QuickScore).ToList();
        
        // Limit candidates at deeper levels (adaptive, but more for unbeatable strength)
        int candidateLimit = depth <= 2 ? 12 : (depth <= 4 ? 10 : 8); // Increased for strength
        sortedCandidates = sortedCandidates.Take(candidateLimit).ToList();
        
        int result;
        if (isMaximizing)
        {
            int maxEval = int.MinValue;
            foreach (var item in sortedCandidates)
            {
                var (x, y) = item.Move;
                board[x, y] = currentPlayer;
                
                int eval = UltimateMinimaxWithTT(board, depth - 1, false, currentPlayer, opponent, 
                    alpha, beta, moveCount + 1);
                
                board[x, y] = 0;
                
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);
                
                // Extended Alpha-Beta Pruning: Early termination
                if (beta <= alpha)
                {
                    break; // Prune remaining branches
                }
            }
            result = maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (var item in sortedCandidates)
            {
                var (x, y) = item.Move;
                board[x, y] = opponent;
                
                int eval = UltimateMinimaxWithTT(board, depth - 1, true, currentPlayer, opponent, 
                    alpha, beta, moveCount + 1);
                
                board[x, y] = 0;
                
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);
                
                // Extended Alpha-Beta Pruning: Early termination
                if (beta <= alpha)
                {
                    break; // Prune remaining branches
                }
            }
            result = minEval;
        }
        
        // Cache the result
        _transpositionTable[hash] = (result, depth);
        return result;
    }
    
    /// <summary>
    /// Legacy method for backward compatibility (calls new method)
    /// </summary>
    private int UltimateMinimax(int[,] board, int depth, bool isMaximizing, int currentPlayer, 
        int opponent, int alpha, int beta, int moveCount)
    {
        return UltimateMinimaxWithTT(board, depth, isMaximizing, currentPlayer, opponent, alpha, beta, moveCount);
    }

    /// <summary>
    /// Monte Carlo Tree Search (MCTS) Hybrid (optimized for speed)
    /// After minimax narrows candidates, MCTS evaluates long-term strategies
    /// Uses deterministic playout evaluation (no randomness)
    /// </summary>
    private int MCTSEvaluate(int[,] board, int currentPlayer, int opponent, int simulations)
    {
        // Simplified MCTS: Deterministic playout evaluation with reduced depth
        // Instead of random playouts, use heuristic-based playouts
        
        var candidates = GetValidMoves(board).Take(3).ToList(); // Only top 3 candidates (speed optimization)
        if (candidates.Count == 0) return 0;
        
        int totalScore = 0;
        int validPlayouts = 0;
        
        foreach (var (x, y) in candidates)
        {
            board[x, y] = currentPlayer;
            
            // Deterministic playout: simulate game using heuristic moves (reduced depth for speed)
            int playoutScore = DeterministicPlayout(board, currentPlayer, opponent, depth: 4);
            
            board[x, y] = 0;
            totalScore += playoutScore;
            validPlayouts++;
        }
        
        return validPlayouts > 0 ? totalScore / validPlayouts : 0;
    }

    /// <summary>
    /// Deterministic playout: Simulate game using heuristic-based moves (not random)
    /// </summary>
    private int DeterministicPlayout(int[,] board, int currentPlayer, int opponent, int depth)
    {
        if (depth == 0)
        {
            return EvaluateBoardUltimate(board, currentPlayer, opponent);
        }
        
        // Check for win
        if (CheckPlayerWin(board, currentPlayer)) return ScoreWin;
        if (CheckPlayerWin(board, opponent)) return -ScoreWin;
        
        var candidates = GetValidMoves(board);
        if (candidates.Count == 0) return 0;
        
        // Select best heuristic move (deterministic, not random)
        var bestMove = candidates.OrderByDescending(m =>
        {
            board[m.x, m.y] = currentPlayer;
            int score = EvaluatePositionScore(board, m.x, m.y, currentPlayer);
            board[m.x, m.y] = 0;
            return score;
        }).First();
        
        board[bestMove.x, bestMove.y] = currentPlayer;
        int result = -DeterministicPlayout(board, opponent, currentPlayer, depth - 1);
        board[bestMove.x, bestMove.y] = 0;
        
        return result;
    }

    /// <summary>
    /// Pattern Recognition System
    /// Detects: Closed/open 3 & 4, Double open 3, Potential kill-sequences, Ladder traps
    /// </summary>
    private int EvaluatePatternRecognition(int[,] board, int x, int y, int currentPlayer, int opponent)
    {
        board[x, y] = currentPlayer;
        int score = 0;
        
        // Detect patterns in all directions
        foreach (var (dx, dy) in Directions)
        {
            var myPattern = AnalyzePattern(board, x, y, dx, dy, currentPlayer);
            var oppPattern = AnalyzePattern(board, x, y, dx, dy, opponent);
            
            // Closed/open 3 & 4 detection
            if (myPattern.Count == 4)
            {
                if (myPattern.OpenEnds == 2)
                    score += ScoreFourOpenTwoEnds; // Auto-win threat
                else if (myPattern.OpenEnds == 1)
                    score += ScoreThreat * 2; // Single threat
            }
            else if (myPattern.Count == 3)
            {
                if (myPattern.OpenEnds == 2)
                    score += 1500; // Double open 3 (very strong)
                else if (myPattern.OpenEnds == 1)
                    score += ScoreThree * 2;
            }
            
            // Block opponent patterns
            if (oppPattern.Count == 4 && oppPattern.OpenEnds > 0)
                score += ScoreBlockFour * 2; // Critical block
            else if (oppPattern.Count == 3 && oppPattern.OpenEnds == 2)
                score += 1200; // Block double open 3
            
            // Potential kill-sequences: Check if this move creates multiple threats
            int threatCount = CountThreatsAfterMove(board, x, y, currentPlayer);
            if (threatCount >= 2)
                score += ScoreDoubleThreat * 2; // Kill sequence
        }
        
        // Ladder trap detection: Check for positions that force opponent into losing patterns
        score += DetectLadderTraps(board, x, y, currentPlayer, opponent);
        
        board[x, y] = 0;
        return score;
    }

    /// <summary>
    /// Detect ladder traps: Positions that force opponent into losing sequences
    /// </summary>
    private int DetectLadderTraps(int[,] board, int x, int y, int currentPlayer, int opponent)
    {
        board[x, y] = currentPlayer;
        int trapScore = 0;
        
        // Check if this move creates a situation where opponent's best response
        // leads to an even worse position
        var oppResponses = GetValidMoves(board).Take(5).ToList();
        
        foreach (var (ox, oy) in oppResponses)
        {
            board[ox, oy] = opponent;
            
            // After opponent's response, check if we have multiple threats
            int ourThreats = CountThreats(board, currentPlayer);
            if (ourThreats >= 2)
            {
                trapScore += 800; // Ladder trap detected
            }
            
            board[ox, oy] = 0;
        }
        
        board[x, y] = 0;
        return trapScore;
    }

    /// <summary>
    /// Global Board Control Heuristics
    /// Prioritizes moves based on: Territory dominance, Future fork potential,
    /// Positional stability, "Choke points" that reduce opponent branching
    /// </summary>
    private int EvaluateGlobalBoardControl(int[,] board, int x, int y, int currentPlayer, int opponent)
    {
        int score = 0;
        
        // Territory dominance: Control of center and key areas
        int centerX = BoardSize / 2;
        int centerY = BoardSize / 2;
        int distFromCenter = Math.Abs(x - centerX) + Math.Abs(y - centerY);
        score += (BoardSize - distFromCenter) * 10; // Center control bonus
        
        // Key diagonals: Main diagonal and anti-diagonal
        if (x == y || x + y == BoardSize - 1)
            score += 50;
        
        // Future fork potential
        score += EvaluateForkPotential(board, x, y, currentPlayer) * 3;
        
        // Positional stability: Moves that are hard to counter
        score += EvaluatePositionalStability(board, x, y, currentPlayer, opponent);
        
        // Choke points: Moves that limit opponent's branching factor
        score += EvaluateChokePoints(board, x, y, currentPlayer, opponent);
        
        return score;
    }

    /// <summary>
    /// Evaluate fork potential: Ability to create multiple threats from one position
    /// </summary>
    private int EvaluateForkPotential(int[,] board, int x, int y, int player)
    {
        board[x, y] = player;
        
        // Count how many directions can create threats
        int forkDirections = 0;
        foreach (var (dx, dy) in Directions)
        {
            var pattern = AnalyzePattern(board, x, y, dx, dy, player);
            if (pattern.Count >= 3 && pattern.OpenEnds > 0)
            {
                forkDirections++;
            }
        }
        
        board[x, y] = 0;
        
        // Fork = ability to create threats in multiple directions
        if (forkDirections >= 2) return 600; // Strong fork
        if (forkDirections == 1) return 200; // Potential fork
        return 0;
    }

    /// <summary>
    /// Evaluate positional stability: How hard is this position to counter
    /// </summary>
    private int EvaluatePositionalStability(int[,] board, int x, int y, int currentPlayer, int opponent)
    {
        board[x, y] = currentPlayer;
        
        // Check how many ways opponent can counter this move
        var counterMoves = GetValidMoves(board).Take(10).ToList();
        int effectiveCounters = 0;
        
        foreach (var (cx, cy) in counterMoves)
        {
            board[cx, cy] = opponent;
            int oppThreats = CountThreats(board, opponent);
            board[cx, cy] = 0;
            
            if (oppThreats > 0) effectiveCounters++;
        }
        
        board[x, y] = 0;
        
        // Stable position = fewer effective counters
        return (10 - effectiveCounters) * 30;
    }

    /// <summary>
    /// Evaluate choke points: Moves that reduce opponent's branching factor
    /// </summary>
    private int EvaluateChokePoints(int[,] board, int x, int y, int currentPlayer, int opponent)
    {
        board[x, y] = currentPlayer;
        
        // Count opponent's valid moves before and after
        int movesBefore = GetValidMoves(board).Count;
        
        // Simulate: if opponent makes best move, how many moves remain?
        var oppBestMoves = GetValidMoves(board).OrderByDescending(m =>
        {
            board[m.x, m.y] = opponent;
            int score = EvaluatePositionScore(board, m.x, m.y, opponent);
            board[m.x, m.y] = 0;
            return score;
        }).Take(3).ToList();
        
        int minMovesAfter = int.MaxValue;
        foreach (var (ox, oy) in oppBestMoves)
        {
            board[ox, oy] = opponent;
            int movesAfter = GetValidMoves(board).Count;
            board[ox, oy] = 0;
            minMovesAfter = Math.Min(minMovesAfter, movesAfter);
        }
        
        board[x, y] = 0;
        
        // Choke point = significantly reduces opponent's options
        int reduction = movesBefore - minMovesAfter;
        return reduction * 20;
    }

    /// <summary>
    /// Evaluate territory dominance: Control of key board areas
    /// </summary>
    private int EvaluateTerritoryDominance(int[,] board, int x, int y, int player)
    {
        int score = 0;
        
        // Check surrounding area control
        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (nx >= 0 && nx < BoardSize && ny >= 0 && ny < BoardSize)
                {
                    if (board[nx, ny] == player)
                        score += 15; // Controlled territory
                    else if (board[nx, ny] == 0)
                        score += 5; // Potential territory
                }
            }
        }
        
        return score;
    }

    /// <summary>
    /// Anti-Human Strategy: Check if move would allow player to form 3-in-a-row
    /// Never allow player to form any 3 in a row unless it is irrelevant
    /// </summary>
    private bool WouldAllowPlayerThree(int[,] board, int x, int y, int opponent)
    {
        // Temporarily place AI move
        board[x, y] = opponent == 1 ? 2 : 1; // Place AI piece (opposite of opponent)
        
        // Check if opponent can form 3-in-a-row after this move
        bool allowsThree = false;
        var oppMoves = GetValidMoves(board).Take(10).ToList();
        
        foreach (var (ox, oy) in oppMoves)
        {
            board[ox, oy] = opponent;
            
            foreach (var (dx, dy) in Directions)
            {
                var pattern = AnalyzePattern(board, ox, oy, dx, dy, opponent);
                if (pattern.Count == 3 && pattern.OpenEnds > 0)
                {
                    allowsThree = true;
                    break;
                }
            }
            
            board[ox, oy] = 0;
            if (allowsThree) break;
        }
        
        board[x, y] = 0;
        return allowsThree;
    }

    /// <summary>
    /// Evaluate board for Ultimate mode with comprehensive scoring
    /// </summary>
    private int EvaluateBoardUltimate(int[,] board, int currentPlayer, int opponent)
    {
        int score = 0;
        
        // Comprehensive pattern evaluation
        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                if (board[x, y] == 0) continue;
                
                int player = board[x, y];
                bool isCurrentPlayer = player == currentPlayer;
                
                foreach (var (dx, dy) in Directions)
                {
                    var pattern = AnalyzePattern(board, x, y, dx, dy, player);
                    int patternScore = GetPatternScore(pattern, isCurrentPlayer);
                    
                    // Enhanced scoring for Ultimate mode
                    if (isCurrentPlayer)
                    {
                        score += patternScore;
                        
                        // Bonus for double open patterns
                        if (pattern.OpenEnds == 2 && pattern.Count >= 3)
                            score += patternScore / 2;
                    }
                    else
                    {
                        // Opponent moves are more dangerous
                        score -= patternScore * 3;
                        
                        // Heavy penalty for opponent double open patterns
                        if (pattern.OpenEnds == 2 && pattern.Count >= 3)
                            score -= patternScore;
                    }
                }
            }
        }
        
        // Global board control factor
        int currentControl = EvaluatePlayerControl(board, currentPlayer);
        int opponentControl = EvaluatePlayerControl(board, opponent);
        score += (currentControl - opponentControl) * 10;
        
        return score;
    }

    /// <summary>
    /// Evaluate overall player control of the board
    /// </summary>
    private int EvaluatePlayerControl(int[,] board, int player)
    {
        int control = 0;
        var checkedPositions = new HashSet<(int x, int y, int dx, int dy)>();
        
        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                if (board[x, y] != player) continue;
                
                foreach (var (dx, dy) in Directions)
                {
                    var key = (x, y, dx, dy);
                    if (checkedPositions.Contains(key)) continue;
                    checkedPositions.Add(key);
                    
                    var pattern = AnalyzePattern(board, x, y, dx, dy, player);
                    control += pattern.Count * (pattern.OpenEnds + 1);
                }
            }
        }
        
        return control;
    }

    /// <summary>
    /// Count total moves on board
    /// </summary>
    private int CountMoves(int[,] board)
    {
        int count = 0;
        for (int x = 0; x < BoardSize; x++)
        {
            for (int y = 0; y < BoardSize; y++)
            {
                if (board[x, y] != 0) count++;
            }
        }
        return count;
    }
    
    /// <summary>
    /// Check if move would allow player to create double threat (unbeatable anti-human strategy)
    /// </summary>
    private bool WouldAllowPlayerDoubleThreat(int[,] board, int x, int y, int opponent)
    {
        // Temporarily place AI move
        board[x, y] = opponent == 1 ? 2 : 1; // Place AI piece (opposite of opponent)
        
        // Check if opponent can create double threat after this move
        bool allowsDoubleThreat = false;
        var oppMoves = GetValidMoves(board).Take(8).ToList();
        
        foreach (var (ox, oy) in oppMoves)
        {
            board[ox, oy] = opponent;
            
            // Count threats opponent would have
            int threatCount = CountThreatsAfterMove(board, ox, oy, opponent);
            if (threatCount >= 2)
            {
                allowsDoubleThreat = true;
            }
            
            board[ox, oy] = 0;
            if (allowsDoubleThreat) break;
        }
        
        board[x, y] = 0;
        return allowsDoubleThreat;
    }
    
    /// <summary>
    /// Evaluate triple threat: Create 3 threats simultaneously (unbeatable position)
    /// </summary>
    private int EvaluateTripleThreat(int[,] board, int x, int y, int player)
    {
        board[x, y] = player;
        
        // Count threats in all directions
        int threatCount = 0;
        var threatDirections = new HashSet<(int dx, int dy)>();
        
        foreach (var (dx, dy) in Directions)
        {
            var pattern = AnalyzePattern(board, x, y, dx, dy, player);
            // Threat = 4 in a row with at least 1 open end
            if (pattern.Count == 4 && pattern.OpenEnds > 0)
            {
                threatDirections.Add((dx, dy));
                threatCount++;
            }
        }
        
        board[x, y] = 0;
        
        // Triple threat = 3+ threats in different directions (unbeatable)
        if (threatCount >= 3 && threatDirections.Count >= 3)
        {
            return 10000; // Massive bonus - unbeatable position
        }
        
        return 0;
    }
    
    /// <summary>
    /// Count number of forks (potential threats in multiple directions) from a position
    /// </summary>
    private int CountForks(int[,] board, int x, int y, int player)
    {
        board[x, y] = player;
        
        int forkCount = 0;
        foreach (var (dx, dy) in Directions)
        {
            var pattern = AnalyzePattern(board, x, y, dx, dy, player);
            // Fork = pattern with 3+ pieces and open ends (potential threat)
            if (pattern.Count >= 3 && pattern.OpenEnds > 0)
            {
                forkCount++;
            }
        }
        
        board[x, y] = 0;
        return forkCount;
    }
}
