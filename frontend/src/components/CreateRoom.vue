<template>
  <div class="space-y-4">
    <div>
      <label class="block text-sm font-medium mb-1">Thời gian mỗi lượt (giây)</label>
      <input
        v-model.number="timeControl"
        type="number"
        min="30"
        max="600"
        class="w-full px-3 py-2 border rounded"
      />
    </div>
    <button
      @click="createRoom"
      :disabled="loading"
      class="w-full px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:bg-gray-400"
    >
      {{ loading ? 'Đang tạo...' : 'Tạo phòng' }}
    </button>
  </div>
</template>

<script setup lang="ts">
// Form tạo phòng PvP: nhập thời gian mỗi lượt, kiểm tra đăng nhập & kết nối SignalR trước khi gọi hub.CreateGame.
import { ref } from 'vue';
import { useAuthStore } from '@/stores/auth';
import { hub } from '@/services/signalr';

const emit = defineEmits<{
  created: [roomId: number, timeControl: number];
}>();

const authStore = useAuthStore();
const timeControl = ref(300);
const loading = ref(false);

async function createRoom() {
  // Kiểm tra authentication
  if (!authStore.isAuthenticated) {
    alert('Vui lòng đăng nhập để tạo phòng');
    return;
  }
  
  // Nếu có token nhưng chưa có user, thử fetch user
  if (!authStore.user && authStore.token) {
    try {
      await authStore.fetchUser();
    } catch (error) {
      console.error('Failed to fetch user:', error);
      alert('Không thể lấy thông tin người dùng. Vui lòng đăng nhập lại.');
      return;
    }
  }
  
  // Kiểm tra lại sau khi fetch
  if (!authStore.user) {
    console.warn('Cannot create room: user not authenticated');
    alert('Vui lòng đăng nhập để tạo phòng');
    return;
  }
  
  // Kiểm tra connection state
  if (hub.state !== 'Connected') {
    console.error('Cannot create room: SignalR not connected. State:', hub.state);
    alert('Kết nối chưa sẵn sàng. Vui lòng đợi...');
    return;
  }
  
  loading.value = true;
  try {
    console.log('Creating game with params:', {
      mode: 'PvP',
      p1UserId: authStore.user.id,
      timeControl: timeControl.value
    });
    
    const result = await hub.invoke('CreateGame', 'PvP', authStore.user.id, null, null, timeControl.value);
    console.log('CreateGame result:', result);
    
    // Emit room info immediately
    if (result && typeof result === 'object' && 'Id' in result) {
      emit('created', result.Id, timeControl.value);
    }
  } catch (error: any) {
    console.error('Failed to create room:', error);
    alert(`Không thể tạo phòng: ${error.message || 'Lỗi không xác định'}`);
  } finally {
    loading.value = false;
  }
}
</script>

