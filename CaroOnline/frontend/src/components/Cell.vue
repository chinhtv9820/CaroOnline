<template>
  <div
    :class="[
      'w-8 h-8 sm:w-10 sm:h-10 border border-gray-400 flex items-center justify-center cursor-pointer transition-all duration-200',
      cellClass,
      isLastMove ? 'ring-2 ring-yellow-400 ring-offset-2' : '',
      isHighlighted ? 'bg-yellow-200 animate-pulse' : '',
      hasValue ? 'animate-scale-in' : ''
    ]"
    @click="handleClick"
  >
    <transition name="fade-scale" mode="out-in">
      <span 
        v-if="value === 1" 
        key="x"
        class="text-2xl sm:text-3xl font-bold text-blue-600"
      >
        X
      </span>
      <span 
        v-else-if="value === 2" 
        key="o"
        class="text-2xl sm:text-3xl font-bold text-red-600"
      >
        O
      </span>
    </transition>
  </div>
</template>

<style scoped>
.animate-scale-in {
  animation: scaleIn 0.3s ease-out;
}

@keyframes scaleIn {
  from {
    transform: scale(0);
    opacity: 0;
  }
  to {
    transform: scale(1);
    opacity: 1;
  }
}

.fade-scale-enter-active,
.fade-scale-leave-active {
  transition: all 0.2s ease;
}

.fade-scale-enter-from,
.fade-scale-leave-to {
  transform: scale(0);
  opacity: 0;
}
</style>

<script setup lang="ts">
// Ô cờ đơn lẻ: hiển thị X/O, highlight nước đi cuối hoặc ô gợi ý, ngăn click khi đã có giá trị.
import { computed } from 'vue';

const props = defineProps<{
  value: number;
  x: number;
  y: number;
  isLastMove?: boolean;
  isHighlighted?: boolean;
  disabled?: boolean;
}>();

const emit = defineEmits<{
  click: [x: number, y: number];
}>();

const cellClass = computed(() => {
  if (props.disabled) return 'bg-gray-200 cursor-not-allowed';
  if (props.value !== 0) return 'bg-white cursor-default';
  return 'bg-gray-50 hover:bg-gray-100 hover:shadow-md';
});

const hasValue = computed(() => props.value !== 0);

function handleClick() {
  if (!props.disabled && props.value === 0) {
    emit('click', props.x, props.y);
  }
}
</script>

