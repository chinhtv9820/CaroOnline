import { defineStore } from 'pinia';
import { ref, computed } from 'vue';

export interface Move {
  player: number;
  x: number;
  y: number;
  moveNumber: number;
}

export interface Game {
  id: number;
  mode: string;
  p1UserId?: number;
  p2UserId?: number;
  currentTurn: number;
  finishedAt?: string;
  result?: string;
  timeControlSeconds: number;
}

export interface ChatMessage {
  gameId: number;
  senderId: number;
  content: string;
  timestamp: string;
}

export const useGameStore = defineStore('game', () => {
  // Store trung tâm của trận đấu: lưu trạng thái bàn cờ, moves, chat, countdown và currentGame cho cả PvE/PvP.
  const currentGame = ref<Game | null>(null);
  const board = ref<number[][]>([]);
  const moves = ref<Move[]>([]);
  const messages = ref<ChatMessage[]>([]);
  const countdown = ref<{ player: number; seconds: number } | null>(null);
  const lastMove = ref<{ x: number; y: number; player: number } | null>(null);

  const BOARD_SIZE = 15;

  function initBoard() {
    board.value = Array(BOARD_SIZE).fill(null).map(() => Array(BOARD_SIZE).fill(0));
  }

  function setGame(game: Game) {
    currentGame.value = game;
    initBoard();
    moves.value = [];
    messages.value = [];
    lastMove.value = null;
  }

  function addMove(move: Move) {
    moves.value.push(move);
    board.value[move.x][move.y] = move.player;
    lastMove.value = { x: move.x, y: move.y, player: move.player };
  }

  function addMessage(message: ChatMessage) {
    messages.value.push(message);
  }

  function setCountdown(player: number, seconds: number) {
    countdown.value = { player, seconds };
  }

  function clearCountdown() {
    countdown.value = null;
  }

  function reset() {
    currentGame.value = null;
    initBoard();
    moves.value = [];
    messages.value = [];
    countdown.value = null;
    lastMove.value = null;
  }

  return {
    currentGame,
    board,
    moves,
    messages,
    countdown,
    lastMove,
    BOARD_SIZE,
    initBoard,
    setGame,
    addMove,
    addMessage,
    setCountdown,
    clearCountdown,
    reset
  };
});

