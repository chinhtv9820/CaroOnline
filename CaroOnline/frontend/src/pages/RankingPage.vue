<template>
  <div class="max-w-4xl mx-auto">
    <h1 class="text-3xl font-bold mb-6">Bảng xếp hạng</h1>

    <div class="mb-4">
      <div class="flex space-x-2 border-b">
        <button
          v-for="period in periods"
          :key="period.value"
          @click="selectedPeriod = period.value"
          :class="[
            'px-4 py-2 font-medium border-b-2 transition',
            selectedPeriod === period.value
              ? 'border-blue-600 text-blue-600'
              : 'border-transparent text-gray-600 hover:text-gray-800'
          ]"
        >
          {{ period.label }}
        </button>
      </div>
    </div>

    <div v-if="loading" class="text-center py-8">
      <Spinner />
    </div>

    <div v-else class="bg-white rounded-lg shadow overflow-hidden">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Hạng</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Người chơi</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Điểm</th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-gray-200">
          <tr v-for="(rank, index) in rankings" :key="rank.id" class="hover:bg-gray-50">
            <td class="px-6 py-4 whitespace-nowrap text-sm font-medium">{{ index + 1 }}</td>
            <td class="px-6 py-4 whitespace-nowrap text-sm">{{ rank.username }}</td>
            <td class="px-6 py-4 whitespace-nowrap text-sm">{{ rank.score }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
// Trang bảng xếp hạng: gọi API /api/rankings/{period}, cho phép chuyển tab ngày/tuần/tháng/năm và hiển thị bảng điểm.
import { ref, watch } from 'vue';
import { api } from '@/services/api';
import Spinner from '@/components/Spinner.vue';

const periods = [
  { value: 'day', label: 'Ngày' },
  { value: 'week', label: 'Tuần' },
  { value: 'month', label: 'Tháng' },
  { value: 'year', label: 'Năm' }
];

const selectedPeriod = ref('day');
const rankings = ref<Array<{ id: number; username: string; score: number }>>([]);
const loading = ref(false);

async function fetchRankings() {
  loading.value = true;
  try {
    const response = await api.get(`/api/rankings/${selectedPeriod.value}`);
    rankings.value = response.data;
  } catch (error) {
    console.error('Failed to fetch rankings:', error);
    rankings.value = [];
  } finally {
    loading.value = false;
  }
}

watch(selectedPeriod, fetchRankings);
fetchRankings();
</script>

