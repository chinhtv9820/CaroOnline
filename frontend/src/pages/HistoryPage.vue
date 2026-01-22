<template>
  <div class="max-w-6xl mx-auto">
    <h1 class="text-3xl font-bold mb-6">Lịch sử trận đấu</h1>

    <!-- Filter by mode -->
    <div class="mb-6 flex space-x-2">
      <button
        v-for="filter in filters"
        :key="filter.value"
        @click="selectedFilter = filter.value"
        :class="[
          'px-4 py-2 rounded-lg font-medium transition',
          selectedFilter === filter.value
            ? 'bg-blue-600 text-white'
            : 'bg-white text-gray-700 hover:bg-gray-100'
        ]"
      >
        {{ filter.label }}
      </button>
    </div>

    <div v-if="loading" class="text-center py-12">
      <Spinner />
      <p class="mt-4 text-gray-600">Đang tải lịch sử...</p>
    </div>

    <div v-else-if="filteredGames.length === 0" class="text-center py-12">
      <div class="text-gray-400 mb-4">
        <svg class="w-16 h-16 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      </div>
      <p class="text-gray-500 text-lg">Chưa có trận đấu nào</p>
    </div>

    <div v-else class="space-y-4">
      <div
        v-for="game in filteredGames"
        :key="game.id"
        class="bg-white rounded-lg shadow-md p-6 hover:shadow-xl transition-all duration-200"
      >
        <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
          <div class="flex-1">
            <div class="flex items-center space-x-3 mb-2">
              <h3 class="text-xl font-semibold text-gray-900">Game #{{ game.id }}</h3>
              <span
                :class="[
                  'px-2 py-1 rounded text-xs font-medium',
                  game.mode === 'PvP' ? 'bg-blue-100 text-blue-800' : 'bg-purple-100 text-purple-800'
                ]"
              >
                {{ game.mode }}
              </span>
            </div>
            <div class="space-y-1 text-sm text-gray-600">
              <p>
                <span class="font-medium">Bắt đầu:</span>
                {{ formatDate(game.createdAt) }}
              </p>
              <p v-if="game.finishedAt">
                <span class="font-medium">Kết thúc:</span>
                {{ formatDate(game.finishedAt) }}
              </p>
              <p v-if="game.result">
                <span class="font-medium">Kết quả:</span>
                <span :class="getResultClass(game.result)">
                  {{ getResultText(game.result) }}
                </span>
              </p>
            </div>
          </div>
          <div class="flex flex-col items-end space-y-2">
            <span
              :class="[
                'px-4 py-2 rounded-lg text-sm font-semibold',
                getStatusClass(game)
              ]"
            >
              {{ game.finishedAt ? 'Hoàn thành' : 'Đang chơi' }}
            </span>
            <button
              @click="viewGame(game.id)"
              class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition text-sm font-medium"
            >
              Xem chi tiết
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Pagination (if needed) -->
    <div v-if="filteredGames.length > 0" class="mt-6 flex justify-center">
      <p class="text-sm text-gray-500">
        Hiển thị {{ filteredGames.length }} / {{ games.length }} trận đấu
      </p>
    </div>
  </div>
</template>

<script setup lang="ts">
// Trang lịch sử trận đấu: load danh sách game của user, filter theo mode và hiển thị trạng thái/ kết quả.
import { ref, computed, onMounted } from 'vue';
import { useAuthStore } from '@/stores/auth';
import { api } from '@/services/api';
import Spinner from '@/components/Spinner.vue';

interface Game {
  id: number;
  mode: string;
  createdAt: string;
  finishedAt?: string;
  result?: string;
}

const authStore = useAuthStore();
const games = ref<Game[]>([]);
const loading = ref(false);
const selectedFilter = ref<'all' | 'PvP' | 'PvE'>('all');

const filters = [
  { value: 'all' as const, label: 'Tất cả' },
  { value: 'PvP' as const, label: 'PVP' },
  { value: 'PvE' as const, label: 'PVE' }
];

const filteredGames = computed(() => {
  if (selectedFilter.value === 'all') {
    return games.value;
  }
  return games.value.filter(g => g.mode === selectedFilter.value);
});

async function fetchGames() {
  if (!authStore.isAuthenticated) {
    games.value = [];
    return;
  }
  
  loading.value = true;
  try {
    const response = await api.get('/api/games');
    // Sort by created date (newest first)
    games.value = (response.data || []).sort((a: Game, b: Game) => 
      new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    );
  } catch (error) {
    console.error('Failed to fetch games:', error);
    games.value = [];
  } finally {
    loading.value = false;
  }
}

function formatDate(dateString: string): string {
  if (!dateString) return 'N/A';
  const date = new Date(dateString);
  return date.toLocaleString('vi-VN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  });
}

function getResultClass(result: string): string {
  if (result?.includes('WIN')) {
    return 'text-green-600 font-semibold';
  }
  return 'text-gray-600';
}

function getResultText(result: string): string {
  if (!result) return 'Chưa kết thúc';
  if (result.includes('P1_WIN')) return 'Người chơi 1 thắng';
  if (result.includes('P2_WIN')) return 'Người chơi 2 thắng';
  if (result.includes('RESIGN')) return 'Đầu hàng';
  return result;
}

function getStatusClass(game: Game): string {
  if (game.finishedAt) {
    return 'bg-green-100 text-green-800';
  }
  return 'bg-yellow-100 text-yellow-800';
}

function viewGame(gameId: number) {
  // TODO: Navigate to game detail page or show modal with game replay
  console.log('View game details:', gameId);
  alert(`Xem chi tiết game #${gameId}\n(Tính năng này sẽ được implement sau)`);
}

onMounted(() => {
  if (authStore.isAuthenticated) {
    fetchGames();
  }
});
</script>

