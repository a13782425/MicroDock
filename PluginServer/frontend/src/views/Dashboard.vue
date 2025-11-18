<template>
  <AppLayout>
    <div class="space-y-6">
      <!-- 页面标题 -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">仪表板</h1>
          <p class="text-gray-600 mt-1">插件管理系统概览</p>
        </div>
        <button
          @click="refreshData"
          :disabled="loading"
          class="btn btn-outline"
        >
          <div v-if="loading" class="w-4 h-4 mr-2 loading-spinner"></div>
          <svg v-else class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
          刷新数据
        </button>
      </div>

      <!-- 统计卡片 -->
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <div class="card">
          <div class="card-body">
            <div class="flex items-center">
              <div class="flex-shrink-0">
                <div class="w-8 h-8 bg-primary-100 rounded-lg flex items-center justify-center">
                  <CubeIcon class="w-5 h-5 text-primary-600" />
                </div>
              </div>
              <div class="ml-4">
                <p class="text-sm font-medium text-gray-600">总插件数</p>
                <p class="text-2xl font-bold text-gray-900">{{ statistics.total }}</p>
              </div>
            </div>
          </div>
        </div>

        <div class="card">
          <div class="card-body">
            <div class="flex items-center">
              <div class="flex-shrink-0">
                <div class="w-8 h-8 bg-success-100 rounded-lg flex items-center justify-center">
                  <CheckCircleIcon class="w-5 h-5 text-success-600" />
                </div>
              </div>
              <div class="ml-4">
                <p class="text-sm font-medium text-gray-600">正常插件</p>
                <p class="text-2xl font-bold text-gray-900">{{ statistics.active }}</p>
              </div>
            </div>
          </div>
        </div>

        <div class="card">
          <div class="card-body">
            <div class="flex items-center">
              <div class="flex-shrink-0">
                <div class="w-8 h-8 bg-warning-100 rounded-lg flex items-center justify-center">
                  <ExclamationTriangleIcon class="w-5 h-5 text-warning-600" />
                </div>
              </div>
              <div class="ml-4">
                <p class="text-sm font-medium text-gray-600">过时插件</p>
                <p class="text-2xl font-bold text-gray-900">{{ statistics.outdated }}</p>
              </div>
            </div>
          </div>
        </div>

        <div class="card">
          <div class="card-body">
            <div class="flex items-center">
              <div class="flex-shrink-0">
                <div class="w-8 h-8 bg-info-100 rounded-lg flex items-center justify-center">
                  <ServerIcon class="w-5 h-5 text-info-600" />
                </div>
              </div>
              <div class="ml-4">
                <p class="text-sm font-medium text-gray-600">总大小</p>
                <p class="text-2xl font-bold text-gray-900">{{ formatFileSize(statistics.total_size) }}</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 快速操作 -->
      <div class="card">
        <div class="card-header">
          <h3 class="text-lg font-semibold text-gray-900">快速操作</h3>
        </div>
        <div class="card-body">
          <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
            <router-link
              to="/plugins?action=upload"
              class="flex items-center p-4 bg-primary-50 rounded-lg hover:bg-primary-100 transition-colors duration-200"
            >
              <div class="flex-shrink-0">
                <CloudArrowUpIcon class="w-8 h-8 text-primary-600" />
              </div>
              <div class="ml-4">
                <h4 class="text-sm font-medium text-primary-900">上传插件</h4>
                <p class="text-sm text-primary-700">添加新的插件到系统</p>
              </div>
            </router-link>

            <router-link
              to="/plugins?action=scan"
              class="flex items-center p-4 bg-success-50 rounded-lg hover:bg-success-100 transition-colors duration-200"
            >
              <div class="flex-shrink-0">
                <MagnifyingGlassIcon class="w-8 h-8 text-success-600" />
              </div>
              <div class="ml-4">
                <h4 class="text-sm font-medium text-success-900">扫描插件</h4>
                <p class="text-sm text-success-700">发现并导入现有插件</p>
              </div>
            </router-link>

            <router-link
              to="/backups?action=create"
              class="flex items-center p-4 bg-warning-50 rounded-lg hover:bg-warning-100 transition-colors duration-200"
            >
              <div class="flex-shrink-0">
                <CloudArrowDownIcon class="w-8 h-8 text-warning-600" />
              </div>
              <div class="ml-4">
                <h4 class="text-sm font-medium text-warning-900">创建备份</h4>
                <p class="text-sm text-warning-700">备份系统或插件状态</p>
              </div>
            </router-link>
          </div>
        </div>
      </div>

      <!-- 插件类型分布 -->
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div class="card">
          <div class="card-header">
            <h3 class="text-lg font-semibold text-gray-900">插件类型分布</h3>
          </div>
          <div class="card-body">
            <div v-if="statistics.by_type.length > 0" class="space-y-3">
              <div
                v-for="type in statistics.by_type"
                :key="type.type"
                class="flex items-center justify-between"
              >
                <div class="flex items-center space-x-3">
                  <div class="w-3 h-3 bg-primary-500 rounded-full"></div>
                  <span class="text-sm font-medium text-gray-900">
                    {{ getTypeText(type.type) }}
                  </span>
                </div>
                <div class="flex items-center space-x-3">
                  <span class="text-sm text-gray-600">{{ type.count }} 个</span>
                  <div class="w-24 bg-gray-200 rounded-full h-2">
                    <div
                      class="bg-primary-500 h-2 rounded-full transition-all duration-500"
                      :style="`width: ${(type.count / statistics.total) * 100}%`"
                    ></div>
                  </div>
                </div>
              </div>
            </div>
            <div v-else class="text-center py-8 text-gray-500">
              暂无插件数据
            </div>
          </div>
        </div>

        <!-- 最近活动 -->
        <div class="card">
          <div class="card-header">
            <h3 class="text-lg font-semibold text-gray-900">最近活动</h3>
          </div>
          <div class="card-body">
            <div class="space-y-4">
              <div class="flex items-center space-x-3">
                <div class="w-2 h-2 bg-success-500 rounded-full"></div>
                <div class="flex-1">
                  <p class="text-sm text-gray-900">系统运行正常</p>
                  <p class="text-xs text-gray-500">{{ new Date().toLocaleString() }}</p>
                </div>
              </div>
              <div class="flex items-center space-x-3">
                <div class="w-2 h-2 bg-primary-500 rounded-full"></div>
                <div class="flex-1">
                  <p class="text-sm text-gray-900">插件扫描完成</p>
                  <p class="text-xs text-gray-500">{{ new Date().toLocaleString() }}</p>
                </div>
              </div>
              <div class="flex items-center space-x-3">
                <div class="w-2 h-2 bg-warning-500 rounded-full"></div>
                <div class="flex-1">
                  <p class="text-sm text-gray-900">发现 {{ statistics.outdated }} 个过时插件</p>
                  <p class="text-xs text-gray-500">{{ new Date().toLocaleString() }}</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 最近插件 -->
      <div class="card">
        <div class="card-header">
          <div class="flex items-center justify-between">
            <h3 class="text-lg font-semibold text-gray-900">最近插件</h3>
            <router-link
              to="/plugins"
              class="text-sm text-primary-600 hover:text-primary-500"
            >
              查看全部
            </router-link>
          </div>
        </div>
        <div class="card-body">
          <div v-if="recentPlugins.length > 0" class="space-y-3">
            <div
              v-for="plugin in recentPlugins"
              :key="plugin.id"
              class="flex items-center justify-between p-3 bg-gray-50 rounded-lg"
            >
              <div class="flex items-center space-x-3">
                <div class="w-8 h-8 bg-primary-100 rounded-lg flex items-center justify-center text-primary-600 font-bold text-sm">
                  {{ plugin.name.charAt(0).toUpperCase() }}
                </div>
                <div>
                  <p class="text-sm font-medium text-gray-900">{{ plugin.display_name || plugin.name }}</p>
                  <p class="text-xs text-gray-500">{{ plugin.version }} • {{ formatDate(plugin.updated_at) }}</p>
                </div>
              </div>
              <div class="flex items-center space-x-2">
                <span :class="[
                  'badge',
                  plugin.is_active ? 'badge-success' : 'badge-gray'
                ]">
                  {{ plugin.is_active ? '正常' : '已禁用' }}
                </span>
                <router-link
                  :to="`/plugins/${plugin.id}`"
                  class="text-primary-600 hover:text-primary-500"
                >
                  <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                    <path d="M10 12a2 2 0 100-4 2 2 0 000 4z" />
                    <path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.064 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd" />
                  </svg>
                </router-link>
              </div>
            </div>
          </div>
          <div v-else class="text-center py-8 text-gray-500">
            暂无插件数据
          </div>
        </div>
      </div>
    </div>
  </AppLayout>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import AppLayout from '@/components/AppLayout.vue'
import { usePluginStore } from '@/stores/plugins'
import {
  CubeIcon,
  CheckCircleIcon,
  ExclamationTriangleIcon,
  ServerIcon,
  CloudArrowUpIcon,
  MagnifyingGlassIcon,
  CloudArrowDownIcon
} from '@heroicons/vue/24/outline'

const pluginStore = usePluginStore()
const { loading } = storeToRefs(pluginStore)

const statistics = ref({
  total: 0,
  active: 0,
  inactive: 0,
  outdated: 0,
  by_type: [],
  total_size: 0
})

const recentPlugins = ref([])

async function refreshData() {
  try {
    await pluginStore.fetchPlugins({ limit: 20 })
    statistics.value = pluginStore.statistics
    recentPlugins.value = pluginStore.plugins.slice(0, 5)
  } catch (error) {
    console.error('刷新数据失败:', error)
  }
}

function formatFileSize(bytes) {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i]
}

function getTypeText(type) {
  const typeMap = {
    'storage': '存储器',
    'service': '服务',
    'tab': '标签页'
  }
  return typeMap[type] || '未知'
}

function formatDate(dateString) {
  if (!dateString) return '未知'
  return new Date(dateString).toLocaleDateString()
}

onMounted(() => {
  refreshData()
})
</script>