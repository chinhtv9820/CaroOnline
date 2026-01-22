import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '@/stores/auth';

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'Menu',
      component: () => import('@/pages/MenuPage.vue')
    },
    {
      path: '/login',
      name: 'Login',
      component: () => import('@/pages/LoginPage.vue')
    },
    {
      path: '/register',
      name: 'Register',
      component: () => import('@/pages/RegisterPage.vue')
    },
    {
      path: '/pve',
      name: 'PVE',
      component: () => import('@/pages/PVEPage.vue')
    },
    {
      path: '/pvp',
      name: 'PVP',
      component: () => import('@/pages/PVPPage.vue')
    },
    {
      path: '/ranking',
      name: 'Ranking',
      component: () => import('@/pages/RankingPage.vue')
    },
    {
      path: '/history',
      name: 'History',
      component: () => import('@/pages/HistoryPage.vue')
    },
    {
      path: '/options',
      name: 'Options',
      component: () => import('@/pages/OptionsPage.vue')
    }
  ]
});

router.beforeEach((to, from, next) => {
  const authStore = useAuthStore();
  if (to.path === '/pvp' && !authStore.isAuthenticated) {
    // PVP requires authentication
    next();
  } else {
    next();
  }
});

export default router;

