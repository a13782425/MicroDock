<template>
  <div class="space-y-6">
    <!-- 头部 -->
    <div class="flex justify-between items-center">
      <h2 class="text-3xl font-bold text-gray-900">用户备份</h2>
    </div>

    <!-- 用户密钥输入 -->
    <div class="bg-white rounded-lg shadow-md p-6">
      <div class="flex gap-4 items-end">
        <div class="flex-1">
          <label for="user-key" class="block text-sm font-medium text-gray-700 mb-1">
            用户密钥
          </label>
          <input 
            id="user-key"
            v-model="userKey"
            type="text" 
            class="block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            placeholder="输入您的用户密钥（字母、数字、下划线）"
          >
        </div>
        <button
          @click="loadBackups"
          :disabled="!isValidKey || loading"
          class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {{ loading ? '加载中...' : '查看备份' }}
        </button>
        <button
          @click="showUploadDialog = true"
          :disabled="!isValidKey"
          class="px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition disabled:opacity-50 disabled:cursor-not-allowed"
        >
          + 上传备份
        </button>
      </div>
    </div>

    <!-- 错误提示 -->
    <div v-if="error" class="bg-red-50 border border-red-200 rounded-lg p-4">
      <p class="text-red-800">{{ error }}</p>
    </div>

    <!-- 备份列表 -->
    <div v-if="backups.length > 0" class="bg-white rounded-lg shadow-md overflow-hidden">
      <table class="min-w-full divide-y divide-gray-200">
        <thead class="bg-gray-50">
          <tr>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">文件名</th>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">类型</th>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">大小</th>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">描述</th>
            <th scope="col" class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">上传时间</th>
            <th scope="col" class="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">操作</th>
          </tr>
        </thead>
        <tbody class="bg-white divide-y divide-gray-200">
          <tr v-for="backup in backups" :key="backup.id">
            <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
              {{ backup.file_name }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
              <span 
                class="px-2 py-1 text-xs rounded-full"
                :class="backup.backup_type === 'program' ? 'bg-purple-100 text-purple-800' : 'bg-blue-100 text-blue-800'"
              >
                {{ backup.backup_type === 'program' ? '主程序' : '插件' }}
              </span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
              {{ formatSize(backup.file_size) }}
            </td>
            <td class="px-6 py-4 text-sm text-gray-500 max-w-xs truncate">
              {{ backup.description || '-' }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
              {{ formatDate(backup.created_at) }}
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium space-x-2">
              <button 
                @click="downloadBackup(backup)"
                class="text-blue-600 hover:text-blue-900"
              >
                下载
              </button>
              <button 
                @click="deleteBackup(backup)"
                class="text-red-600 hover:text-red-900"
              >
                删除
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- 空状态 -->
    <div v-if="!loading && hasSearched && backups.length === 0" class="text-center py-12 bg-white rounded-lg shadow-md">
      <p class="text-gray-500 text-lg">暂无备份</p>
      <button
        @click="showUploadDialog = true"
        class="mt-4 px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700"
      >
        上传第一个备份
      </button>
    </div>

    <!-- 上传备份对话框 -->
    <BackupUploadDialog 
      v-model="showUploadDialog"
      :user-key="userKey"
      @uploaded="handleUploaded"
    />
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { backupService } from '../services/pluginService'
import BackupUploadDialog from '../components/BackupUploadDialog.vue'
import { useNotify } from '../utils/toast'

const userKey = ref('')
const backups = ref([])
const loading = ref(false)
const error = ref(null)
const hasSearched = ref(false)
const showUploadDialog = ref(false)
const notify = useNotify()

const isValidKey = computed(() => {
  return /^[a-zA-Z0-9_]+$/.test(userKey.value) && userKey.value.length <= 256
})

async function loadBackups() {
  if (!isValidKey.value) return
  
  loading.value = true
  error.value = null
  hasSearched.value = true
  
  try {
    const response = await backupService.getBackups(userKey.value)
    backups.value = response.backups || []
  } catch (e) {
    error.value = e.message || '加载备份列表失败'
    backups.value = []
  } finally {
    loading.value = false
  }
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

async function downloadBackup(backup) {
  try {
    const blob = await backupService.downloadBackup(userKey.value, backup.id)
    const url = window.URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = backup.file_name
    a.click()
    window.URL.revokeObjectURL(url)
    notify.success('下载已开始')
  } catch (e) {
    notify.error('下载失败: ' + e.message)
  }
}

async function deleteBackup(backup) {
  if (!confirm(`确定要删除备份 "${backup.file_name}" 吗？`)) return
  
  try {
    await backupService.deleteBackup(userKey.value, backup.id)
    notify.success('备份已删除')
    await loadBackups()
  } catch (e) {
    notify.error('删除失败: ' + e.message)
  }
}

function handleUploaded() {
  showUploadDialog.value = false
  loadBackups()
}
</script>

