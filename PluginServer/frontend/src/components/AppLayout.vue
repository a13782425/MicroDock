<template>
  <div class="min-h-screen bg-gray-50">
    <!-- 顶部导航 -->
    <nav class="bg-white shadow-sm border-b border-gray-200">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between items-center h-16">
          <!-- Logo和标题 -->
          <div class="flex items-center space-x-4">
            <div class="flex-shrink-0">
              <div class="w-8 h-8 bg-gradient-to-br from-primary-500 to-primary-600 rounded-lg flex items-center justify-center text-white font-bold">
                M
              </div>
            </div>
            <div>
              <h1 class="text-xl font-semibold text-gray-900">MicroDock</h1>
              <p class="text-xs text-gray-500">插件管理服务器</p>
            </div>
          </div>

          <!-- 导航菜单 -->
          <div class="hidden md:flex space-x-8">
            <router-link
              v-for="item in navigation"
              :key="item.name"
              :to="item.to"
              :class="[
                'inline-flex items-center px-1 pt-1 text-sm font-medium border-b-2 transition-colors duration-200',
                $route.path === item.to
                  ? 'border-primary-500 text-primary-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
              ]"
            >
              <component :is="item.icon" class="w-4 h-4 mr-2" />
              {{ item.name }}
            </router-link>
          </div>

          <!-- 移动端菜单按钮 -->
          <div class="md:hidden">
            <button
              @click="mobileMenuOpen = !mobileMenuOpen"
              class="inline-flex items-center justify-center p-2 rounded-md text-gray-400 hover:text-gray-500 hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-inset focus:ring-primary-500"
            >
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
              </svg>
            </button>
          </div>
        </div>
      </div>

      <!-- 移动端菜单 -->
      <div v-if="mobileMenuOpen" class="md:hidden border-t border-gray-200">
        <div class="px-2 pt-2 pb-3 space-y-1">
          <router-link
            v-for="item in navigation"
            :key="item.name"
            :to="item.to"
            :class="[
              'block pl-3 pr-4 py-2 text-base font-medium rounded-md transition-colors duration-200',
              $route.path === item.to
                ? 'bg-primary-50 border-primary-500 text-primary-700'
                : 'text-gray-600 hover:text-gray-900 hover:bg-gray-50'
            ]"
            @click="mobileMenuOpen = false"
          >
            <component :is="item.icon" class="w-4 h-4 mr-3 inline-block" />
            {{ item.name }}
          </router-link>
        </div>
      </div>
    </nav>

    <!-- 主内容区域 -->
    <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <slot />
    </main>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import {
  HomeIcon,
  CubeIcon,
  DocumentTextIcon,
  CloudArrowDownIcon,
  Cog6ToothIcon
} from '@heroicons/vue/24/outline'

const mobileMenuOpen = ref(false)

const navigation = [
  {
    name: '仪表板',
    to: '/',
    icon: HomeIcon
  },
  {
    name: '插件管理',
    to: '/plugins',
    icon: CubeIcon
  },
  {
    name: '版本管理',
    to: '/versions',
    icon: DocumentTextIcon
  },
  {
    name: '备份管理',
    to: '/backups',
    icon: CloudArrowDownIcon
  },
  {
    name: '设置',
    to: '/settings',
    icon: Cog6ToothIcon
  }
]
</script>