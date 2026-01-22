<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
    <div class="max-w-md w-full space-y-8">
      <div>
        <h2 class="mt-6 text-center text-3xl font-extrabold text-gray-900">
          Tạo tài khoản mới
        </h2>
        <p class="mt-2 text-center text-sm text-gray-600">
          Hoặc
          <router-link to="/login" class="font-medium text-blue-600 hover:text-blue-500">
            đăng nhập nếu đã có tài khoản
          </router-link>
        </p>
      </div>
      
      <form class="mt-8 space-y-6" @submit.prevent="handleRegister">
        <div class="rounded-md shadow-sm -space-y-px">
          <!-- Username -->
          <div>
            <label for="username" class="sr-only">Tên đăng nhập</label>
            <input
              id="username"
              v-model="form.username"
              type="text"
              required
              class="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-t-md focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm"
              :class="{ 'border-red-500': errors.username }"
              placeholder="Tên đăng nhập"
              @blur="validateUsername"
            />
            <p v-if="errors.username" class="mt-1 text-sm text-red-600">{{ errors.username }}</p>
          </div>
          
          <!-- Email -->
          <div>
            <label for="email" class="sr-only">Email</label>
            <input
              id="email"
              v-model="form.email"
              type="email"
              required
              class="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm"
              :class="{ 'border-red-500': errors.email }"
              placeholder="Email"
              @blur="validateEmail"
            />
            <p v-if="errors.email" class="mt-1 text-sm text-red-600">{{ errors.email }}</p>
          </div>
          
          <!-- Password -->
          <div>
            <label for="password" class="sr-only">Mật khẩu</label>
            <input
              id="password"
              v-model="form.password"
              type="password"
              required
              class="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm"
              :class="{ 'border-red-500': errors.password }"
              placeholder="Mật khẩu (tối thiểu 6 ký tự)"
              @blur="validatePassword"
            />
            <p v-if="errors.password" class="mt-1 text-sm text-red-600">{{ errors.password }}</p>
          </div>
          
          <!-- Confirm Password -->
          <div>
            <label for="confirmPassword" class="sr-only">Xác nhận mật khẩu</label>
            <input
              id="confirmPassword"
              v-model="form.confirmPassword"
              type="password"
              required
              class="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-b-md focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm"
              :class="{ 'border-red-500': errors.confirmPassword }"
              placeholder="Xác nhận mật khẩu"
              @blur="validateConfirmPassword"
            />
            <p v-if="errors.confirmPassword" class="mt-1 text-sm text-red-600">{{ errors.confirmPassword }}</p>
          </div>
          
          <!-- Display Name (Optional) -->
          <div class="mt-4">
            <label for="displayName" class="sr-only">Tên hiển thị (tùy chọn)</label>
            <input
              id="displayName"
              v-model="form.displayName"
              type="text"
              class="appearance-none rounded-md relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm"
              placeholder="Tên hiển thị (tùy chọn)"
            />
          </div>
        </div>

        <!-- Error Message -->
        <div v-if="error" class="rounded-md bg-red-50 p-4">
          <div class="flex">
            <div class="flex-shrink-0">
              <svg class="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
              </svg>
            </div>
            <div class="ml-3">
              <p class="text-sm font-medium text-red-800">{{ error }}</p>
            </div>
          </div>
        </div>

        <!-- Success Message -->
        <div v-if="success" class="rounded-md bg-green-50 p-4">
          <div class="flex">
            <div class="flex-shrink-0">
              <svg class="h-5 w-5 text-green-400" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
              </svg>
            </div>
            <div class="ml-3">
              <p class="text-sm font-medium text-green-800">{{ success }}</p>
            </div>
          </div>
        </div>

        <div>
          <button
            type="submit"
            :disabled="loading || !isFormValid"
            class="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:bg-gray-400 disabled:cursor-not-allowed"
          >
            <span v-if="loading" class="absolute left-0 inset-y-0 flex items-center pl-3">
              <svg class="animate-spin h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
            </span>
            {{ loading ? 'Đang tạo tài khoản...' : 'Đăng ký' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { useAuthStore } from '@/stores/auth';

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();

const form = ref({
  username: '',
  email: '',
  password: '',
  confirmPassword: '',
  displayName: ''
});

const errors = ref<Record<string, string>>({});
const error = ref('');
const success = ref('');
const loading = ref(false);

// Email validation regex
const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

// Validate username
function validateUsername() {
  if (!form.value.username) {
    errors.value.username = 'Tên đăng nhập là bắt buộc';
    return false;
  }
  if (form.value.username.length < 3) {
    errors.value.username = 'Tên đăng nhập phải có ít nhất 3 ký tự';
    return false;
  }
  if (!/^[a-zA-Z0-9_]+$/.test(form.value.username)) {
    errors.value.username = 'Tên đăng nhập chỉ được chứa chữ cái, số và dấu gạch dưới';
    return false;
  }
  delete errors.value.username;
  return true;
}

// Validate email
function validateEmail() {
  if (!form.value.email) {
    errors.value.email = 'Email là bắt buộc';
    return false;
  }
  if (!emailRegex.test(form.value.email)) {
    errors.value.email = 'Email không hợp lệ';
    return false;
  }
  delete errors.value.email;
  return true;
}

// Validate password
function validatePassword() {
  if (!form.value.password) {
    errors.value.password = 'Mật khẩu là bắt buộc';
    return false;
  }
  if (form.value.password.length < 6) {
    errors.value.password = 'Mật khẩu phải có ít nhất 6 ký tự';
    return false;
  }
  delete errors.value.password;
  return true;
}

// Validate confirm password
function validateConfirmPassword() {
  if (!form.value.confirmPassword) {
    errors.value.confirmPassword = 'Vui lòng xác nhận mật khẩu';
    return false;
  }
  if (form.value.password !== form.value.confirmPassword) {
    errors.value.confirmPassword = 'Mật khẩu xác nhận không khớp';
    return false;
  }
  delete errors.value.confirmPassword;
  return true;
}

// Check if form is valid
const isFormValid = computed(() => {
  return form.value.username &&
         form.value.email &&
         form.value.password &&
         form.value.confirmPassword &&
         Object.keys(errors.value).length === 0;
});

// Handle form submission
async function handleRegister() {
  error.value = '';
  success.value = '';
  
  // Validate all fields
  const isUsernameValid = validateUsername();
  const isEmailValid = validateEmail();
  const isPasswordValid = validatePassword();
  const isConfirmPasswordValid = validateConfirmPassword();
  
  if (!isUsernameValid || !isEmailValid || !isPasswordValid || !isConfirmPasswordValid) {
    error.value = 'Vui lòng kiểm tra lại thông tin đã nhập';
    return;
  }
  
  loading.value = true;
  
  try {
    const result = await authStore.register(
      form.value.username,
      form.value.email,
      form.value.password,
      form.value.displayName || undefined
    );
    
    if (result.success) {
      success.value = 'Đăng ký thành công! Đang chuyển hướng...';
      // Redirect to the page user was trying to access, or default to /pve
      const redirectTo = (route.query.redirect as string) || '/pve';
      setTimeout(() => {
        router.push(redirectTo);
      }, 1000);
    } else {
      error.value = result.error || 'Đăng ký thất bại';
    }
  } catch (err: any) {
    error.value = err.response?.data?.message || err.message || 'Đã xảy ra lỗi khi đăng ký';
  } finally {
    loading.value = false;
  }
}
</script>

