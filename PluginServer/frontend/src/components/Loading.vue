<template>
  <div
    :class="[
      'flex items-center justify-center',
      overlay ? 'fixed inset-0 bg-black bg-opacity-50 z-50' : 'w-full h-full'
    ]"
  >
    <div class="text-center">
      <!-- 默认加载动画 -->
      <div v-if="!custom" class="relative">
        <div class="w-12 h-12 border-4 border-gray-200 border-t-primary-600 rounded-full animate-spin"></div>
        <div v-if="text" class="mt-4 text-sm text-gray-600">{{ text }}</div>
      </div>

      <!-- 自定义加载内容 -->
      <div v-else>
        <slot name="default">
          <div class="loading-spinner"></div>
          <div v-if="text" class="mt-4 text-sm text-gray-600">{{ text }}</div>
        </slot>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  text: {
    type: String,
    default: ''
  },
  overlay: {
    type: Boolean,
    default: false
  },
  custom: {
    type: Boolean,
    default: false
  }
})
</script>

<style scoped>
.loading-spinner {
  @apply inline-block w-8 h-8 border-2 border-current border-t-transparent rounded-full animate-spin;
}
</style>