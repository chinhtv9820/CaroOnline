<template>
  <div class="max-w-6xl mx-auto">
    <h1 class="text-3xl font-bold mb-6">Chơi với người</h1>

    <LoginPrompt v-if="!authStore.isAuthenticated" />

    <div v-else>
      <Lobby
        v-if="!gameStore.currentGame"
        @game-started="handleGameStarted"
      />

      <div v-else class="space-y-6">
        <div class="flex justify-between items-center">
          <div>
            <h2 class="text-xl font-semibold">Game ID: {{ gameStore.currentGame.id }}</h2>
            <p class="text-gray-600">Mode: {{ gameStore.currentGame.mode }}</p>
          </div>
          <button
            v-if="!gameEnded"
            @click="resign"
            class="px-4 py-2 bg-red-500 text-white rounded hover:bg-red-600"
          >
            Đầu hàng
          </button>
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div class="lg:col-span-2 relative">
            <div class="flex justify-center mb-4">
              <Countdown
                v-if="gameStore.countdown"
                :player="gameStore.countdown.player"
                :seconds="gameStore.countdown.seconds"
              />
            </div>
            
            <div class="relative">
              <Board
                :board="gameStore.board"
                :last-move="gameStore.lastMove"
                :disabled="!isMyTurn || gameEnded"
                @cell-click="handleCellClick"
              />

              <div 
                v-if="gameEnded" 
                class="absolute inset-0 z-10 flex flex-col items-center justify-center bg-black/70 rounded-lg text-white animate-fade-in"
              >
                <div class="text-3xl font-bold mb-2 p-4 text-center">
                  {{ finalResultText }}
                </div>
                
                <div class="flex space-x-4 mt-4">
                  <button
                    @click="goToMenu"
                    class="px-6 py-2 bg-gray-500 hover:bg-gray-600 rounded text-white font-semibold transition"
                  >
                    Về Menu
                  </button>
                  <button
                    @click="resetGame"
                    class="px-6 py-2 bg-blue-600 hover:bg-blue-700 rounded text-white font-semibold transition"
                  >
                    Chơi lại
                  </button>
                </div>
              </div>
            </div>
          </div>

          <div class="space-y-4">
            <Chatbox
              :messages="gameStore.messages"
              :current-user-id="authStore.user?.id"
              :disabled="gameEnded"
              @send="handleSendMessage"
            />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router'; // Import router
import { useAuthStore } from '@/stores/auth';
import { useGameStore } from '@/stores/game';
import { useSignalRStore } from '@/stores/signalr';
import { hub } from '@/services/signalr';
import { api } from '@/services/api';
import Board from '@/components/Board.vue';
import Countdown from '@/components/Countdown.vue';
import Chatbox from '@/components/Chatbox.vue';
import LoginPrompt from '@/components/LoginPrompt.vue';
import Lobby from '@/components/Lobby.vue';

const router = useRouter();
const authStore = useAuthStore();
const gameStore = useGameStore();
const signalRStore = useSignalRStore();

const gameEnded = ref(false);

// Tên hiển thị của người chơi hiện tại (Fallback về email hoặc ID nếu không có username)
const myDisplayName = computed(() => {
  return authStore.user?.username || authStore.user?.email || `User ${authStore.user?.id}`;
});

const isMyTurn = computed(() => {
  if (!gameStore.currentGame || !authStore.user) {
    return false;
  }
  const myPlayer = gameStore.currentGame.p1UserId === authStore.user.id ? 1 : 2;
  return gameStore.currentGame.currentTurn === myPlayer;
});

// Logic hiển thị kết quả (Yêu cầu 2)
const finalResultText = computed(() => {
  if (!gameStore.currentGame?.result) return '';
  
  const result = gameStore.currentGame.result;
  if (result === 'DRAW' || result.includes('DRAW')) return 'Hòa!';

  // Xác định ai thắng
  const p1Wins = result.includes('P1_WIN');
  const amIP1 = gameStore.currentGame.p1UserId === authStore.user?.id;
  
  // Nếu tôi là P1 và P1 thắng => Tôi thắng
  // Nếu tôi là P2 (tức !amIP1) và P2 thắng (!p1Wins) => Tôi thắng
  const iWon = (p1Wins && amIP1) || (!p1Wins && !amIP1);

  if (iWon) {
    return `Người thắng: ${myDisplayName.value}`;
  } else {
    return `Người thua: ${myDisplayName.value}`;
  }
});

onMounted(async () => {
  try {
    if (authStore.isAuthenticated && !authStore.user) {
      await authStore.fetchUser();
    }
    await signalRStore.start();
    setupSignalRHandlers();
  } catch (error) {
    console.error('Failed to setup PVP page:', error);
  }
});

onUnmounted(() => {
  cleanupSignalRHandlers();
});

function setupSignalRHandlers() {
  hub.on('GameStarted', async (data: any) => {
    try {
      const response = await api.get(`/api/games/${data.gameId}`);
      const gameData = response.data;
      
      gameStore.setGame({
        id: gameData.id,
        mode: gameData.mode,
        p1UserId: gameData.p1UserId,
        p2UserId: gameData.p2UserId,
        currentTurn: gameData.currentTurn || 1,
        finishedAt: gameData.finishedAt,
        result: gameData.result,
        timeControlSeconds: gameData.timeControlSeconds
      });
      
      await hub.invoke('JoinGame', data.gameId);
      
      if (gameData.moves && gameData.moves.length > 0) {
        gameData.moves.forEach((move: any) => {
          gameStore.addMove({
            player: move.player,
            x: move.x,
            y: move.y,
            moveNumber: move.moveNumber
          });
        });
      }
    } catch (error) {
      console.error('Failed to fetch game data:', error);
    }
  });

  hub.on('MoveMade', async (data: any) => {
    gameStore.addMove({
      player: data.player,
      x: data.x,
      y: data.y,
      moveNumber: data.moveNumber
    });
    
    if (gameStore.currentGame) {
      if (data.currentTurn !== undefined) {
        gameStore.currentGame.currentTurn = data.currentTurn;
      } else {
        gameStore.currentGame.currentTurn = data.player === 1 ? 2 : 1;
      }
    }
  });

  hub.on('UpdateTimer', (player: number, seconds: number) => {
    gameStore.setCountdown(player, seconds);
  });

  hub.on('GameEnded', (data: any) => {
    // Cập nhật kết quả vào store để computed finalResultText hoạt động
    if (gameStore.currentGame) {
        gameStore.currentGame.result = data.result;
    }
    gameEnded.value = true;
  });

  hub.on('ChatMessage', (data: any) => {
    // Yêu cầu 1: Fix lỗi lặp tin nhắn
    // Kiểm tra xem tin nhắn đã tồn tại trong store chưa trước khi thêm
    const exists = gameStore.messages.some(m => 
      m.senderId === data.senderId && 
      m.timestamp === data.timestamp && 
      m.content === data.content
    );

    if (!exists) {
      gameStore.addMessage(data);
    }
  });

  hub.on('PlayerKicked', (data: any) => {
    if (data.kickedUserId === authStore.user?.id) {
      alert('Bạn đã bị kick khỏi phòng!');
      resetGame();
    }
  });
}

function cleanupSignalRHandlers() {
  hub.off('GameStarted');
  hub.off('MoveMade');
  hub.off('UpdateTimer');
  hub.off('GameEnded');
  hub.off('ChatMessage');
  hub.off('PlayerKicked');
}

function handleGameStarted() {
  gameEnded.value = false;
}

async function handleCellClick(x: number, y: number) {
  if (!isMyTurn.value || gameEnded.value || !gameStore.currentGame) return;
  try {
    await hub.invoke('MakeMove', gameStore.currentGame.id, x, y);
  } catch (error) {
    console.error('Failed to make move:', error);
  }
}

async function handleSendMessage(message: string) {
  if (!gameStore.currentGame) return;
  try {
    await hub.invoke('SendChatMessage', gameStore.currentGame.id, message);
  } catch (error) {
    console.error('Failed to send message:', error);
  }
}

async function resign() {
  if (!gameStore.currentGame) return;
  try {
    await hub.invoke('Resign', gameStore.currentGame.id);
  } catch (error) {
    console.error('Failed to resign:', error);
  }
}

// Logic cho nút "Chơi lại" (Yêu cầu 3)
function resetGame() {
  gameStore.reset();
  gameEnded.value = false;
  // Khi reset store, view sẽ tự động chuyển về Lobby do v-if="!gameStore.currentGame"
}

// Logic cho nút "Về Menu" (Yêu cầu 3)
function goToMenu() {
    // Giả sử đường dẫn menu là '/menu', bạn có thể chỉnh lại thành '/' nếu cần
    router.push('/menu'); 
}
</script>

<style scoped>
/* Hiệu ứng hiện dần cho overlay */
.animate-fade-in {
  animation: fadeIn 0.3s ease-out;
}

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}
</style>
