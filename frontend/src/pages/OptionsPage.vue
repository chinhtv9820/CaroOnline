<template>
  <div class="max-w-2xl mx-auto">
    <h1 class="text-3xl font-bold mb-6">Tùy chọn</h1>

    <div class="space-y-6">
      <!-- Turn Time Control -->
      <div class="bg-white rounded-lg shadow-md p-6">
        <h2 class="text-xl font-semibold mb-4 flex items-center">
          <svg class="w-6 h-6 mr-2 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          Thời gian lượt
        </h2>
        <TimeSelect v-model="timeControl" />
        <p class="mt-2 text-sm text-gray-500">
          Thời gian tối đa cho mỗi lượt đi (giây)
        </p>
      </div>

      <!-- Theme Selection -->
      <div class="bg-white rounded-lg shadow-md p-6">
        <h2 class="text-xl font-semibold mb-4 flex items-center">
          <svg class="w-6 h-6 mr-2 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 21a4 4 0 01-4-4V5a2 2 0 012-2h4a2 2 0 012 2v12a4 4 0 01-4 4zm0 0h12a2 2 0 002-2v-4a2 2 0 00-2-2h-2.343M11 7.343l1.657-1.657a2 2 0 012.828 0l2.829 2.829a2 2 0 010 2.828l-8.486 8.485M7 17h.01" />
          </svg>
          Giao diện
        </h2>
        <ThemeSelect v-model="theme" />
        <p class="mt-2 text-sm text-gray-500">
          Chọn theme cho bàn cờ và giao diện
        </p>
      </div>

      <!-- Sound & Effects -->
      <div class="bg-white rounded-lg shadow-md p-6">
        <h2 class="text-xl font-semibold mb-4 flex items-center">
          <svg class="w-6 h-6 mr-2 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15.536 8.464a5 5 0 010 7.072m2.828-9.9a9 9 0 010 12.728M5.586 15H4a1 1 0 01-1-1v-4a1 1 0 011-1h1.586l4.707-4.707C10.923 3.663 12 4.109 12 5v14c0 .891-1.077 1.337-1.707.707L5.586 15z" />
          </svg>
          Âm thanh & Hiệu ứng
        </h2>
        <div class="space-y-4">
          <SoundToggle v-model="soundEnabled" label="Bật âm thanh" />
          
          <!-- Animation Toggle -->
          <div class="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
            <div>
              <label class="text-sm font-medium text-gray-700">Bật animation</label>
              <p class="text-xs text-gray-500 mt-1">Hiệu ứng khi đặt quân và nước đi</p>
            </div>
            <button
              @click="animationEnabled = !animationEnabled"
              :class="[
                'relative inline-flex h-6 w-11 items-center rounded-full transition-colors',
                animationEnabled ? 'bg-blue-600' : 'bg-gray-300'
              ]"
            >
              <span
                :class="[
                  'inline-block h-4 w-4 transform rounded-full bg-white transition-transform',
                  animationEnabled ? 'translate-x-6' : 'translate-x-1'
                ]"
              />
            </button>
          </div>
        </div>
      </div>

      <!-- Save Button -->
      <div class="flex justify-end space-x-3">
        <button
          @click="resetOptions"
          class="px-6 py-2 bg-gray-300 text-gray-700 rounded-lg hover:bg-gray-400 transition font-medium"
        >
          Đặt lại
        </button>
        <button
          @click="saveOptions"
          class="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition font-medium shadow-md"
        >
          Lưu cài đặt
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
// Trang cấu hình cá nhân: lưu thời gian lượt, theme, âm thanh và animation vào localStorage để áp dụng cho trận mới.
import { ref, onMounted } from 'vue';
import TimeSelect from '@/components/TimeSelect.vue';
import ThemeSelect from '@/components/ThemeSelect.vue';
import SoundToggle from '@/components/SoundToggle.vue';

interface GameOptions {
  timeControl: number;
  theme: string;
  soundEnabled: boolean;
  animationEnabled: boolean;
}

const timeControl = ref(300);
const theme = ref('default');
const soundEnabled = ref(true);
const animationEnabled = ref(true);

const defaultOptions: GameOptions = {
  timeControl: 300,
  theme: 'default',
  soundEnabled: true,
  animationEnabled: true
};

onMounted(() => {
  // Load saved options from localStorage
  const saved = localStorage.getItem('gameOptions');
  if (saved) {
    try {
      const options: GameOptions = JSON.parse(saved);
      timeControl.value = options.timeControl || defaultOptions.timeControl;
      theme.value = options.theme || defaultOptions.theme;
      soundEnabled.value = options.soundEnabled !== false;
      animationEnabled.value = options.animationEnabled !== false;
    } catch (error) {
      console.error('Failed to load options:', error);
      resetOptions();
    }
  }
});

function saveOptions() {
  const options: GameOptions = {
    timeControl: timeControl.value,
    theme: theme.value,
    soundEnabled: soundEnabled.value,
    animationEnabled: animationEnabled.value
  };
  localStorage.setItem('gameOptions', JSON.stringify(options));
  
  // Apply theme immediately
  applyTheme(theme.value);
  
  // Show success notification (you can use Notification component here)
  alert('Đã lưu cài đặt thành công!');
}

function resetOptions() {
  timeControl.value = defaultOptions.timeControl;
  theme.value = defaultOptions.theme;
  soundEnabled.value = defaultOptions.soundEnabled;
  animationEnabled.value = defaultOptions.animationEnabled;
  saveOptions();
}

function applyTheme(themeName: string) {
  // Remove existing theme classes
  document.documentElement.classList.remove('theme-default', 'theme-dark', 'theme-colorful');
  // Add new theme class
  document.documentElement.classList.add(`theme-${themeName}`);
}
</script>

