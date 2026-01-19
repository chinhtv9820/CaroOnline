<template>
  <div class="space-y-6">
    <!-- Header with online count -->
    <div class="bg-white rounded-lg shadow-md p-4">
      <div class="flex items-center justify-between">
        <h2 class="text-2xl font-bold text-gray-900">Lobby</h2>
        <div class="flex items-center space-x-2 text-gray-600">
          <div class="w-3 h-3 bg-green-500 rounded-full animate-pulse"></div>
          <span class="text-sm font-medium">{{ usersOnlineCount }} người online</span>
        </div>
      </div>
    </div>

    <!-- Main Content Grid -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <!-- Create Room Section -->
      <div v-if="!currentRoom" class="lg:col-span-1">
        <div class="bg-white rounded-lg shadow-md p-6">
          <h3 class="text-lg font-semibold mb-4 flex items-center">
            <svg class="w-5 h-5 mr-2 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            Tạo phòng
          </h3>
          <CreateRoom @created="handleRoomCreated" />
        </div>
      </div>

      <!-- Room View (when room is created) -->
      <div v-if="currentRoom" class="lg:col-span-3">
        <RoomView
          :room-id="currentRoom.id"
          :owner-id="currentRoom.ownerId"
          :owner-username="currentRoom.ownerUsername"
          :players="currentRoom.players"
          :online-users="onlineUsers"
          :online-count="usersOnlineCount"
          :time-control="currentRoom.timeControl"
          :current-user-id="currentUserId"
          :join-requests="joinRequests"
          :inviting-user-id="invitingUserId"
          :waiting-countdown="waitingCountdown"
          @leave-room="handleLeaveRoom"
          @invite-player="handleInvitePlayer"
          @kick-player="handleKickPlayer"
          @accept-join="handleAcceptJoin"
          @reject-join="handleRejectJoin"
        />
      </div>

      <!-- Players List & Available Rooms -->
      <div class="lg:col-span-2 space-y-6">
        <!-- Online Players -->
        <div class="bg-white rounded-lg shadow-md p-6">
          <h3 class="text-lg font-semibold mb-4 flex items-center">
            <svg class="w-5 h-5 mr-2 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
            </svg>
            Người chơi online
          </h3>
          <PlayerList @invite="handleInvite" />
        </div>

        <!-- Available Rooms -->
        <div v-if="!currentRoom" class="bg-white rounded-lg shadow-md p-6">
          <div class="flex items-center justify-between mb-4">
            <h3 class="text-lg font-semibold flex items-center">
              <svg class="w-5 h-5 mr-2 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
              </svg>
              Phòng có sẵn
            </h3>
            <button
              @click="fetchAvailableRooms"
              class="px-3 py-1 text-sm bg-gray-100 text-gray-700 rounded hover:bg-gray-200 transition"
            >
              Làm mới
            </button>
          </div>
          <div v-if="loadingRooms" class="text-center py-8">
            <Spinner />
          </div>
          <div v-else-if="availableRoomsList.length === 0" class="text-center py-8 text-gray-500">
            <p>Chưa có phòng nào</p>
            <p class="text-sm mt-2">Tạo phòng mới để bắt đầu chơi!</p>
          </div>
          <div v-else class="space-y-3 max-h-96 overflow-y-auto">
            <div
              v-for="room in availableRoomsList"
              :key="room.id"
              class="border rounded-lg p-4 hover:bg-gray-50 transition cursor-pointer"
              @click="joinRoom(room.id)"
            >
              <div class="flex items-center justify-between">
                <div class="flex-1">
                  <p class="font-semibold text-lg">Phòng #{{ room.id }}</p>
                  <p class="text-sm text-gray-600 mt-1">
                    Chủ phòng: {{ room.ownerUsername || `User ${room.ownerId}` }} • 
                    {{ room.playerCount || 1 }}/2 người chơi
                  </p>
                  <p class="text-xs text-gray-500 mt-1">
                    Thời gian: {{ room.timeControl || 300 }}s/lượt
                  </p>
                </div>
                <button
                  @click.stop="joinRoom(room.id)"
                  :disabled="joiningRoomId === room.id"
                  class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:bg-gray-400 transition text-sm font-medium flex items-center space-x-2"
                >
                  <span v-if="joiningRoomId === room.id">
                    <svg class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    <span>Đang vào...</span>
                  </span>
                  <span v-else>Vào phòng</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Pending Challenge Notification -->
    <div
      v-if="pendingChallenge"
      class="bg-yellow-50 border-l-4 border-yellow-400 rounded-lg p-4 shadow-md"
    >
      <div class="flex items-start">
        <div class="flex-shrink-0">
          <svg class="h-6 w-6 text-yellow-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
          </svg>
        </div>
        <div class="ml-3 flex-1">
          <p class="font-semibold text-yellow-800">Lời mời từ: User #{{ pendingChallenge.fromUserId }}</p>
          <div class="mt-3 flex space-x-2">
            <button
              @click="acceptChallenge"
              class="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition font-medium"
            >
              Chấp nhận
            </button>
            <button
              @click="rejectChallenge"
              class="px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition font-medium"
            >
              Từ chối
            </button>
          </div>
          <div v-if="challengeCountdown > 0" class="mt-3 flex items-center space-x-2 text-sm text-yellow-700">
            <svg class="w-4 h-4 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span>Còn lại: {{ challengeCountdown }}s</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
// Component điều phối toàn bộ giao diện Lobby: tạo phòng, hiển thị danh sách phòng/người chơi,
// quản lý join request/challenge và nhúng RoomView khi chủ phòng đang ở trong phòng.
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useSignalRStore } from '@/stores/signalr';
import { useLobbyStore } from '@/stores/lobby';
import { useAuthStore } from '@/stores/auth';
import { hub } from '@/services/signalr';
import { api } from '@/services/api';
import CreateRoom from './CreateRoom.vue';
import PlayerList from './PlayerList.vue';
import RoomView from './RoomView.vue';
import Spinner from './Spinner.vue';

const emit = defineEmits<{
  gameStarted: [];
}>();

const signalRStore = useSignalRStore();
const lobbyStore = useLobbyStore();
const authStore = useAuthStore();

const pendingChallenge = ref<{ id: string; fromUserId: number } | null>(null);
const challengeCountdown = ref(0);

// Room state
const currentRoom = ref<{
  id: number;
  ownerId: number;
  ownerUsername: string;
  players: Array<{ id: number; username: string }>;
  timeControl: number;
} | null>(null);

// Online users
const onlineUsers = ref<Array<{ id: number; username: string; isInGame?: boolean }>>([]);
const usersOnlineCount = computed(() => onlineUsers.value.length);
const availableRoomsList = ref<Array<{
  id: number;
  ownerId: number;
  ownerUsername?: string;
  playerCount: number;
  timeControl: number;
}>>([]);
const loadingRooms = ref(false);
const joiningRoomId = ref<number | null>(null);

// Join requests
const joinRequests = ref<Array<{ userId: number; username: string; countdown: number }>>([]);
const waitingCountdown = ref(0);
const invitingUserId = ref<number | null>(null);

const currentUserId = computed(() => authStore.user?.id || 0);

onMounted(async () => {
  try {
    await signalRStore.start();
    
    // Đợi một chút để đảm bảo OnConnectedAsync đã hoàn thành
    await new Promise(resolve => setTimeout(resolve, 500));
    
    // Chỉ invoke khi connection đã sẵn sàng
    if (hub.state === 'Connected') {
      console.log('Calling JoinLobby...');
      await hub.invoke('JoinLobby');
      console.log('JoinLobby completed');
    } else {
      console.warn('Hub not connected, state:', hub.state);
    }
    
    setupSignalRHandlers();
    await fetchAvailableRooms();
  } catch (error) {
    console.error('Failed to setup lobby:', error);
  }
});

async function fetchAvailableRooms() {
  loadingRooms.value = true;
  try {
    // Fetch available games (rooms) from API
    const response = await api.get('/api/games');
    const games = response.data || [];
    
    // Filter for PvP games that are not full and not finished
    availableRoomsList.value = games
      .filter((g: any) => 
        g.mode === 'PvP' && 
        !g.finishedAt && 
        (!g.p2UserId || g.p2UserId === null)
      )
      .map((g: any) => ({
        id: g.id,
        ownerId: g.roomOwnerId || g.p1UserId || 0,
        ownerUsername: undefined, // Will be fetched separately if needed
        playerCount: (g.p1UserId ? 1 : 0) + (g.p2UserId ? 1 : 0),
        timeControl: g.timeControlSeconds || 300
      }));
  } catch (error) {
    console.error('Failed to fetch available rooms:', error);
    availableRoomsList.value = [];
  } finally {
    loadingRooms.value = false;
  }
}

onUnmounted(() => {
  cleanupSignalRHandlers();
});

function setupSignalRHandlers() {
  hub.on('LobbyUpdate', (data: any) => {
    const count = data.usersOnline || 0;
    lobbyStore.usersOnlineCount = count;
    // Update count from onlineUsers if available
    if (onlineUsers.value.length > 0) {
      // Count is already updated from OnlineUsersList
    }
  });

  hub.on('OnlineUsersList', (userIds: number[]) => {
    console.log('Received online users:', userIds);
    console.log('Current user ID:', currentUserId.value);
    
    // In real app, fetch usernames from API
    onlineUsers.value = userIds.map(id => ({
      id,
      username: `User ${id}`,
      isInGame: false
    }));
    
    // Update count
    lobbyStore.usersOnlineCount = userIds.length;
    
    console.log('Updated onlineUsers:', onlineUsers.value);
    console.log('Updated usersOnlineCount:', lobbyStore.usersOnlineCount);
  });

  hub.on('RoomCreated', async (data: any) => {
    // Room created but game not started yet (waiting for 2nd player)
    console.log('Room created:', data);
    // Update current room if it matches
    if (currentRoom.value && currentRoom.value.id === data.gameId) {
      // Room already set up, just confirm
      return;
    }
    // If room not set up yet, fetch it
    try {
      const response = await api.get(`/api/games/${data.gameId}`);
      const gameData = response.data;
      currentRoom.value = {
        id: data.gameId,
        ownerId: gameData.roomOwnerId || gameData.p1UserId || currentUserId.value,
        ownerUsername: authStore.user?.username || `User ${currentUserId.value}`,
        players: gameData.p1UserId ? [{
          id: gameData.p1UserId,
          username: authStore.user?.username || `User ${gameData.p1UserId}`
        }] : [],
        timeControl: gameData.timeControlSeconds || 300
      };
    } catch (error) {
      console.error('Failed to fetch room data on RoomCreated:', error);
    }
  });

  hub.on('GameStarted', async (data: any) => {
    // Game actually started (2 players present)
    console.log('Game started:', data);
    // Fetch game data
    try {
      const response = await api.get(`/api/games/${data.gameId}`);
      const gameData = response.data;
      
      // Update current room with game data
      if (currentRoom.value && currentRoom.value.id === data.gameId) {
        currentRoom.value.players = [
          { id: gameData.p1UserId, username: `User ${gameData.p1UserId}` },
          ...(gameData.p2UserId ? [{ id: gameData.p2UserId, username: `User ${gameData.p2UserId}` }] : [])
        ];
      }
      
      emit('gameStarted');
    } catch (error) {
      console.error('Failed to fetch game data:', error);
      emit('gameStarted');
    }
  });

  hub.on('PlayerJoined', (data: any) => {
    if (currentRoom.value && currentRoom.value.id === data.gameId) {
      // Add player to room if not already present
      const playerExists = currentRoom.value.players.some(p => p.id === data.userId);
      if (!playerExists) {
        currentRoom.value.players.push({
          id: data.userId,
          username: `User ${data.userId}`
        });
      }
    }
  });

  // Join request handlers (for room owner)
  hub.on('JoinRequest', (data: any) => {
    if (currentRoom.value && currentRoom.value.id === data.gameId) {
      joinRequests.value.push({
        userId: data.userId,
        username: data.username || `User ${data.userId}`,
        countdown: 8
      });
      
      // Start countdown
      const requestIndex = joinRequests.value.length - 1;
      const interval = setInterval(() => {
        if (joinRequests.value[requestIndex]) {
          joinRequests.value[requestIndex].countdown--;
          if (joinRequests.value[requestIndex].countdown <= 0) {
            clearInterval(interval);
            joinRequests.value = joinRequests.value.filter(r => r.userId !== data.userId);
          }
        } else {
          clearInterval(interval);
        }
      }, 1000);
    }
  });

  // Join request countdown (for requester)
  hub.on('JoinRequestCountdown', (data: any) => {
    if (currentRoom.value && currentRoom.value.id === data.gameId) {
      waitingCountdown.value = data.remainingSeconds;
    }
  });

  hub.on('JoinRequestTimeout', (data: any) => {
    if (currentRoom.value && currentRoom.value.id === data.gameId) {
      waitingCountdown.value = 0;
      alert('Yêu cầu vào phòng đã hết hạn');
      currentRoom.value = null;
    }
  });

  hub.on('JoinRequestRejected', (data: any) => {
    if (currentRoom.value && currentRoom.value.id === data.gameId) {
      waitingCountdown.value = 0;
      alert('Chủ phòng đã từ chối yêu cầu vào phòng');
      currentRoom.value = null;
    }
  });

  hub.on('ChallengeReceived', (data: any) => {
    pendingChallenge.value = { id: data.id, fromUserId: data.fromUserId };
    challengeCountdown.value = 10;
    
    const interval = setInterval(() => {
      challengeCountdown.value--;
      if (challengeCountdown.value <= 0) {
        clearInterval(interval);
        pendingChallenge.value = null;
      }
    }, 1000);
  });

  hub.on('ChallengeCountdown', (data: any) => {
    if (data.challengeId === pendingChallenge.value?.id) {
      challengeCountdown.value = data.remainingSeconds;
    }
  });

  hub.on('ChallengeTimeout', () => {
    pendingChallenge.value = null;
    challengeCountdown.value = 0;
  });
}

async function handleRoomCreated(roomId: number, timeControl: number) {
  // Fetch game/room data
  try {
    const response = await api.get(`/api/games/${roomId}`);
    const gameData = response.data;
    
    currentRoom.value = {
      id: roomId,
      ownerId: gameData.roomOwnerId || gameData.p1UserId || currentUserId.value,
      ownerUsername: authStore.user?.username || `User ${currentUserId.value}`,
      players: gameData.p1UserId ? [{
        id: gameData.p1UserId,
        username: authStore.user?.username || `User ${gameData.p1UserId}`
      }] : [],
      timeControl
    };
    
    // Ensure we're in lobby group first
    try {
      if (hub.state === 'Connected') {
        await hub.invoke('JoinLobby');
      }
    } catch (error: any) {
      console.error('Failed to join lobby:', error);
    }
    
    // Join game group (as owner, this will just add to group, not start game)
    try {
      await hub.invoke('JoinGame', roomId);
      console.log('Successfully joined game group as owner');
    } catch (error: any) {
      console.error('Failed to join game group:', error);
      // If already in group or other error, continue
    }
  } catch (error) {
    console.error('Failed to fetch room data:', error);
  }
}

function handleLeaveRoom() {
  currentRoom.value = null;
  joinRequests.value = [];
  waitingCountdown.value = 0;
}

async function handleInvitePlayer(userId: number) {
  if (!currentRoom.value) return;
  
  invitingUserId.value = userId;
  try {
    await hub.invoke('SendChallenge', userId, 10);
  } catch (error: any) {
    console.error('Failed to invite player:', error);
    alert(`Không thể mời người chơi: ${error.message || 'Lỗi không xác định'}`);
  } finally {
    setTimeout(() => {
      invitingUserId.value = null;
    }, 2000);
  }
}

async function handleKickPlayer(userId: number) {
  if (!currentRoom.value) return;
  
  try {
    await hub.invoke('KickPlayer', currentRoom.value.id, userId);
    currentRoom.value.players = currentRoom.value.players.filter(p => p.id !== userId);
  } catch (error: any) {
    console.error('Failed to kick player:', error);
    alert(`Không thể kick người chơi: ${error.message || 'Lỗi không xác định'}`);
  }
}

async function handleAcceptJoin(userId: number) {
  if (!currentRoom.value) return;
  
  try {
    await hub.invoke('AcceptJoinRequest', currentRoom.value.id, userId);
    joinRequests.value = joinRequests.value.filter(r => r.userId !== userId);
  } catch (error: any) {
    console.error('Failed to accept join request:', error);
    alert(`Không thể chấp nhận: ${error.message || 'Lỗi không xác định'}`);
  }
}

async function handleRejectJoin(userId: number) {
  if (!currentRoom.value) return;
  
  try {
    await hub.invoke('RejectJoinRequest', currentRoom.value.id, userId);
    joinRequests.value = joinRequests.value.filter(r => r.userId !== userId);
  } catch (error: any) {
    console.error('Failed to reject join request:', error);
  }
}

async function joinRoom(roomId: number) {
  if (joiningRoomId.value === roomId) return;
  
  joiningRoomId.value = roomId;
  waitingCountdown.value = 8;
  
  try {
    // Ensure we're in lobby and presence is updated
    if (hub.state === 'Connected') {
      try {
        await hub.invoke('JoinLobby');
        console.log('Joined lobby before joining room');
      } catch (error: any) {
        console.warn('Failed to join lobby before joining room:', error);
      }
    }
    
    // Join game (will trigger join request if room has owner)
    console.log(`Attempting to join room ${roomId}...`);
    await hub.invoke('JoinGame', roomId);
    console.log(`Successfully joined room ${roomId}`);
    
    // Fetch room data
    const response = await api.get(`/api/games/${roomId}`);
    const gameData = response.data;
    
    currentRoom.value = {
      id: roomId,
      ownerId: gameData.roomOwnerId || gameData.p1UserId || 0,
      ownerUsername: `User ${gameData.roomOwnerId || gameData.p1UserId || 0}`,
      players: [
        ...(gameData.p1UserId ? [{
          id: gameData.p1UserId,
          username: `User ${gameData.p1UserId}`
        }] : []),
        ...(gameData.p2UserId ? [{
          id: gameData.p2UserId,
          username: `User ${gameData.p2UserId}`
        }] : [])
      ],
      timeControl: gameData.timeControlSeconds || 300
    };
    
    // If user is owner or already in room, no need to wait
    if (currentRoom.value.ownerId === currentUserId.value || 
        currentRoom.value.players.some(p => p.id === currentUserId.value)) {
      waitingCountdown.value = 0;
      joiningRoomId.value = null;
    }
    
    // Start countdown timer
    const countdownInterval = setInterval(() => {
      if (waitingCountdown.value > 0) {
        waitingCountdown.value--;
      } else {
        clearInterval(countdownInterval);
        if (currentRoom.value && currentRoom.value.players.length < 2) {
          // Still waiting, but countdown expired
          joiningRoomId.value = null;
        }
      }
    }, 1000);
  } catch (error: any) {
    console.error('Failed to join room:', error);
    alert(`Không thể vào phòng: ${error.message || 'Lỗi không xác định'}`);
    waitingCountdown.value = 0;
    joiningRoomId.value = null;
    currentRoom.value = null;
  }
}

function cleanupSignalRHandlers() {
  hub.off('ChallengeReceived');
  hub.off('ChallengeCountdown');
  hub.off('ChallengeTimeout');
  hub.off('GameStarted');
}

async function acceptChallenge() {
  if (!pendingChallenge.value) return;
  
  try {
    await hub.invoke('AcceptChallenge', pendingChallenge.value.id);
    pendingChallenge.value = null;
  } catch (error) {
    console.error('Failed to accept challenge:', error);
  }
}

async function rejectChallenge() {
  if (!pendingChallenge.value) return;
  
  try {
    await hub.invoke('RejectChallenge', pendingChallenge.value.id);
    pendingChallenge.value = null;
  } catch (error) {
    console.error('Failed to reject challenge:', error);
  }
}

function handleInvite(userId: number) {
  // If in room, use room invite
  if (currentRoom.value) {
    handleInvitePlayer(userId);
  } else {
    // Otherwise use challenge system
    hub.invoke('SendChallenge', userId, 10);
  }
}
</script>

