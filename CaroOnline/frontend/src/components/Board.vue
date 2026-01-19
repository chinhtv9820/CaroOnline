<template>
  <div class="flex flex-col items-center space-y-4">
    <div class="grid gap-0 border-2 border-gray-600 bg-gray-300 p-2" :style="gridStyle">
      <template v-for="(row, x) in board" :key="`row-${x}`">
        <Cell
          v-for="(cell, y) in row"
          :key="`cell-${x}-${y}`"
          :value="cell"
          :x="x"
          :y="y"
          :is-last-move="isLastMove(x, y)"
          :is-highlighted="isHighlighted(x, y)"
          :disabled="disabled || cell !== 0"
          @click="handleCellClick"
        />
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
// Component bảng cờ 15x15: render grid Cell.vue, highlight nước đi cuối và ô gợi ý,
// emit sự kiện 'cellClick' khi người chơi chọn ô còn trống.
import { computed } from 'vue';
import Cell from './Cell.vue';

const props = defineProps<{
  board: number[][];
  lastMove?: { x: number; y: number; player: number } | null;
  highlightedCell?: { x: number; y: number } | null;
  disabled?: boolean;
}>();

const emit = defineEmits<{
  cellClick: [x: number, y: number];
}>();

const gridStyle = computed(() => ({
  gridTemplateColumns: `repeat(${props.board.length}, minmax(0, 1fr))`,
  display: 'grid'
}));

function isLastMove(x: number, y: number): boolean {
  return props.lastMove?.x === x && props.lastMove?.y === y;
}

function isHighlighted(x: number, y: number): boolean {
  return props.highlightedCell?.x === x && props.highlightedCell?.y === y;
}

function handleCellClick(x: number, y: number) {
  emit('cellClick', x, y);
}
</script>

