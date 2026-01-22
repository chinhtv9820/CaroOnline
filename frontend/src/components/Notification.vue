<template>
  <transition name="slide-down">
    <div
      v-if="visible"
      :class="[
        'fixed top-4 right-4 z-50 max-w-sm w-full bg-white rounded-lg shadow-lg p-4',
        type === 'success' ? 'border-l-4 border-green-500' : '',
        type === 'error' ? 'border-l-4 border-red-500' : '',
        type === 'info' ? 'border-l-4 border-blue-500' : '',
        type === 'warning' ? 'border-l-4 border-yellow-500' : ''
      ]"
    >
      <div class="flex items-start">
        <div class="flex-shrink-0">
          <!-- Success Icon -->
          <svg
            v-if="type === 'success'"
            class="h-6 w-6 text-green-500"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
          <!-- Error Icon -->
          <svg
            v-else-if="type === 'error'"
            class="h-6 w-6 text-red-500"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
          <!-- Info Icon -->
          <svg
            v-else-if="type === 'info'"
            class="h-6 w-6 text-blue-500"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
          <!-- Warning Icon -->
          <svg
            v-else
            class="h-6 w-6 text-yellow-500"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
            />
          </svg>
        </div>
        <div class="ml-3 flex-1">
          <p class="text-sm font-medium text-gray-900">{{ title }}</p>
          <p v-if="message" class="mt-1 text-sm text-gray-500">{{ message }}</p>
        </div>
        <div class="ml-4 flex-shrink-0">
          <button
            @click="close"
            class="inline-flex text-gray-400 hover:text-gray-500 focus:outline-none"
          >
            <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </button>
        </div>
      </div>
    </div>
  </transition>
</template>

<script setup lang="ts">
// Notification toast: nhận type/title/message, tự động biến mất sau duration hoặc khi user đóng.
import { ref, onMounted, onUnmounted } from 'vue';

const props = defineProps<{
  type?: 'success' | 'error' | 'info' | 'warning';
  title: string;
  message?: string;
  duration?: number;
}>();

const emit = defineEmits<{
  close: [];
}>();

const visible = ref(true);
let timeoutId: number | null = null;

function close() {
  visible.value = false;
  setTimeout(() => {
    emit('close');
  }, 300);
}

onMounted(() => {
  if (props.duration && props.duration > 0) {
    timeoutId = window.setTimeout(() => {
      close();
    }, props.duration);
  }
});

onUnmounted(() => {
  if (timeoutId) {
    clearTimeout(timeoutId);
  }
});
</script>

<style scoped>
.slide-down-enter-active,
.slide-down-leave-active {
  transition: all 0.3s ease;
}

.slide-down-enter-from {
  transform: translateY(-100%);
  opacity: 0;
}

.slide-down-leave-to {
  transform: translateY(-100%);
  opacity: 0;
}
</style>

