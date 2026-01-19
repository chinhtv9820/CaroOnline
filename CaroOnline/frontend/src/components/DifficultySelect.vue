<template>
  <div class="space-y-4">
    <h3 class="text-lg font-semibold">Chọn độ khó:</h3>
    <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
      <button
        v-for="difficulty in difficulties"
        :key="difficulty.value"
        @click="selectDifficulty(difficulty.value)"
        :class="[
          'px-6 py-4 rounded-lg border-2 transition-all',
          selected === difficulty.value
            ? difficulty.value === 4
              ? 'border-red-500 bg-red-50 ring-2 ring-red-200'
              : 'border-blue-500 bg-blue-50 ring-2 ring-blue-200'
            : difficulty.value === 4
              ? 'border-red-300 hover:border-red-400 hover:bg-red-50'
              : 'border-gray-300 hover:border-blue-300 hover:bg-gray-50'
        ]"
      >
        <div class="font-semibold" :class="difficulty.value === 4 ? 'text-red-600' : ''">
          {{ difficulty.label }}
        </div>
        <div class="text-sm text-gray-600 mt-1">{{ difficulty.description }}</div>
        <div v-if="difficulty.value === 4" class="text-xs text-red-500 font-bold mt-1">
          ⚠️ Không thể đánh bại
        </div>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';

const emit = defineEmits<{
  select: [difficulty: number];
}>();

const selected = ref<number | null>(null);

const difficulties = [
  { value: 1, label: 'Easy', description: 'IQ 150 - Ngẫu nhiên + Block' },
  { value: 2, label: 'Medium', description: 'IQ 200 - Heuristic' },
  { value: 3, label: 'Hard', description: 'IQ 400 - Minimax AI' },
  { value: 4, label: 'ULTIMATE', description: 'IQ 9000 - Unbeatable AI' }
];

function selectDifficulty(value: number) {
  selected.value = value;
  emit('select', value);
}
</script>

