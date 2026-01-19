import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { api } from '@/services/api';

export interface User {
  id: number;
  username: string;
  displayName?: string;
}

export const useAuthStore = defineStore('auth', () => {
  // Store quản lý đăng nhập: lưu token, thông tin user và cung cấp login/register/fetch/logout.
  const token = ref<string | null>(localStorage.getItem('token'));
  const user = ref<User | null>(null);

  const isAuthenticated = computed(() => !!token.value);

  async function login(usernameOrEmail: string, password: string) {
    try {
      const response = await api.post('/api/auth/login', { 
        usernameOrEmail, 
        password 
      });
      token.value = response.data.token;
      // Backend trả về { Id, Username, Email, DisplayName, token }
      user.value = {
        id: response.data.id || response.data.Id,
        username: response.data.username || response.data.Username,
        displayName: response.data.displayName || response.data.DisplayName
      };
      localStorage.setItem('token', token.value);
      return { success: true };
    } catch (error: any) {
      return { success: false, error: error.response?.data?.message || 'Login failed' };
    }
  }

  async function register(username: string, email: string, password: string, displayName?: string) {
    try {
      const response = await api.post('/api/auth/register', { 
        username, 
        email, 
        password, 
        displayName 
      });
      token.value = response.data.token;
      // Backend trả về { Id, Username, Email, DisplayName, token }
      user.value = {
        id: response.data.id || response.data.Id,
        username: response.data.username || response.data.Username,
        displayName: response.data.displayName || response.data.DisplayName
      };
      localStorage.setItem('token', token.value);
      return { success: true };
    } catch (error: any) {
      return { success: false, error: error.response?.data?.message || 'Registration failed' };
    }
  }

  async function fetchUser() {
    if (!token.value) return;
    try {
      const response = await api.get('/api/users/me');
      // Backend trả về { Id, Username, DisplayName }
      user.value = {
        id: response.data.id || response.data.Id,
        username: response.data.username || response.data.Username,
        displayName: response.data.displayName || response.data.DisplayName
      };
    } catch (error) {
      logout();
    }
  }

  function logout() {
    token.value = null;
    user.value = null;
    localStorage.removeItem('token');
  }

  // Tự động fetch user khi có token (khi store được khởi tạo)
  if (token.value && !user.value) {
    fetchUser().catch(() => {
      // Nếu fetch fail, có thể token đã hết hạn, clear nó
      token.value = null;
      localStorage.removeItem('token');
    });
  }

  return {
    token,
    user,
    isAuthenticated,
    login,
    register,
    fetchUser,
    logout
  };
});

