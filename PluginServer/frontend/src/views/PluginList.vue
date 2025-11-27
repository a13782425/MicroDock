<template>
  <div class="space-y-6">
    <!-- 头部 -->
    <div class="flex justify-between items-center">
      <h2 class="text-3xl font-bold text-gray-900">插件列表</h2>
      <button
        @click="showUploadDialog = true"
        class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition"
      >
        + 上传插件
      </button>
    </div>

    <!-- 加载状态 -->
    <div v-if="pluginStore.loading" class="text-center py-12">
      <div class="inline-block animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      <p class="mt-4 text-gray-600">加载中...</p>
    </div>

    <!-- 错误提示 -->
    <div v-if="pluginStore.error" class="bg-red-50 border border-red-200 rounded-lg p-4">
      <p class="text-red-800">{{ pluginStore.error }}</p>
    </div>

    <!-- 插件网格 -->
    <div v-if="!pluginStore.loading && pluginStore.plugins.length > 0" 
         class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <div
        v-for="plugin in pluginStore.plugins"
        :key="plugin.id"
        class="bg-white rounded-lg shadow-md p-6 hover:shadow-lg transition"
      >
        <div class="flex justify-between items-start mb-4">
          <div class="flex-1">
            <h3 class="text-xl font-semibold text-gray-900">{{ plugin.display_name }}</h3>
            <p class="text-sm text-gray-500">{{ plugin.name }}</p>
          </div>
          <span class="px-2 py-1 text-xs rounded-full"
                :class="plugin.is_enabled ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'">
            {{ plugin.is_enabled ? '已启用' : '已禁用' }}
          </span>
        </div>

        <p class="text-gray-600 text-sm mb-4 line-clamp-2">{{ plugin.description }}</p>

        <div class="space-y-2 text-sm text-gray-600 mb-4">
          <div class="flex justify-between">
            <span>版本:</span>
            <span class="font-mono">{{ plugin.version_number }}</span>
          </div>
          <div class="flex justify-between">
            <span>作者:</span>
            <span>{{ plugin.author || '未知' }}</span>
          </div>
          <div class="flex justify-between">
            <span>DLL:</span>
            <span class="font-mono text-xs">{{ plugin.main_dll }}</span>
          </div>
        </div>

        <div class="flex gap-2">
          <button
            @click="togglePlugin(plugin)"
            class="flex-1 px-3 py-2 rounded text-sm transition"
            :class="plugin.is_enabled 
              ? 'bg-gray-200 hover:bg-gray-300' 
              : 'bg-green-200 hover:bg-green-300'"
          >
            {{ plugin.is_enabled ? '禁用' : '启用' }}
          </button>
          <button
            @click="viewVersions(plugin)"
            class="flex-1 px-3 py-2 bg-blue-100 hover:bg-blue-200 rounded text-sm transition"
          >
            版本
          </button>
          <button
            @click="downloadPlugin(plugin)"
            class="px-3 py-2 bg-purple-100 hover:bg-purple-200 rounded text-sm transition"
          >
            下载
          </button>
          <button
            @click="deletePlugin(plugin)"
            class="px-3 py-2 bg-red-100 hover:bg-red-200 rounded text-sm transition"
          >
            删除
          </button>
        </div>
      </div>
    </div>

    <!-- 空状态 -->
    <div v-if="!pluginStore.loading && pluginStore.plugins.length === 0" 
         class="text-center py-12">
      <p class="text-gray-500 text-lg">暂无插件</p>
      <button
        @click="showUploadDialog = true"
        class="mt-4 px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
      >
        上传第一个插件
      </button>
    </div>

    <!-- 上传对话框 -->
    <UploadDialog 
      v-model="showUploadDialog"
      @uploaded="handleUploaded"
    />

    <!-- 版本对话框 -->
    <VersionDialog
      v-model="showVersionDialog"
      :plugin="selectedPlugin"
    />
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { usePluginStore } from '../stores/plugin'
import UploadDialog from '../components/UploadDialog.vue'
import VersionDialog from '../components/VersionDialog.vue'
import { pluginService } from '../services/pluginService'
import { useNotify } from '../utils/toast'

const pluginStore = usePluginStore()
const showUploadDialog = ref(false)
const showVersionDialog = ref(false)
const selectedPlugin = ref(null)
const notify = useNotify()

onMounted(() => {
  pluginStore.fetchPlugins()
})

async function togglePlugin(plugin) {
  try {
    await pluginStore.togglePlugin(plugin.id, !plugin.is_enabled)
    notify.success(plugin.is_enabled ? '插件已禁用' : '插件已启用')
  } catch (error) {
    notify.error('操作失败: ' + error.message)
  }
}

function viewVersions(plugin) {
  selectedPlugin.value = plugin
  showVersionDialog.value = true
}

async function downloadPlugin(plugin) {
  try {
    const blob = await pluginService.downloadPlugin(plugin.id)
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${plugin.name}-${plugin.version_number}.zip`
    a.click()
    window.URL.revokeObjectURL(url)
    notify.success('下载已开始')
  } catch (error) {
    notify.error('下载失败: ' + error.message)
  }
}

async function deletePlugin(plugin) {
  if (!confirm(`确定要删除插件 "${plugin.display_name}" 吗？`)) {
    return
  }
  
  try {
    await pluginStore.deletePlugin(plugin.id)
    notify.success('插件已删除')
  } catch (error) {
    notify.error('删除失败: ' + error.message)
  }
}

function handleUploaded() {
  showUploadDialog.value = false
  pluginStore.fetchPlugins()
}
</script>
