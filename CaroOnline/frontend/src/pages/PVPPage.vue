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
            @click="resign"
            class="px-4 py-2 bg-red-500 text-white rounded hover:bg-red-600"
          >
            Đầu hàng
          </button>
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div class="lg:col-span-2">
            <div class="flex justify-center mb-4">
              <Countdown
                v-if="gameStore.countdown"
                :player="gameStore.countdown.player"
                :seconds="gameStore.countdown.seconds"
              />
            </div>
            <Board
              :board="gameStore.board"
              :last-move="gameStore.lastMove"
              :disabled="!isMyTurn || gameEnded"
              @cell-click="handleCellClick"
            />
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

        <div v-if="gameEnded" class="text-center">
          <p class="text-2xl font-bold" :class="getResultClass()">
            {{ getResultMessage() }}
          </p>
          <button
            @click="resetGame"
            class="mt-4 px-6 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
          >
            Quay lại Lobby
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
// Trang PvP: quản lý luồng lobby → tạo/join phòng → vào bàn cờ, đồng bộ countdown/move/chat qua SignalR.
import { ref, computed, onMounted, onUnmounted } from 'vue';
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

const authStore = useAuthStore();
const gameStore = useGameStore();
const signalRStore = useSignalRStore();

const gameEnded = ref(false);

const isMyTurn = computed(() => {
  if (!gameStore.currentGame || !authStore.user) {
    console.log('isMyTurn: No game or user', { hasGame: !!gameStore.currentGame, hasUser: !!authStore.user });
    return false;
  }
  
  const myPlayer = gameStore.currentGame.p1UserId === authStore.user.id ? 1 : 2;
  const isTurn = gameStore.currentGame.currentTurn === myPlayer;
  
  console.log('isMyTurn check:', {
    myUserId: authStore.user.id,
    p1UserId: gameStore.currentGame.p1UserId,
    p2UserId: gameStore.currentGame.p2UserId,
    myPlayer,
    currentTurn: gameStore.currentGame.currentTurn,
    isTurn
  });
  
  return isTurn;
});

onMounted(async () => {
  try {
    // Fetch user nếu có token nhưng chưa có user
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
    console.log('GameStarted event received:', data);
    
    // Fetch full game data from API
    try {
      const response = await api.get(`/api/games/${data.gameId}`);
      const gameData = response.data;
      console.log('Fetched game data:', gameData);
      
      // Set game in store
      gameStore.setGame({
        id: gameData.id,
        mode: gameData.mode,
        p1UserId: gameData.p1UserId,
        p2UserId: gameData.p2UserId,
        currentTurn: gameData.currentTurn || 1, // Default to 1 if not set
        finishedAt: gameData.finishedAt,
        result: gameData.result,
        timeControlSeconds: gameData.timeControlSeconds
      });
      
      console.log('Game set in store:', {
        id: gameData.id,
        p1UserId: gameData.p1UserId,
        p2UserId: gameData.p2UserId,
        currentTurn: gameData.currentTurn || 1,
        myUserId: authStore.user?.id
      });
      
      // Join game group
      await hub.invoke('JoinGame', data.gameId);
      
      // Load moves if any
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
    
    // Update currentTurn from server response
    if (gameStore.currentGame && data.currentTurn !== undefined) {
      gameStore.currentGame.currentTurn = data.currentTurn;
      console.log(`Move made by player ${data.player}, currentTurn updated to ${data.currentTurn}`);
    } else if (gameStore.currentGame) {
      // Fallback: switch turn manually if server didn't send currentTurn
      const newTurn = data.player === 1 ? 2 : 1;
      gameStore.currentGame.currentTurn = newTurn;
      console.log(`Move made by player ${data.player}, switching to turn ${newTurn} (fallback)`);
    }
  });

  hub.on('UpdateTimer', (player: number, seconds: number) => {
    gameStore.setCountdown(player, seconds);
  });

  hub.on('GameEnded', (data: any) => {
    gameEnded.value = true;
  });

  hub.on('ChatMessage', (data: any) => {
    gameStore.addMessage(data);
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

function resetGame() {
  gameStore.reset();
  gameEnded.value = false;
}

function getResultClass(): string {
  if (!gameStore.currentGame?.result) return '';
  if (gameStore.currentGame.result.includes('P1_WIN') && gameStore.currentGame.p1UserId === authStore.user?.id) {
    return 'text-green-600';
  }
  if (gameStore.currentGame.result.includes('P2_WIN') && gameStore.currentGame.p2UserId === authStore.user?.id) {
    return 'text-green-600';
  }
  return 'text-red-600';
}

function getResultMessage(): string {
  if (!gameStore.currentGame?.result) return '';
  if (gameStore.currentGame.result.includes('WIN') && 
      ((gameStore.currentGame.result.includes('P1') && gameStore.currentGame.p1UserId === authStore.user?.id) ||
       (gameStore.currentGame.result.includes('P2') && gameStore.currentGame.p2UserId === authStore.user?.id))) {
    return 'Bạn thắng!';
  }
  return 'Bạn thua!';
}
</script>

