<template>
  <div class="bg-white rounded-lg shadow-lg p-6">
    <!-- Room Header -->
    <div class="flex items-center justify-between mb-6">
      <div>
        <h3 class="text-2xl font-bold text-gray-900">Phòng #{{ roomId }}</h3>
        <p class="text-sm text-gray-600 mt-1">
          <span class="inline-flex items-center">
            <span class="w-2 h-2 bg-green-500 rounded-full mr-2 animate-pulse"></span>
            {{ onlineCount }} người online
          </span>
        </p>
      </div>
      <button
        v-if="isOwner"
        @click="$emit('leave-room')"
        class="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition"
      >
        Rời phòng
      </button>
    </div>

    <!-- Room Info -->
    <div class="mb-6 p-4 bg-gray-50 rounded-lg">
      <div class="grid grid-cols-2 gap-4 text-sm">
        <div>
          <span class="text-gray-600">Chủ phòng:</span>
          <span class="font-semibold ml-2">{{ ownerUsername }}</span>
        </div>
        <div>
          <span class="text-gray-600">Thời gian:</span>
          <span class="font-semibold ml-2">{{ timeControl }}s/lượt</span>
        </div>
        <div>
          <span class="text-gray-600">Người chơi:</span>
          <span class="font-semibold ml-2">{{ players.length }}/2</span>
        </div>
        <div>
          <span class="text-gray-600">Trạng thái:</span>
          <span class="font-semibold ml-2" :class="statusClass">
            {{ statusText }}
          </span>
        </div>
      </div>
    </div>

    <!-- Players in Room -->
    <div class="mb-6">
      <h4 class="text-lg font-semibold mb-3">Người chơi trong phòng</h4>
      <div class="space-y-2">
        <div
          v-for="player in players"
          :key="player.id"
          class="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
        >
          <div class="flex items-center space-x-3">
            <div class="w-10 h-10 bg-blue-500 rounded-full flex items-center justify-center text-white font-bold">
              {{ player.username.charAt(0).toUpperCase() }}
            </div>
            <div>
              <p class="font-medium">{{ player.username }}</p>
              <p v-if="player.id === ownerId" class="text-xs text-gray-500">Chủ phòng</p>
            </div>
          </div>
          <button
            v-if="isOwner && player.id !== currentUserId && players.length > 1"
            @click="$emit('kick-player', player.id)"
            class="px-3 py-1 bg-red-500 text-white text-sm rounded hover:bg-red-600 transition"
          >
            Kick
          </button>
        </div>
      </div>
    </div>

    <!-- Join Requests (Owner only) -->
    <div v-if="isOwner && joinRequests && joinRequests.length > 0" class="mb-6">
      <h4 class="text-lg font-semibold mb-3">Yêu cầu vào phòng</h4>
      <div class="space-y-3">
        <div
          v-for="request in joinRequests"
          :key="request.userId"
          class="p-4 bg-yellow-50 border border-yellow-200 rounded-lg"
        >
          <div class="flex items-center justify-between">
            <div>
              <p class="font-medium">{{ request.username }}</p>
              <p class="text-sm text-gray-600">Muốn vào phòng</p>
              <p v-if="request.countdown > 0" class="text-xs text-yellow-600 mt-1">
                Còn lại: {{ request.countdown }}s
              </p>
            </div>
            <div class="flex space-x-2">
              <button
                @click="$emit('accept-join', request.userId)"
                class="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition text-sm font-medium"
              >
                Đồng ý
              </button>
              <button
                @click="$emit('reject-join', request.userId)"
                class="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition text-sm font-medium"
              >
                Từ chối
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Online Players List (for inviting) -->
    <div v-if="isOwner && players.length < 2">
      <h4 class="text-lg font-semibold mb-3">Mời người chơi</h4>
      <div v-if="onlineUsers.length === 0" class="text-center py-4 text-gray-500 text-sm">
        Không có người chơi online
      </div>
      <div v-else class="space-y-2 max-h-60 overflow-y-auto">
        <div
          v-for="user in availableUsers"
          :key="user.id"
          class="flex items-center justify-between p-3 bg-gray-50 rounded-lg hover:bg-gray-100 transition"
        >
          <div class="flex items-center space-x-3">
            <div class="w-8 h-8 bg-green-500 rounded-full flex items-center justify-center text-white text-xs font-bold">
              {{ user.username.charAt(0).toUpperCase() }}
            </div>
            <span class="font-medium">{{ user.username }}</span>
          </div>
          <button
            @click="$emit('invite-player', user.id)"
            :disabled="invitingUserId === user.id"
            class="px-4 py-1 bg-blue-600 text-white text-sm rounded hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed transition"
          >
            <span v-if="invitingUserId === user.id" class="flex items-center">
              <svg class="animate-spin h-4 w-4 mr-1" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              Đang mời...
            </span>
            <span v-else>Mời</span>
          </button>
        </div>
      </div>
    </div>

    <!-- Waiting for owner (if not owner and room not full) -->
    <div v-if="!isOwner && players.length < 2" class="mt-6 p-4 bg-blue-50 border border-blue-200 rounded-lg">
      <div class="flex items-center justify-center space-x-2">
        <svg class="animate-spin h-5 w-5 text-blue-600" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <p class="text-blue-800 font-medium">Đang chờ chủ phòng đồng ý...</p>
        <p v-if="waitingCountdown && waitingCountdown > 0" class="text-blue-600 text-sm">({{ waitingCountdown }}s)</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
// RoomView hiển thị chi tiết phòng hiện tại, xử lý accept/reject join, invite/kick và trạng thái countdown chờ.
import { computed } from 'vue';

interface Player {
  id: number;
  username: string;
}

interface OnlineUser {
  id: number;
  username: string;
  isInGame?: boolean;
}

interface JoinRequest {
  userId: number;
  username: string;
  countdown: number;
}

const props = defineProps<{
  roomId: number;
  ownerId: number;
  ownerUsername: string;
  players: Player[];
  onlineUsers: OnlineUser[];
  onlineCount: number;
  timeControl: number;
  currentUserId: number;
  joinRequests?: JoinRequest[];
  invitingUserId?: number | null;
  waitingCountdown?: number | null;
}>();

const emit = defineEmits<{
  'leave-room': [];
  'invite-player': [userId: number];
  'kick-player': [userId: number];
  'accept-join': [userId: number];
  'reject-join': [userId: number];
}>();

const isOwner = computed(() => props.currentUserId === props.ownerId);

const statusText = computed(() => {
  if (props.players.length === 2) return 'Đã đủ người';
  if (props.players.length === 1) return 'Đang chờ người chơi';
  return 'Trống';
});

const statusClass = computed(() => {
  if (props.players.length === 2) return 'text-green-600';
  return 'text-yellow-600';
});

const availableUsers = computed(() => {
  // Filter out users already in room
  const playerIds = props.players.map(p => p.id);
  return props.onlineUsers.filter(
    u => !playerIds.includes(u.id) && !u.isInGame
  );
});
</script>

