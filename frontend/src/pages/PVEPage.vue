<template>
  <div class="max-w-4xl mx-auto">
    <h1 class="text-3xl font-bold mb-6">Chơi với AI</h1>

    <div v-if="!gameStarted" class="space-y-6">
      <DifficultySelect @select="handleDifficultySelect" />
      <div class="flex justify-center">
        <button
          @click="startGame"
          :disabled="!selectedDifficulty"
          class="px-8 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed"
        >
          Bắt đầu
        </button>
      </div>
    </div>

    <div v-else class="space-y-6">
      <div class="flex justify-between items-center">
        <div>
          <h2 class="text-xl font-semibold">Game ID: {{ gameStore.currentGame?.id }}</h2>
          <p class="text-gray-600">Độ khó: {{ getDifficultyName(selectedDifficulty) }}</p>
        </div>
        <button
          @click="resign"
          class="px-4 py-2 bg-red-500 text-white rounded hover:bg-red-600"
        >
          Đầu hàng
        </button>
      </div>

      <div class="flex justify-center">
        <Countdown
          v-if="gameStore.countdown"
          :player="gameStore.countdown.player"
          :seconds="gameStore.countdown.seconds"
        />
      </div>

      <!-- AI Thinking Indicator -->
      <div v-if="isAITurn" class="text-center mb-4">
        <div class="inline-flex items-center space-x-2 text-gray-600">
          <svg class="animate-spin h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
          </svg>
          <span>AI đang suy nghĩ...</span>
        </div>
      </div>

      <Board
        :board="gameStore.board"
        :last-move="gameStore.lastMove"
        :highlighted-cell="highlightedCell"
        :disabled="isAITurn || gameEnded"
        @cell-click="handleCellClick"
      />

      <div v-if="gameEnded" class="text-center">
        <p class="text-2xl font-bold" :class="gameResult === 'win' ? 'text-green-600' : 'text-red-600'">
          {{ gameResult === 'win' ? 'Bạn thắng!' : 'Bạn thua!' }}
        </p>
        <button
          @click="resetGame"
          class="mt-4 px-6 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
        >
          Chơi lại
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
// Trang PvE: cho phép chọn độ khó, bắt đầu trận với AI, xử lý countdown và logic đánh theo lượt người vs AI.
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useGameStore } from '@/stores/game';
import { api } from '@/services/api';
import Board from '@/components/Board.vue';
import Countdown from '@/components/Countdown.vue';
import DifficultySelect from '@/components/DifficultySelect.vue';

const gameStore = useGameStore();
const selectedDifficulty = ref<number | null>(null);
const gameStarted = ref(false);
const gameEnded = ref(false);
const gameResult = ref<'win' | 'lose' | null>(null);
const isAITurn = ref(false);
const highlightedCell = ref<{ x: number; y: number } | null>(null);

function handleDifficultySelect(difficulty: number) {
  selectedDifficulty.value = difficulty;
}

async function startGame() {
  if (!selectedDifficulty.value) return;
  
  try {
    const response = await api.post('/api/games', {
      mode: 'PvE',
      p1UserId: null,
      p2UserId: null,
      pveDifficulty: selectedDifficulty.value,
      timeControlSeconds: 300
    });
    
    gameStore.setGame(response.data);
    gameStore.initBoard();
    gameStarted.value = true;
    gameEnded.value = false;
    gameResult.value = null;
  } catch (error: any) {
    console.error('Failed to start game:', error);
    alert(`Không thể tạo game: ${error.response?.data?.message || error.message || 'Backend chưa chạy. Vui lòng start backend server.'}`);
  }
}

async function handleCellClick(x: number, y: number) {
  if (isAITurn.value || gameEnded.value || !gameStore.currentGame) return;
  
  try {
    const response = await api.post(`/api/games/${gameStore.currentGame.id}/moves`, {
      x,
      y
    });
    
    gameStore.addMove({
      player: 1,
      x,
      y,
      moveNumber: response.data.moveNumber
    });
    
    // Check if game ended
    if (response.data.gameFinished) {
      gameEnded.value = true;
      gameResult.value = response.data.winner === 1 ? 'win' : 'lose';
    } else {
      // AI turn
      isAITurn.value = true;
      await makeAIMove();
    }
  } catch (error) {
    console.error('Failed to make move:', error);
  }
}

async function makeAIMove() {
  if (!gameStore.currentGame) return;
  
  try {
    // Wait a bit for visual effect
    await new Promise(resolve => setTimeout(resolve, 500));
    
    const response = await api.post(`/api/games/${gameStore.currentGame.id}/moves`, {
      x: -1, // Signal for AI move
      y: -1
    });
    
    if (response.data.move) {
      const move = response.data.move;
      gameStore.addMove(move);
      highlightedCell.value = { x: move.x, y: move.y };
      
      setTimeout(() => {
        highlightedCell.value = null;
      }, 1000);
      
      if (response.data.gameFinished) {
        gameEnded.value = true;
        gameResult.value = response.data.winner === 1 ? 'win' : 'lose';
      } else {
        isAITurn.value = false;
      }
    }
  } catch (error) {
    console.error('Failed to make AI move:', error);
    isAITurn.value = false;
  }
}

async function resign() {
  if (!gameStore.currentGame) return;
  
  try {
    await api.post(`/api/games/${gameStore.currentGame.id}/resign`);
    gameEnded.value = true;
    gameResult.value = 'lose';
  } catch (error) {
    console.error('Failed to resign:', error);
  }
}

function resetGame() {
  gameStore.reset();
  gameStarted.value = false;
  gameEnded.value = false;
  gameResult.value = null;
  selectedDifficulty.value = null;
  isAITurn.value = false;
  highlightedCell.value = null;
}

function getDifficultyName(difficulty: number | null): string {
  const names: Record<number, string> = {
    1: 'Easy (IQ 150)',
    2: 'Medium (IQ 200)',
    3: 'Hard (IQ 400)',
    4: 'ULTIMATE (IQ 9000)'
  };
  return names[difficulty || 0] || 'Unknown';
}

onMounted(() => {
  gameStore.initBoard();
});

onUnmounted(() => {
  resetGame();
});
</script>

