<template>
  <Modal :show="show" @update:show="emit('update:show', $event)" title="上传插件" @close="resetForm">
    <form @submit.prevent="handleSubmit" class="space-y-4">
      <!-- 文件上传区域 -->
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-2">
          插件文件 <span class="text-danger-500">*</span>
        </label>
        <div
          @drop="handleDrop"
          @dragover.prevent
          @dragenter.prevent
          :class="[
            'relative border-2 border-dashed rounded-lg p-6 text-center transition-colors duration-200',
            isDragging ? 'border-primary-500 bg-primary-50' : 'border-gray-300 hover:border-gray-400',
            previewing ? 'border-warning-400 bg-warning-50' : ''
          ]"
        >
          <input
            ref="fileInput"
            type="file"
            @change="handleFileSelect"
            accept=".zip"
            class="hidden"
          />

          <div v-if="!selectedFile" class="space-y-2">
            <div class="mx-auto w-12 h-12 bg-gray-100 rounded-full flex items-center justify-center">
              <svg class="w-6 h-6 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
              </svg>
            </div>
            <div>
              <p class="text-sm text-gray-600">
                拖拽ZIP文件到此处，或者
                <button
                  type="button"
                  @click="$refs.fileInput.click()"
                  class="text-primary-600 hover:text-primary-500 font-medium"
                >
                  点击选择文件
                </button>
              </p>
              <p class="text-xs text-gray-500 mt-1">
                仅支持 .zip 格式，最大 50MB
              </p>
              <p class="text-xs text-primary-600 mt-1">
                <strong>新版功能:</strong> 自动从 plugin.json 提取插件信息
              </p>
            </div>
          </div>

          <div v-else class="space-y-2">
            <div class="mx-auto w-12 h-12 rounded-full flex items-center justify-center"
                 :class="previewError ? 'bg-danger-100' : 'bg-success-100'">
              <svg v-if="previewError" class="w-6 h-6 text-danger-600" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
              </svg>
              <svg v-else class="w-6 h-6 text-success-600" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
              </svg>
            </div>
            <div>
              <p class="text-sm font-medium text-gray-900">{{ selectedFile.name }}</p>
              <p class="text-xs text-gray-500">{{ formatFileSize(selectedFile.size) }}</p>
              <div v-if="previewing" class="flex items-center mt-1">
                <div class="w-3 h-3 mr-2 loading-spinner"></div>
                <p class="text-xs text-warning-600">正在解析插件信息...</p>
              </div>
              <div v-else-if="previewError" class="mt-1">
                <p class="text-xs text-danger-600">解析失败: {{ previewError }}</p>
              </div>
              <div v-else-if="extractedMetadata" class="mt-1">
                <p class="text-xs text-success-600">✓ 成功解析 plugin.json</p>
              </div>
            </div>
            <button
              type="button"
              @click="removeFile"
              class="text-sm text-danger-600 hover:text-danger-500"
            >
              移除文件
            </button>
          </div>
        </div>
      </div>

  
      <!-- 插件信息表单 -->
      <div class="space-y-4">
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">
              插件名称 <span class="text-danger-500">*</span>
              <span v-if="extractedMetadata" class="text-xs text-primary-600 ml-1">(已自动填充)</span>
            </label>
            <input
              v-model="formData.name"
              type="text"
              required
              class="form-input"
              placeholder="例如: MyPlugin"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">
              版本号 <span class="text-danger-500">*</span>
              <span v-if="extractedMetadata" class="text-xs text-primary-600 ml-1">(已自动填充)</span>
            </label>
            <input
              v-model="formData.version"
              type="text"
              required
              class="form-input"
              placeholder="例如: 1.0.0"
            />
          </div>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">
              显示名称
              <span v-if="extractedMetadata" class="text-xs text-primary-600 ml-1">(已自动填充)</span>
            </label>
            <input
              v-model="formData.display_name"
              type="text"
              class="form-input"
              placeholder="插件显示名称"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">
              插件类型
            </label>
            <select v-model="formData.plugin_type" class="form-select">
              <option value="storage">存储器</option>
              <option value="service">服务</option>
              <option value="tab">标签页</option>
            </select>
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">
            作者
            <span v-if="extractedMetadata && extractedMetadata.author" class="text-xs text-primary-600 ml-1">(已自动填充)</span>
          </label>
          <input
            v-model="formData.author"
            type="text"
            class="form-input"
            placeholder="插件作者"
          />
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">
            描述
            <span v-if="extractedMetadata && extractedMetadata.description" class="text-xs text-primary-600 ml-1">(已自动填充)</span>
          </label>
          <textarea
            v-model="formData.description"
            rows="3"
            class="form-textarea"
            placeholder="插件功能描述..."
          ></textarea>
        </div>
      </div>
    </form>

    <template #footer>
      <div class="flex justify-end space-x-3">
        <button
          type="button"
          @click="close"
          class="btn btn-outline"
        >
          取消
        </button>
        <button
          @click="handleSubmit"
          :disabled="!canSubmit"
          class="btn btn-primary"
        >
          <div v-if="uploading" class="w-4 h-4 mr-2 loading-spinner"></div>
          {{ uploading ? '上传中...' : '上传插件' }}
        </button>
      </div>
    </template>
  </Modal>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { useNotificationStore } from '@/stores/notification'
import { usePluginStore } from '@/stores/plugins'
import { API_BASE_URL } from '@/services/api'
import Modal from './Modal.vue'

const props = defineProps({
  show: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['update:show', 'uploaded'])

const notificationStore = useNotificationStore()
const pluginStore = usePluginStore()

const fileInput = ref(null)
const selectedFile = ref(null)
const isDragging = ref(false)
const uploading = ref(false)
const previewing = ref(false)
const previewError = ref('')
const extractedMetadata = ref(null)

const formData = ref({
  name: '',
  display_name: '',
  version: '',
  description: '',
  author: '',
  plugin_type: 'storage'
})

const canSubmit = computed(() => {
  return selectedFile.value &&
         formData.value.name.trim() &&
         formData.value.version.trim() &&
         !uploading.value &&
         !previewing.value
})

function close() {
  emit('update:show', false)
}

function resetForm() {
  selectedFile.value = null
  isDragging.value = false
  uploading.value = false
  previewing.value = false
  previewError.value = ''
  extractedMetadata.value = null
  formData.value = {
    name: '',
    display_name: '',
    version: '',
    description: '',
    author: '',
    plugin_type: 'storage'
  }
}

function handleDrop(e) {
  e.preventDefault()
  isDragging.value = false

  const files = e.dataTransfer.files
  if (files.length > 0) {
    handleFile(files[0])
  }
}

function handleFileSelect(e) {
  const files = e.target.files
  if (files.length > 0) {
    handleFile(files[0])
  }
}

async function handleFile(file) {
  try {
    // 重置状态
    previewError.value = ''
    extractedMetadata.value = null
    previewing.value = true

    // 严格的文件验证 - 只允许ZIP
    const fileExt = file.name.toLowerCase().substring(file.name.lastIndexOf('.'))
    if (fileExt !== '.zip') {
      throw new Error('新版仅支持 .zip 格式文件，请确保插件包含 plugin.json 配置文件')
    }

    if (file.size > 50 * 1024 * 1024) { // 50MB
      throw new Error('文件大小不能超过 50MB')
    }

    selectedFile.value = file

    // 预览并提取插件元数据
    await previewPlugin(file)

  } catch (error) {
    notificationStore.showError(error.message)
    previewError.value = error.message
    selectedFile.value = null
    previewing.value = false
  }
}

async function previewPlugin(file) {
  try {
    previewing.value = true
    previewError.value = ''

    // 创建FormData预览
    const formData_preview = new FormData()
    formData_preview.append('file', file)

    // 调用预览API
    const response = await fetch(`${API_BASE_URL}/plugins/preview`, {
      method: 'POST',
      body: formData_preview
    })

    const result = await response.json()

    if (result.success && result.metadata) {
      extractedMetadata.value = result.metadata

      // 自动填充表单数据
      if (result.metadata.name) {
        formData.value.name = result.metadata.name
      }
      if (result.metadata.displayName) {
        formData.value.display_name = result.metadata.displayName
      }
      if (result.metadata.version) {
        formData.value.version = result.metadata.version
      }
      if (result.metadata.author) {
        formData.value.author = result.metadata.author
      }
      if (result.metadata.description) {
        formData.value.description = result.metadata.description
      }

      notificationStore.showSuccess('插件信息提取成功')
    } else {
      throw new Error(result.error || '无法解析插件文件')
    }

  } catch (error) {
    previewError.value = error.message
    throw error
  } finally {
    previewing.value = false
  }
}

function removeFile() {
  selectedFile.value = null
  extractedMetadata.value = null
  previewError.value = ''
  if (fileInput.value) {
    fileInput.value.value = ''
  }
}

function formatFileSize(bytes) {
  if (bytes === 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i]
}

async function handleSubmit() {
  if (!canSubmit.value) return

  try {
    uploading.value = true

    const formData_upload = new FormData()
    formData_upload.append('file', selectedFile.value)

    // 如果有提取的元数据，则使用提取的值，否则使用表单中的值
    if (extractedMetadata.value) {
      // 后端会优先使用ZIP文件中的元数据，这里可以不传或传空值
      // 但为了兼容性，我们仍然传递表单值作为后备
      formData_upload.append('name', formData.value.name.trim())
      formData_upload.append('display_name', formData.value.display_name.trim())
      formData_upload.append('version', formData.value.version.trim())
      formData_upload.append('description', formData.value.description.trim())
      formData_upload.append('author', formData.value.author.trim())
    } else {
      // 兜底：如果没有提取到元数据，使用表单中的值
      formData_upload.append('name', formData.value.name.trim())
      formData_upload.append('display_name', formData.value.display_name.trim())
      formData_upload.append('version', formData.value.version.trim())
      formData_upload.append('description', formData.value.description.trim())
      formData_upload.append('author', formData.value.author.trim())
    }

    formData_upload.append('plugin_type', formData.value.plugin_type)

    await pluginStore.uploadPlugin(formData_upload)

    // 只有在真正成功时才显示成功消息
    notificationStore.showSuccess('插件上传成功')
    close()
    emit('uploaded')

  } catch (error) {
    // 错误已在API拦截器中处理，这里不需要额外处理
    console.error('插件上传失败:', error)
  } finally {
    uploading.value = false
  }
}

// 拖拽事件处理
function handleDragOver(e) {
  e.preventDefault()
  isDragging.value = true
}

function handleDragLeave(e) {
  e.preventDefault()
  isDragging.value = false
}

// 监听对话框关闭，清理状态
watch(() => props.show, (newVal) => {
  if (!newVal) {
    resetForm()
  }
})
</script>