<template>
  <div class="max-w-md mx-auto bg-white rounded-lg shadow-lg p-6">
    <h2 class="text-2xl font-bold mb-4">Đăng nhập để chơi PVP</h2>
    <p class="text-gray-600 mb-6">Bạn cần đăng nhập để chơi với người khác</p>
    <div class="space-y-4">
      <button
        @click="goToLogin"
        class="w-full px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 transition"
      >
        Đăng nhập
      </button>
      <button
        @click="goToRegister"
        class="w-full px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700 transition"
      >
        Đăng ký
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useRouter, useRoute } from 'vue-router';

const router = useRouter();
const route = useRoute();

function goToLogin() {
  // Lưu trang hiện tại để redirect lại sau khi login
  router.push({
    path: '/login',
    query: { redirect: route.path }
  });
}

function goToRegister() {
  // Lưu trang hiện tại để redirect lại sau khi register
  router.push({
    path: '/register',
    query: { redirect: route.path }
  });
}
</script>

