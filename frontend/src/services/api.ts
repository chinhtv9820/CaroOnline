/// <reference types="../vite-env.d.ts" />
import axios from 'axios'

// Auto-detect API URL trong development
function getApiBaseUrl(): string {
  // Nếu có env variable, dùng nó
  if (import.meta.env.VITE_API_BASE) {
    return import.meta.env.VITE_API_BASE;
  }
  
  // Development: tự động detect từ window.location
  if (import.meta.env.DEV) {
    const hostname = window.location.hostname;
    const protocol = window.location.protocol;
    // Nếu là localhost, dùng localhost:8080
    if (hostname === 'localhost' || hostname === '127.0.0.1') {
      return 'http://localhost:8080';
    }
    // Nếu là IP khác, dùng cùng IP với port 8080
    return `${protocol}//${hostname}:8080`;
  }
  
  // Production: dùng relative URL hoặc config
  return import.meta.env.VITE_API_BASE || '';
}

export const api = axios.create({
  baseURL: getApiBaseUrl()
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) {
    config.headers = config.headers || {}
    config.headers['Authorization'] = `Bearer ${token}`
  }
  return config
})


