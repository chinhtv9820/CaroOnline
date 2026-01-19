<template>
  <div class="space-y-2">
    <select
      :value="modelValue"
      @change="$emit('update:modelValue', Number(($event.target as HTMLSelectElement).value))"
      class="w-full px-3 py-2 border rounded"
    >
      <option value="60">1 phút</option>
      <option value="180">3 phút</option>
      <option value="300">5 phút</option>
      <option value="600">10 phút</option>
    </select>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  modelValue: number;
}>();

defineEmits<{
  'update:modelValue': [value: number];
}>();
</script>

