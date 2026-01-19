import { defineStore } from 'pinia';
import { ref } from 'vue';

export interface OnlineUser {
  id: number;
  username: string;
  isInGame: boolean;
}

export interface GameRoom {
  id: number;
  ownerId: number;
  ownerUsername: string;
  players: OnlineUser[];
  maxPlayers: number;
  timeControl: number;
  createdAt: string;
}

export interface Challenge {
  id: string;
  fromUserId: number;
  fromUsername: string;
  toUserId: number;
  expiresAt: Date;
}

export const useLobbyStore = defineStore('lobby', () => {
  // Store lobby: quản lý danh sách người online, phòng hiện có, challenge gửi/nhận và trạng thái loading.
  // Online users list
  const onlineUsers = ref<OnlineUser[]>([]);
  const usersOnlineCount = ref(0);

  // Game rooms
  const availableRooms = ref<GameRoom[]>([]);
  const currentRoom = ref<GameRoom | null>(null);

  // Challenges
  const pendingChallenges = ref<Challenge[]>([]);
  const sentChallenges = ref<Challenge[]>([]);

  // Loading states
  const loadingUsers = ref(false);
  const loadingRooms = ref(false);
  const creatingRoom = ref(false);
  const joiningRoom = ref(false);

  // Actions
  function setOnlineUsers(users: OnlineUser[]) {
    onlineUsers.value = users;
    usersOnlineCount.value = users.length;
  }

  function addOnlineUser(user: OnlineUser) {
    if (!onlineUsers.value.find(u => u.id === user.id)) {
      onlineUsers.value.push(user);
      usersOnlineCount.value = onlineUsers.value.length;
    }
  }

  function removeOnlineUser(userId: number) {
    onlineUsers.value = onlineUsers.value.filter(u => u.id !== userId);
    usersOnlineCount.value = onlineUsers.value.length;
  }

  function setAvailableRooms(rooms: GameRoom[]) {
    availableRooms.value = rooms;
  }

  function addRoom(room: GameRoom) {
    if (!availableRooms.value.find(r => r.id === room.id)) {
      availableRooms.value.push(room);
    }
  }

  function removeRoom(roomId: number) {
    availableRooms.value = availableRooms.value.filter(r => r.id !== roomId);
  }

  function setCurrentRoom(room: GameRoom | null) {
    currentRoom.value = room;
  }

  function addPendingChallenge(challenge: Challenge) {
    if (!pendingChallenges.value.find(c => c.id === challenge.id)) {
      pendingChallenges.value.push(challenge);
    }
  }

  function removePendingChallenge(challengeId: string) {
    pendingChallenges.value = pendingChallenges.value.filter(c => c.id !== challengeId);
  }

  function addSentChallenge(challenge: Challenge) {
    if (!sentChallenges.value.find(c => c.id === challenge.id)) {
      sentChallenges.value.push(challenge);
    }
  }

  function removeSentChallenge(challengeId: string) {
    sentChallenges.value = sentChallenges.value.filter(c => c.id !== challengeId);
  }

  function reset() {
    onlineUsers.value = [];
    usersOnlineCount.value = 0;
    availableRooms.value = [];
    currentRoom.value = null;
    pendingChallenges.value = [];
    sentChallenges.value = [];
  }

  return {
    // State
    onlineUsers,
    usersOnlineCount,
    availableRooms,
    currentRoom,
    pendingChallenges,
    sentChallenges,
    loadingUsers,
    loadingRooms,
    creatingRoom,
    joiningRoom,
    // Actions
    setOnlineUsers,
    addOnlineUser,
    removeOnlineUser,
    setAvailableRooms,
    addRoom,
    removeRoom,
    setCurrentRoom,
    addPendingChallenge,
    removePendingChallenge,
    addSentChallenge,
    removeSentChallenge,
    reset
  };
});

