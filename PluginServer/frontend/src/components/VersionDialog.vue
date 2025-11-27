<template>
  <div v-if="modelValue" class="fixed inset-0 z-50 overflow-y-auto" aria-labelledby="modal-title" role="dialog" aria-modal="true">
    <div class="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
      <div class="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" aria-hidden="true" @click="close"></div>

      <span class="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">&#8203;</span>

      <div class="inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-4xl sm:w-full">
        <div class="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
          <div class="sm:flex sm:items-start">
            <div class="mt-3 text-center sm:mt-0 sm:ml-4 sm:text-left w-full">
              <h3 class="text-lg leading-6 font-medium text-gray-900 mb-4" id="modal-title">
                版本历史 - {{ plugin?.display_name }}
              </h3>
              
              <div class="overflow-x-auto">
                <table class="min-w-full divide-y divide-gray-200">
                  <thead class="bg-gray-50">
                    <tr>
                      <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">版本</th>
                      <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">文件大小</th>
                      <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">上传时间</th>
                      <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">下载次数</th>
                      <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">状态</th>
                      <th scope="col" class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">操作</th>
                    </tr>
                  </thead>
                  <tbody class="bg-white divide-y divide-gray-200">
                    <tr v-for="version in versions" :key="version.version">
                      <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                        {{ version.version }}
                        <span v-if="version.version === plugin?.current_version" class="ml-2 px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-green-100 text-green-800">
                          当前
                        </span>
                      </td>
                      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {{ formatSize(version.file_size) }}
                      </td>
                      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {{ formatDate(version.created_at) }}
                      </td>
                      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        {{ version.download_count }}
                      </td>
                      <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                        <span v-if="version.is_deprecated" class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-yellow-100 text-yellow-800">
                          已过时
                        </span>
                        <span v-else class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-blue-100 text-blue-800">
                          正常
                        </span>
                      </td>
                      <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium space-x-2">
                        <button 
                          @click="downloadVersion(version)"
                          class="text-blue-600 hover:text-blue-900"
                        >
                          下载
                        </button>
                        <button 
                          v-if="!version.is_deprecated"
                          @click="deprecateVersion(version)"
                          class="text-yellow-600 hover:text-yellow-900"
                        >
                          标记过时
                        </button>
                      </td>
                    </tr>
                    <tr v-if="versions.length === 0">
                      <td colspan="6" class="px-6 py-4 text-center text-sm text-gray-500">
                        暂无版本信息
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
        <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
          <button 
            type="button" 
            class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm"
            @click="close"
          >
            关闭
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import { pluginService } from '../services/pluginService'

const props = defineProps({
  modelValue: Boolean,
  plugin: Object
})

const emit = defineEmits(['update:modelValue'])
const versions = ref([])

watch(() => props.modelValue, async (newVal) => {
  if (newVal && props.plugin) {
    await loadVersions()
  }
})

async function loadVersions() {
  try {
    // 使用插件名查询版本列表
    versions.value = await pluginService.getPluginVersions(props.plugin.name)
  } catch (error) {
    console.error('加载版本失败:', error)
  }
}

function close() {
  emit('update:modelValue', false)
}

function formatSize(bytes) {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

function formatDate(dateString) {
  return new Date(dateString).toLocaleString()
}

async function downloadVersion(version) {
  try {
    // 使用插件名 + 版本号下载
    const blob = await pluginService.downloadVersion(props.plugin.name, version.version)
    
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `${props.plugin.name}@${version.version}.zip`
    a.click()
    window.URL.revokeObjectURL(url)
  } catch (error) {
    alert('下载失败: ' + error.message)
  }
}

async function deprecateVersion(version) {
  if (!confirm(`确定要将版本 ${version.version} 标记为过时吗？`)) return
  
  try {
    // 使用插件名 + 版本号标记为过时
    await pluginService.deprecateVersion(props.plugin.name, version.version)
    await loadVersions() // 刷新列表
  } catch (error) {
    alert('操作失败: ' + error.message)
  }
}
</script>
