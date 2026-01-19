<template>
  <div class="space-y-2">
    <select
      :value="modelValue"
      @change="$emit('update:modelValue', ($event.target as HTMLSelectElement).value)"
      class="w-full px-3 py-2 border rounded"
    >
      <option value="default">Mặc định</option>
      <option value="dark">Tối</option>
      <option value="blue">Xanh dương</option>
    </select>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  modelValue: string;
}>();

defineEmits<{
  'update:modelValue': [value: string];
}>();
</script>

