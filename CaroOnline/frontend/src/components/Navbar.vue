<template>
  <nav class="bg-blue-600 text-white shadow-lg">
    <div class="container mx-auto px-4">
      <div class="flex items-center justify-between h-16">
        <div class="flex items-center space-x-8">
          <router-link to="/" class="text-xl font-bold">Caro Online</router-link>
          <router-link
            to="/"
            class="px-3 py-2 rounded hover:bg-blue-700 transition"
            active-class="bg-blue-800"
          >
            Menu
          </router-link>
          <div class="flex space-x-4">
            <router-link
              v-for="item in menuItems"
              :key="item.path"
              :to="item.path"
              class="px-3 py-2 rounded hover:bg-blue-700 transition"
              active-class="bg-blue-800"
            >
              {{ item.label }}
            </router-link>
          </div>
        </div>
        <div class="flex items-center space-x-4">
          <span v-if="authStore.isAuthenticated" class="text-sm">
            {{ authStore.user?.username }}
          </span>
          <button
            v-if="authStore.isAuthenticated"
            @click="handleLogout"
            class="px-4 py-2 bg-red-500 rounded hover:bg-red-600 transition"
          >
            Đăng xuất
          </button>
        </div>
      </div>
    </div>
  </nav>
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/auth';
import { useRouter } from 'vue-router';

const authStore = useAuthStore();
const router = useRouter();

const menuItems = [
  { path: '/pve', label: 'PVE' },
  { path: '/pvp', label: 'PVP' },
  { path: '/ranking', label: 'Ranking' },
  { path: '/history', label: 'History' },
  { path: '/options', label: 'Options' }
];

function handleLogout() {
  authStore.logout();
  router.push('/pve');
}
</script>

