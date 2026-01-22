<template>
  <div class="flex items-center space-x-2">
    <div class="text-lg font-semibold">
      <span v-if="player === 1" class="text-blue-600">Người chơi 1</span>
      <span v-else class="text-red-600">Người chơi 2</span>
    </div>
    <div
      :class="[
        'px-4 py-2 rounded-lg font-mono text-xl',
        seconds <= 10 ? 'bg-red-500 text-white animate-pulse' : 'bg-gray-200 text-gray-800'
      ]"
    >
      {{ formatTime(seconds) }}
    </div>
  </div>
</template>

<script setup lang="ts">
// Countdown hiển thị thời gian còn lại của từng người chơi; chuyển màu đỏ và pulse khi <=10s.
defineProps<{
  player: number;
  seconds: number;
}>();

function formatTime(seconds: number): string {
  const mins = Math.floor(seconds / 60);
  const secs = seconds % 60;
  return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
}
</script>

