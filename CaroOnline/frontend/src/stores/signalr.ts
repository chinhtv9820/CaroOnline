import { defineStore } from 'pinia';
import { ref } from 'vue';
import { hub } from '@/services/signalr';

export const useSignalRStore = defineStore('signalr', () => {
  // Store giúp theo dõi trạng thái kết nối SignalR (isConnected, connectionId) và cung cấp hàm start/stop an toàn.
  const isConnected = ref(false);
  const connectionId = ref<string | null>(null);

  async function start() {
    // Nếu đã connected, cập nhật state và return
    if (hub.state === 'Connected') {
      isConnected.value = true;
      connectionId.value = hub.connectionId;
      return;
    }
    
    // Nếu đang connecting hoặc reconnecting, đợi đến khi connected hoặc disconnected
    if (hub.state === 'Connecting' || hub.state === 'Reconnecting') {
      return new Promise<void>((resolve, reject) => {
        let attempts = 0;
        const maxAttempts = 50; // 5 giây timeout
        
        const checkConnection = () => {
          attempts++;
          
          if (hub.state === 'Connected') {
            isConnected.value = true;
            connectionId.value = hub.connectionId;
            resolve();
          } else if (hub.state === 'Disconnected') {
            // Nếu disconnected sau khi connecting, thử start lại
            start().then(resolve).catch(reject);
          } else if (attempts >= maxAttempts) {
            reject(new Error('Connection timeout'));
          } else {
            // Vẫn đang connecting, đợi thêm
            setTimeout(checkConnection, 100);
          }
        };
        checkConnection();
      });
    }
    
    // Chỉ start nếu đang ở trạng thái Disconnected
    if (hub.state !== 'Disconnected') {
      return; // Không làm gì nếu ở trạng thái khác
    }
    
    try {
      await hub.start();
      isConnected.value = true;
      connectionId.value = hub.connectionId;
    } catch (error) {
      console.error('SignalR connection failed:', error);
      isConnected.value = false;
      throw error;
    }
  }

  async function stop() {
    if (hub.state === 'Disconnected') return;
    await hub.stop();
    isConnected.value = false;
    connectionId.value = null;
  }

  // Listen to connection events
  hub.onclose(() => {
    isConnected.value = false;
    connectionId.value = null;
  });

  hub.onreconnected((id) => {
    isConnected.value = true;
    connectionId.value = id || null;
  });

  return {
    isConnected,
    connectionId,
    start,
    stop
  };
});

