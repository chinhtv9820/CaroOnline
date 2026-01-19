<template>
  <div class="space-y-2">
    <div v-if="loading" class="text-center py-4">
      <Spinner />
    </div>
    <div v-else-if="players.length === 0" class="text-center py-4 text-gray-500">
      Không có người chơi online
    </div>
    <div v-else class="space-y-2">
      <div
        v-for="player in players"
        :key="player.id"
        class="flex items-center justify-between p-2 border rounded hover:bg-gray-50"
      >
        <span>{{ player.username }}</span>
        <button
          @click="$emit('invite', player.id)"
          class="px-3 py-1 bg-blue-500 text-white text-sm rounded hover:bg-blue-600"
        >
          Mời
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
// Danh sách người chơi online trong lobby, cho phép chủ phòng gửi invite nhanh đến từng user.
import { ref, onMounted, onUnmounted } from 'vue';
import { useSignalRStore } from '@/stores/signalr';
import { hub } from '@/services/signalr';
import Spinner from './Spinner.vue';

const emit = defineEmits<{
  invite: [userId: number];
}>();

const signalRStore = useSignalRStore();
const players = ref<Array<{ id: number; username: string }>>([]);
const loading = ref(true);

onMounted(async () => {
  await signalRStore.start();
  setupSignalRHandlers();
});

onUnmounted(() => {
  cleanupSignalRHandlers();
});

function setupSignalRHandlers() {
  hub.on('LobbyUpdate', (data: any) => {
    // In a real app, you'd fetch the actual player list from the server
    // For now, we'll just show the count
    loading.value = false;
  });
}

function cleanupSignalRHandlers() {
  hub.off('LobbyUpdate');
}
</script>

