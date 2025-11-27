<template>
  <!-- 登录页面不显示导航栏 -->
  <div v-if="$route.path === '/login'" class="min-h-screen">
    <router-view />
  </div>
  
  <!-- 其他页面显示完整布局 -->
  <div v-else class="min-h-screen bg-gray-100">
    <nav class="bg-white shadow-lg">
      <div class="max-w-7xl mx-auto px-4">
        <div class="flex justify-between h-16">
          <div class="flex items-center">
            <h1 class="text-2xl font-bold text-gray-800">MicroDock 插件管理</h1>
          </div>
          <div class="flex space-x-4 items-center">
            <router-link 
              to="/" 
              class="px-3 py-2 rounded-md text-sm font-medium hover:bg-gray-200"
              :class="$route.path === '/' ? 'bg-gray-200' : ''"
            >
              插件列表
            </router-link>
            <router-link 
              to="/backups" 
              class="px-3 py-2 rounded-md text-sm font-medium hover:bg-gray-200"
              :class="$route.path === '/backups' ? 'bg-gray-200' : ''"
            >
              用户备份
            </router-link>
            
            <!-- 分隔线 -->
            <div class="h-6 w-px bg-gray-300"></div>
            
            <!-- 登录状态 -->
            <div v-if="authStore.isLoggedIn" class="flex items-center space-x-3">
              <span class="text-sm text-gray-600">
                <span class="inline-flex items-center px-2 py-1 bg-green-100 text-green-800 rounded-full text-xs">
                  <svg class="w-3 h-3 mr-1" fill="currentColor" viewBox="0 0 20 20">
                    <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
                  </svg>
                  {{ authStore.username }}
                </span>
              </span>
              <button 
                @click="handleLogout"
                class="px-3 py-1.5 text-sm text-red-600 hover:bg-red-50 rounded-md transition"
              >
                登出
              </button>
            </div>
            <router-link 
              v-else
              to="/login" 
              class="px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-md hover:bg-blue-700 transition"
            >
              管理员登录
            </router-link>
          </div>
        </div>
      </div>
    </nav>
    
    <main class="max-w-7xl mx-auto py-6 px-4">
      <router-view />
    </main>
  </div>
</template>

<script setup>
import { useAuthStore } from './stores/auth'
import { useRouter } from 'vue-router'
import { useNotify } from './utils/toast'

const authStore = useAuthStore()
const router = useRouter()
const notify = useNotify()

function handleLogout() {
  authStore.logout()
  notify.success('已登出')
  router.push('/')
}
</script>
