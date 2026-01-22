from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import List, Tuple
import time

app = FastAPI()

# --- CẤU HÌNH ---
BOARD_SIZE = 15
EMPTY = 0
MAX_CANDIDATES = 8  # CHÌA KHÓA TỐI ƯU: Chỉ xét tối đa 8 nước đi tốt nhất mỗi lượt

# Điểm số Heuristic
SCORES = {
    (5, 2): 100000000,
    (5, 1): 100000000,
    (5, 0): 100000000,
    (4, 2): 10000000, # Chắc chắn thắng
    (4, 1): 50000,    # Nguy hiểm cấp độ đỏ
    (3, 2): 10000,    # Nguy hiểm cấp độ cam
    (3, 1): 100,       
    (2, 2): 100,
    (2, 1): 5,
    (1, 2): 2,
}

class BoardState(BaseModel):
    board: List[List[int]]
    currentPlayer: int
    difficulty: int 

@app.post("/api/calculate-move")
def calculate_move(data: BoardState):
    start_time = time.time()
    board = data.board
    player = data.currentPlayer
    
    # Điều chỉnh độ sâu dựa trên độ khó
    depth = 2
    if data.difficulty == 1: depth = 1      # Easy
    elif data.difficulty == 2: depth = 2    # Normal
    elif data.difficulty == 3: depth = 3    # Hard (Đã nhanh hơn nhờ tối ưu)
    elif data.difficulty == 4: depth = 4    # Ultimate

    print(f"--- Thinking (Depth {depth}) ---")

    best_move = get_best_move(board, depth, player)
    
    duration = time.time() - start_time
    print(f"Move: {best_move} | Time: {duration:.2f}s")
    
    if best_move is None:
        return {"x": -1, "y": -1}
        
    return {"x": best_move[0], "y": best_move[1]}

# --- CORE AI LOGIC ---

def get_best_move(board, depth, player):
    # 1. Lấy danh sách ô trống xung quanh các quân đã đánh
    candidates = get_nearby_cells(board)
    
    if not candidates:
        return (7, 7)

    # 2. Sàng lọc ứng viên (Quan trọng nhất để tăng tốc)
    # Chỉ giữ lại TOP những nước đi có tiềm năng nhất để Minimax đỡ phải tính rác
    opponent = 3 - player
    
    # Check nhanh: Có nước thắng ngay hoặc phải chặn ngay không?
    for r, c in candidates:
        board[r][c] = player
        if check_win_fast(board, r, c, player): # Thắng luôn
            board[r][c] = 0
            return (r, c)
        board[r][c] = 0
        
        board[r][c] = opponent
        if check_win_fast(board, r, c, opponent): # Phải chặn gấp
            board[r][c] = 0
            return (r, c)
        board[r][c] = 0
    
    # Chấm điểm sơ bộ để sắp xếp (Move Ordering)
    # Nước đi nào tạo ra nhiều thế cờ hay sẽ được ưu tiên xét trước
    scored_candidates = []
    for r, c in candidates:
        score = quick_evaluate_move(board, r, c, player, opponent)
        scored_candidates.append((score, (r, c)))
    
    # Sắp xếp giảm dần và cắt bớt (Pruning)
    scored_candidates.sort(key=lambda x: x[0], reverse=True)
    top_candidates = [move for score, move in scored_candidates[:MAX_CANDIDATES]]
    
    # 3. Chạy Minimax trên tập ứng viên đã lọc
    best_score = -float('inf')
    best_move = top_candidates[0]
    alpha = -float('inf')
    beta = float('inf')

    for r, c in top_candidates:
        board[r][c] = player
        score = minimax(board, depth - 1, alpha, beta, False, player)
        board[r][c] = 0

        if score > best_score:
            best_score = score
            best_move = (r, c)
        
        alpha = max(alpha, best_score)
        
    return best_move

def minimax(board, depth, alpha, beta, is_maximizing, player):
    opponent = 3 - player
    
    # Điều kiện dừng
    if depth == 0:
        return evaluate_board(board, player)
    
    # Lấy ứng viên (nhưng ở level sâu thì lấy ít hơn để nhanh)
    candidates = get_nearby_cells(board, dist=1) 
    if not candidates:
        return evaluate_board(board, player)

    # Sắp xếp ứng viên sơ bộ để cắt nhánh Alpha-Beta tốt hơn
    # (Ở đây làm đơn giản là không sort lại để tiết kiệm time, chỉ dựa vào check win)
    
    if is_maximizing:
        max_eval = -float('inf')
        for r, c in candidates[:6]: # Chỉ xét 6 nước tốt nhất ở độ sâu con
            board[r][c] = player
            
            if check_win_fast(board, r, c, player):
                board[r][c] = 0
                return 1000000 + depth # Ưu tiên thắng sớm
                
            eval = minimax(board, depth - 1, alpha, beta, False, player)
            board[r][c] = 0
            max_eval = max(max_eval, eval)
            alpha = max(alpha, eval)
            if beta <= alpha:
                break
        return max_eval
    else:
        min_eval = float('inf')
        for r, c in candidates[:6]:
            board[r][c] = opponent
            
            if check_win_fast(board, r, c, opponent):
                board[r][c] = 0
                return -1000000 - depth
                
            eval = minimax(board, depth - 1, alpha, beta, True, player)
            board[r][c] = 0
            min_eval = min(min_eval, eval)
            beta = min(beta, eval)
            if beta <= alpha:
                break
        return min_eval

def get_nearby_cells(board, dist=2):
    """Tìm các ô trống xung quanh quân đã đánh"""
    rows = len(board)
    cols = len(board[0])
    candidates = set()
    
    for r in range(rows):
        for c in range(cols):
            if board[r][c] != EMPTY:
                for dr in range(-dist, dist+1):
                    for dc in range(-dist, dist+1):
                        if dr == 0 and dc == 0: continue
                        nr, nc = r + dr, c + dc
                        if 0 <= nr < rows and 0 <= nc < cols and board[nr][nc] == EMPTY:
                            candidates.add((nr, nc))
    return list(candidates)

def quick_evaluate_move(board, r, c, player, opponent):
    """Đánh giá nhanh 1 nước đi để phục vụ sắp xếp"""
    board[r][c] = player
    attack_score = evaluate_point(board, r, c, player)
    board[r][c] = opponent
    defend_score = evaluate_point(board, r, c, opponent)
    board[r][c] = 0
    return attack_score + defend_score # Công thủ toàn diện

def evaluate_point(board, r, c, player):
    """Chấm điểm cục bộ tại 1 điểm (nhanh hơn quét cả bàn)"""
    score = 0
    directions = [(0, 1), (1, 0), (1, 1), (1, -1)]
    
    for dr, dc in directions:
        length = 1
        open_ends = 0
        
        # Check hướng dương
        k = 1
        while 0 <= r + k*dr < BOARD_SIZE and 0 <= c + k*dc < BOARD_SIZE and board[r + k*dr][c + k*dc] == player:
            length += 1
            k += 1
        if 0 <= r + k*dr < BOARD_SIZE and 0 <= c + k*dc < BOARD_SIZE and board[r + k*dr][c + k*dc] == EMPTY:
            open_ends += 1
            
        # Check hướng âm
        k = 1
        while 0 <= r - k*dr < BOARD_SIZE and 0 <= c - k*dc < BOARD_SIZE and board[r - k*dr][c - k*dc] == player:
            length += 1
            k += 1
        if 0 <= r - k*dr < BOARD_SIZE and 0 <= c - k*dc < BOARD_SIZE and board[r - k*dr][c - k*dc] == EMPTY:
            open_ends += 1
            
        if length >= 5: score += 10000
        elif length == 4 and open_ends == 2: score += 5000
        elif length == 4 and open_ends == 1: score += 100
        elif length == 3 and open_ends == 2: score += 500
        elif length == 3 and open_ends == 1: score += 10
        elif length == 2 and open_ends == 2: score += 5

    return score

def evaluate_board(board, player):
    """Quét bàn cờ (Phiên bản nhẹ)"""
    score = 0
    opponent = 3 - player
    # Chỉ quét các ô có quân thay vì full bàn cờ
    for r in range(BOARD_SIZE):
        for c in range(BOARD_SIZE):
            if board[r][c] == player:
                score += evaluate_point(board, r, c, player)
            elif board[r][c] == opponent:
                score -= evaluate_point(board, r, c, opponent) * 1.2
    return score

def check_win_fast(board, r, c, player):
    """Kiểm tra thắng cực nhanh tại điểm vừa đánh"""
    directions = [(0, 1), (1, 0), (1, 1), (1, -1)]
    for dr, dc in directions:
        count = 1
        for i in range(1, 5):
            if not (0 <= r + i*dr < BOARD_SIZE and 0 <= c + i*dc < BOARD_SIZE and board[r + i*dr][c + i*dc] == player): break
            count += 1
        for i in range(1, 5):
            if not (0 <= r - i*dr < BOARD_SIZE and 0 <= c - i*dc < BOARD_SIZE and board[r - i*dr][c - i*dc] == player): break
            count += 1
        if count >= 5: return True
    return False
