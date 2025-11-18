<template>
  <AppLayout>
    <div class="space-y-6">
      <!-- 页面标题 -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">插件管理</h1>
          <p class="text-gray-600 mt-1">管理和配置系统插件</p>
        </div>
        <div>
          <button
            @click="showUploadModal = true"
            class="btn btn-primary"
          >
            <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4m8 0H8" />
            </svg>
            上传插件
          </button>
          <button
            @click="refreshPlugins"
            :disabled="loading"
            class="btn btn-outline"
          >
            <div v-if="loading" class="w-4 h-4 mr-2 loading-spinner"></div>
            刷新
          </button>
        </div>
      </div>

      <!-- 插件列表 -->
      <div v-if="pluginStore.plugins.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <PluginCard
          v-for="plugin in pluginStore.plugins"
          :key="plugin.id"
          :plugin="plugin"
          @view-details="viewPluginDetails"
          @deleted="onPluginDeleted"
        />
      </div>

      <!-- 空状态 -->
      <div v-else class="text-center py-12">
        <div class="w-16 h-16 bg-gray-200 rounded-full flex items-center justify-center mx-auto mb-4">
          <svg class="w-8 h-8 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 012 2h6a2 2 0 012-2V5a2 2 0 012-2z" />
          </svg>
        </div>
        <h3 class="text-lg font-medium text-gray-900 mb-2">暂无插件</h3>
        <p class="text-gray-500">还没有安装任何插件，点击上方按钮开始添加</p>
      </div>

      <!-- 上传模态框 -->
      <PluginUpload
        v-model:show="showUploadModal"
        @uploaded="onPluginUploaded"
      />
    </div>
  </AppLayout>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { usePluginStore } from '@/stores/plugins'
import AppLayout from '@/components/AppLayout.vue'
import PluginCard from '@/components/PluginCard.vue'
import PluginUpload from '@/components/PluginUpload.vue'

const pluginStore = usePluginStore()
const loading = ref(false)
const showUploadModal = ref(false)

async function refreshPlugins() {
  try {
    loading.value = true
    await pluginStore.fetchPlugins()
  } catch (error) {
    console.error('获取插件列表失败:', error)
  } finally {
    loading.value = false
  }
}

function viewPluginDetails(plugin) {
  // 跳转到插件详情页面
  console.log('查看插件详情:', plugin)
}

function onPluginDeleted(pluginId) {
  // 从列表中移除删除的插件
  pluginStore.plugins = pluginStore.plugins.filter(p => p.id !== pluginId)
}

function onPluginUploaded() {
  // 重新获取插件列表
  refreshPlugins()
}

onMounted(() => {
  refreshPlugins()
})
</script>