<template>
  <div id="app" class="min-h-screen bg-gray-50">
    <!-- 通知组件 -->
    <Notifications />

    <!-- 主应用内容 -->
    <router-view />
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import Notifications from '@/components/Notifications.vue'

onMounted(() => {
  // 移除加载屏幕
  setTimeout(() => {
    document.body.classList.add('app-ready')
  }, 500)
})
</script>

<style>
#app {
  font-family: 'Inter', system-ui, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}
</style>