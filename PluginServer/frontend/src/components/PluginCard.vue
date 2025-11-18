<template>
  <div class="card hover:shadow-medium transition-all duration-300 transform hover:-translate-y-1">
    <!-- 卡片头部 -->
    <div class="card-header">
      <div class="flex items-center justify-between">
        <div class="flex items-center space-x-3">
          <div class="flex-shrink-0">
            <div class="w-10 h-10 bg-gradient-to-br from-primary-500 to-primary-600 rounded-lg flex items-center justify-center text-white font-bold">
              {{ plugin.name.charAt(0).toUpperCase() }}
            </div>
          </div>
          <div>
            <h3 class="text-lg font-semibold text-gray-900 truncate">
              {{ plugin.display_name || plugin.name }}
            </h3>
            <div class="flex items-center space-x-2">
              <span :class="[
                'badge',
                getStatusClass(plugin)
              ]">
                {{ getStatusText(plugin) }}
              </span>
              <span class="badge badge-gray">
                {{ getTypeText(plugin.plugin_type) }}
              </span>
            </div>
          </div>
        </div>
        <div class="flex items-center space-x-2">
          <button
            @click="togglePluginStatus"
            :class="[
              'btn-sm',
              plugin.is_active ? 'btn-warning' : 'btn-success'
            ]"
            :disabled="loading"
          >
            <div v-if="loading" class="w-4 h-4 mr-1 loading-spinner"></div>
            {{ plugin.is_active ? '禁用' : '启用' }}
          </button>
          <div class="relative">
            <button
              @click="showDropdown = !showDropdown"
              class="btn-sm btn-outline"
            >
              <svg class="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                <path d="M10 6a2 2 0 110-4 2 2 0 010 4zM10 12a2 2 0 110-4 2 2 0 010 4zM10 18a2 2 0 110-4 2 2 0 010 4z" />
              </svg>
            </button>
            <div v-if="showDropdown" class="dropdown">
              <button
                @click="viewDetails"
                class="dropdown-item"
              >
                <svg class="w-4 h-4 mr-2" fill="currentColor" viewBox="0 0 20 20">
                  <path d="M10 12a2 2 0 100-4 2 2 0 000 4z" />
                  <path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.064 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd" />
                </svg>
                查看详情
              </button>
              <button
                @click="downloadPlugin"
                class="dropdown-item"
              >
                <svg class="w-4 h-4 mr-2" fill="currentColor" viewBox="0 0 20 20">
                  <path fill-rule="evenodd" d="M3 17a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm3.293-7.707a1 1 0 011.414 0L9 10.586V3a1 1 0 112 0v7.586l1.293-1.293a1 1 0 111.414 1.414l-3 3a1 1 0 01-1.414 0l-3-3a1 1 0 010-1.414z" clip-rule="evenodd" />
                </svg>
                下载插件
              </button>
              <button
                @click="confirmDelete"
                class="dropdown-item text-danger-600 hover:bg-danger-50"
              >
                <svg class="w-4 h-4 mr-2" fill="currentColor" viewBox="0 0 20 20">
                  <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" />
                </svg>
                删除插件
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- 卡片内容 -->
    <div class="card-body">
      <div class="space-y-3">
        <!-- 描述 -->
        <p v-if="plugin.description" class="text-sm text-gray-600 line-clamp-2">
          {{ plugin.description }}
        </p>
        <p v-else class="text-sm text-gray-400 italic">
          暂无描述
        </p>

        <!-- 基本信息 -->
        <div class="grid grid-cols-2 gap-3 text-xs text-gray-500">
          <div>
            <span class="block font-medium text-gray-700">版本</span>
            {{ plugin.version }}
          </div>
          <div>
            <span class="block font-medium text-gray-700">作者</span>
            {{ plugin.author || '未知' }}
          </div>
          <div>
            <span class="block font-medium text-gray-700">大小</span>
            {{ formatFileSize(plugin.file_size) }}
          </div>
          <div>
            <span class="block font-medium text-gray-700">更新时间</span>
            {{ formatDate(plugin.updated_at) }}
          </div>
        </div>

        <!-- 依赖项 -->
        <div v-if="dependencies.length > 0" class="flex flex-wrap gap-1">
          <span
            v-for="dep in dependencies"
            :key="dep"
            class="inline-flex items-center px-2 py-1 rounded text-xs bg-gray-100 text-gray-700"
          >
            <svg class="w-3 h-3 mr-1" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M12.586 4.586a2 2 0 112.828 2.828l-3 3a2 2 0 01-2.828 0 1 1 0 00-1.414 1.414 4 4 0 005.656 0l3-3a4 4 0 00-5.656-5.656l-1.5 1.5a1 1 0 101.414 1.414l1.5-1.5zm-5 5a2 2 0 012.828 0 1 1 0-1.414-1.414 4 4 0 00-5.656 5.656l3 3a4 4 0 005.656-5.656l-1.5-1.5a1 1 0 10-1.414 1.414l1.5 1.5a2 2 0 11-2.828 2.828l-3-3z" clip-rule="evenodd" />
            </svg>
            {{ dep }}
          </span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useNotificationStore } from '@/stores/notification'
import { usePluginStore } from '@/stores/plugins'
import dayjs from 'dayjs'

const props = defineProps({
  plugin: {
    type: Object,
    required: true
  }
})

const emit = defineEmits(['view-details', 'deleted'])

const notificationStore = useNotificationStore()
const pluginStore = usePluginStore()

const showDropdown = ref(false)
const loading = ref(false)

const dependencies = computed(() => {
  try {
    return props.plugin.dependencies ? JSON.parse(props.plugin.dependencies) : []
  } catch {
    return []
  }
})

function getStatusClass(plugin) {
  if (plugin.is_outdated) return 'badge-warning'
  if (plugin.is_active) return 'badge-success'
  return 'badge-gray'
}

function getStatusText(plugin) {
  if (plugin.is_outdated) return '已过时'
  if (plugin.is_active) return '正常'
  return '已禁用'
}

function getTypeText(type) {
  const typeMap = {
    'storage': '存储器',
    'service': '服务',
    'tab': '标签页'
  }
  return typeMap[type] || '未知'
}

function formatFileSize(bytes) {
  if (!bytes) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i]
}

function formatDate(dateString) {
  if (!dateString) return '未知'
  return dayjs(dateString).format('YYYY-MM-DD HH:mm')
}

async function togglePluginStatus() {
  try {
    loading.value = true
    await pluginStore.togglePlugin(props.plugin.id, !props.plugin.is_active)
    notificationStore.showSuccess(
      `插件已${props.plugin.is_active ? '禁用' : '启用'}`
    )
  } catch (error) {
    // 错误已在store中处理
  } finally {
    loading.value = false
    showDropdown.value = false
  }
}

async function downloadPlugin() {
  try {
    await pluginStore.downloadPlugin(props.plugin.id)
    notificationStore.showSuccess('插件下载开始')
  } catch (error) {
    // 错误已在store中处理
  } finally {
    showDropdown.value = false
  }
}

function viewDetails() {
  emit('view-details', props.plugin)
  showDropdown.value = false
}

async function confirmDelete() {
  if (confirm(`确定要删除插件 "${props.plugin.name}" 吗？\n删除前会自动创建备份。`)) {
    try {
      loading.value = true
      await pluginStore.deletePlugin(props.plugin.id)
      notificationStore.showSuccess('插件删除成功')
      emit('deleted', props.plugin.id)
    } catch (error) {
      // 错误已在store中处理
    } finally {
      loading.value = false
      showDropdown.value = false
    }
  }
}

// 点击外部关闭下拉菜单
document.addEventListener('click', (e) => {
  if (showDropdown.value && !e.target.closest('.relative')) {
    showDropdown.value = false
  }
})
</script>