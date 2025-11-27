<template>
  <div v-if="modelValue" class="fixed inset-0 z-50 overflow-y-auto" aria-labelledby="modal-title" role="dialog" aria-modal="true">
    <div class="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
      <!-- 背景遮罩 -->
      <div class="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" aria-hidden="true" @click="close"></div>

      <span class="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">&#8203;</span>

      <div class="inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
        <div class="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
          <div class="sm:flex sm:items-start">
            <div class="mt-3 text-center sm:mt-0 sm:ml-4 sm:text-left w-full">
              <h3 class="text-lg leading-6 font-medium text-gray-900" id="modal-title">
                上传备份
              </h3>
              <div class="mt-4 space-y-4">
                <!-- 备份类型选择 -->
                <div>
                  <label class="block text-sm font-medium text-gray-700 mb-2">
                    备份类型 <span class="text-red-500">*</span>
                  </label>
                  <div class="flex gap-4">
                    <label class="flex items-center">
                      <input 
                        type="radio" 
                        v-model="backupType" 
                        value="program"
                        class="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300"
                      >
                      <span class="ml-2 text-sm text-gray-700">主程序</span>
                    </label>
                    <label class="flex items-center">
                      <input 
                        type="radio" 
                        v-model="backupType" 
                        value="plugin"
                        class="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300"
                      >
                      <span class="ml-2 text-sm text-gray-700">插件</span>
                    </label>
                  </div>
                </div>

                <!-- 插件名称（仅 plugin 类型显示） -->
                <div v-if="backupType === 'plugin'">
                  <label for="plugin-name" class="block text-sm font-medium text-gray-700">
                    插件名称 <span class="text-red-500">*</span>
                  </label>
                  <input 
                    id="plugin-name"
                    v-model="pluginName"
                    type="text" 
                    class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                    placeholder="例如：com.example.myplugin"
                  >
                  <p class="mt-1 text-xs text-gray-500">请输入要备份的插件名称</p>
                </div>

                <!-- 备份描述 -->
                <div>
                  <label for="backup-desc" class="block text-sm font-medium text-gray-700">
                    备份描述（可选）
                  </label>
                  <input 
                    id="backup-desc"
                    v-model="description"
                    type="text" 
                    class="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                    placeholder="例如：版本 1.0.0 备份"
                  >
                </div>

                <!-- 文件选择区域 -->
                <div 
                  class="flex justify-center px-6 pt-5 pb-6 border-2 border-gray-300 border-dashed rounded-md hover:border-blue-500 transition cursor-pointer"
                  @click="$refs.fileInput.click()"
                  @drop.prevent="handleDrop"
                  @dragover.prevent
                >
                  <div class="space-y-1 text-center">
                    <svg class="mx-auto h-12 w-12 text-gray-400" stroke="currentColor" fill="none" viewBox="0 0 48 48" aria-hidden="true">
                      <path d="M28 8H12a4 4 0 00-4 4v20m32-12v8m0 0v8a4 4 0 01-4 4H12a4 4 0 01-4-4v-4m32-4l-3.172-3.172a4 4 0 00-5.656 0L28 28M8 32l9.172-9.172a4 4 0 015.656 0L28 28m0 0l4 4m4-24h8m-4-4v8m-12 4h.02" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
                    </svg>
                    <div class="flex text-sm text-gray-600 justify-center">
                      <label class="relative cursor-pointer bg-white rounded-md font-medium text-blue-600 hover:text-blue-500 focus-within:outline-none">
                        <span>选择文件</span>
                        <input 
                          ref="fileInput"
                          type="file" 
                          class="sr-only"
                          @change="handleFileSelect"
                        >
                      </label>
                      <p class="pl-1">或拖拽文件到此处</p>
                    </div>
                    <p class="text-xs text-gray-500">
                      支持任意格式文件
                    </p>
                  </div>
                </div>

                <!-- 文件名显示 -->
                <div v-if="selectedFile" class="flex items-center text-sm text-gray-600 bg-gray-50 p-2 rounded">
                  <svg class="h-5 w-5 text-gray-400 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                  </svg>
                  {{ selectedFile.name }}
                  <span class="ml-auto text-xs text-gray-400">{{ formatSize(selectedFile.size) }}</span>
                </div>

                <!-- 进度条 -->
                <div v-if="uploading">
                  <div class="relative pt-1">
                    <div class="flex mb-2 items-center justify-between">
                      <div>
                        <span class="text-xs font-semibold inline-block py-1 px-2 uppercase rounded-full text-blue-600 bg-blue-200">
                          上传中
                        </span>
                      </div>
                      <div class="text-right">
                        <span class="text-xs font-semibold inline-block text-blue-600">
                          {{ uploadProgress }}%
                        </span>
                      </div>
                    </div>
                    <div class="overflow-hidden h-2 mb-4 text-xs flex rounded bg-blue-200">
                      <div :style="{ width: uploadProgress + '%' }" class="shadow-none flex flex-col text-center whitespace-nowrap text-white justify-center bg-blue-500 transition-all duration-300"></div>
                    </div>
                  </div>
                </div>

                <!-- 错误信息 -->
                <div v-if="error" class="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded relative" role="alert">
                  <span class="block sm:inline text-sm">{{ error }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div class="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
          <button 
            type="button" 
            class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-blue-600 text-base font-medium text-white hover:bg-blue-700 focus:outline-none sm:ml-3 sm:w-auto sm:text-sm disabled:opacity-50 disabled:cursor-not-allowed"
            :disabled="!canUpload || uploading"
            @click="upload"
          >
            {{ uploading ? '上传中...' : '开始上传' }}
          </button>
          <button 
            type="button" 
            class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm"
            @click="close"
            :disabled="uploading"
          >
            取消
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { backupService } from '../services/pluginService'
import { useNotify } from '../utils/toast'

const props = defineProps({
  modelValue: Boolean,
  userKey: {
    type: String,
    required: true
  }
})

const emit = defineEmits(['update:modelValue', 'uploaded'])

const notify = useNotify()

const fileInput = ref(null)
const selectedFile = ref(null)
const backupType = ref('program')
const pluginName = ref('')
const description = ref('')
const uploading = ref(false)
const uploadProgress = ref(0)
const error = ref(null)

const canUpload = computed(() => {
  // plugin 类型需要填写插件名
  if (backupType.value === 'plugin' && !pluginName.value.trim()) {
    return false
  }
  return selectedFile.value && backupType.value && props.userKey
})

function close() {
  if (uploading.value) return
  emit('update:modelValue', false)
  reset()
}

function reset() {
  selectedFile.value = null
  backupType.value = 'program'
  pluginName.value = ''
  description.value = ''
  error.value = null
  uploadProgress.value = 0
  if (fileInput.value) {
    fileInput.value.value = ''
  }
}

function handleFileSelect(event) {
  const file = event.target.files[0]
  if (file) {
    selectedFile.value = file
    error.value = null
  }
}

function handleDrop(event) {
  const file = event.dataTransfer.files[0]
  if (file) {
    selectedFile.value = file
    error.value = null
  }
}

function formatSize(bytes) {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i]
}

async function upload() {
  if (!canUpload.value) return
  
  uploading.value = true
  error.value = null
  uploadProgress.value = 0
  
  try {
    await backupService.uploadBackup(
      selectedFile.value,
      props.userKey,
      backupType.value,
      description.value,
      backupType.value === 'plugin' ? pluginName.value.trim() : null,
      (progressEvent) => {
        uploadProgress.value = Math.round((progressEvent.loaded * 100) / progressEvent.total)
      }
    )
    
    notify.success('备份上传成功')
    emit('uploaded')
    reset()
  } catch (e) {
    const errorMsg = e.message || '上传失败'
    error.value = errorMsg
    notify.error(errorMsg)
  } finally {
    uploading.value = false
  }
}
</script>

