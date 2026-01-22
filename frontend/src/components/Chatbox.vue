<template>
  <div class="flex flex-col h-64 border border-gray-300 rounded-lg bg-white">
    <div class="px-4 py-2 bg-gray-100 border-b font-semibold">Chat</div>
    <div ref="messagesContainer" class="flex-1 overflow-y-auto p-4 space-y-2">
      <div
        v-for="(msg, index) in messages"
        :key="index"
        :class="[
          'p-2 rounded',
          msg.senderId === currentUserId ? 'bg-blue-100 ml-auto text-right' : 'bg-gray-100'
        ]"
      >
        <div class="text-xs text-gray-500">{{ formatTime(msg.timestamp) }}</div>
        <div>{{ msg.content }}</div>
      </div>
    </div>
    <div class="border-t p-2 flex space-x-2">
      <input
        v-model="message"
        @keyup.enter="sendMessage"
        type="text"
        placeholder="Nhập tin nhắn..."
        class="flex-1 px-3 py-2 border rounded focus:outline-none focus:ring-2 focus:ring-blue-500"
      />
      <button
        @click="sendMessage"
        :disabled="!message.trim()"
        class="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600 disabled:bg-gray-300 disabled:cursor-not-allowed"
      >
        Gửi
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
// Chatbox trong phòng PvP: hiển thị lịch sử tin nhắn, tự động scroll xuống cuối và emit sự kiện 'send'.
import { ref, watch, nextTick } from 'vue';
import type { ChatMessage } from '@/stores/game';

const props = defineProps<{
  messages: ChatMessage[];
  currentUserId?: number;
  disabled?: boolean;
}>();

const emit = defineEmits<{
  send: [message: string];
}>();

const message = ref('');
const messagesContainer = ref<HTMLElement>();

watch(() => props.messages.length, () => {
  nextTick(() => {
    if (messagesContainer.value) {
      messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight;
    }
  });
});

function sendMessage() {
  if (message.value.trim() && !props.disabled) {
    emit('send', message.value.trim());
    message.value = '';
  }
}

function formatTime(timestamp: string): string {
  const date = new Date(timestamp);
  return date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
}
</script>

