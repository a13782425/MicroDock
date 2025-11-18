<template>
  <Modal v-model:show="show" title="上传插件" @close="resetForm">
    <form @submit.prevent="handleSubmit" class="space-y-4">
      <!-- 文件上传区域 -->
      <div>
        <label class="block text-sm font-medium text-gray-700 mb-2">
          插件文件
        </label>
        <div
          @drop="handleDrop"
          @dragover.prevent
          @dragenter.prevent
          :class="[
            'relative border-2 border-dashed rounded-lg p-6 text-center transition-colors duration-200',
            isDragging ? 'border-primary-500 bg-primary-50' : 'border-gray-300 hover:border-gray-400'
          ]"
        >
          <input
            ref="fileInput"
            type="file"
            @change="handleFileSelect"
            accept=".zip,.dll"
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
                拖拽文件到此处，或者
                <button
                  type="button"
                  @click="$refs.fileInput.click()"
                  class="text-primary-600 hover:text-primary-500 font-medium"
                >
                  点击选择文件
                </button>
              </p>
              <p class="text-xs text-gray-500 mt-1">
                支持 .zip 和 .dll 格式，最大 100MB
              </p>
            </div>
          </div>

          <div v-else class="space-y-2">
            <div class="mx-auto w-12 h-12 bg-success-100 rounded-full flex items-center justify-center">
              <svg class="w-6 h-6 text-success-600" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
              </svg>
            </div>
            <div>
              <p class="text-sm font-medium text-gray-900">{{ selectedFile.name }}</p>
              <p class="text-xs text-gray-500">{{ formatFileSize(selectedFile.size) }}</p>
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

      <!-- 插件信息 -->
      <div class="grid grid-cols-2 gap-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">
            插件名称 *
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
            版本号 *
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
        </label>
        <textarea
          v-model="formData.description"
          rows="3"
          class="form-textarea"
          placeholder="插件功能描述..."
        ></textarea>
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
import { ref, computed } from 'vue'
import { useNotificationStore } from '@/stores/notification'
import { usePluginStore } from '@/stores/plugins'
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
         !uploading.value
})

function close() {
  emit('update:show', false)
}

function resetForm() {
  selectedFile.value = null
  isDragging.value = false
  uploading.value = false
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

function handleFile(file) {
  try {
    // 验证文件
    pluginService.validatePluginFile(file)

    selectedFile.value = file

    // 如果文件名看起来像插件名，自动填充
    const nameWithoutExt = file.name.replace(/\.[^/.]+$/, '')
    if (!formData.value.name) {
      formData.value.name = nameWithoutExt
    }
    if (!formData.value.display_name) {
      formData.value.display_name = nameWithoutExt
    }

  } catch (error) {
    notificationStore.showError(error.message)
    selectedFile.value = null
  }
}

function removeFile() {
  selectedFile.value = null
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
    formData_upload.append('name', formData.value.name.trim())
    formData_upload.append('display_name', formData.value.display_name.trim())
    formData_upload.append('version', formData.value.version.trim())
    formData_upload.append('description', formData.value.description.trim())
    formData_upload.append('author', formData.value.author.trim())
    formData_upload.append('plugin_type', formData.value.plugin_type)

    await pluginStore.uploadPlugin(formData_upload)

    notificationStore.showSuccess('插件上传成功')
    close()
    emit('uploaded')

  } catch (error) {
    // 错误已在store中处理
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
</script>