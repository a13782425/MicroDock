import api from './api'

export const pluginService = {
  // 获取插件列表
  async getPlugins(params = {}) {
    const { skip = 0, limit = 100, plugin_type, is_active, search } = params
    const queryParams = new URLSearchParams({
      skip: skip.toString(),
      limit: limit.toString()
    })

    if (plugin_type) queryParams.append('plugin_type', plugin_type)
    if (is_active !== undefined && is_active !== null) queryParams.append('is_active', is_active.toString())
    if (search) queryParams.append('search', search)

    return api.get(`/plugins?${queryParams}`)
  },

  // 获取插件详情（包含版本信息）
  async getPluginWithVersions(pluginId) {
    return api.get(`/plugins/${pluginId}`)
  },

  // 根据名称获取插件
  async getPluginByName(pluginName) {
    return api.get(`/plugins/name/${pluginName}`)
  },

  // 上传插件
  async uploadPlugin(formData) {
    return api.post('/plugins', formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    })
  },

  // 更新插件
  async updatePlugin(pluginId, data) {
    return api.put(`/plugins/${pluginId}`, data)
  },

  // 删除插件
  async deletePlugin(pluginId) {
    return api.delete(`/plugins/${pluginId}`)
  },

  // 下载插件
  async downloadPlugin(pluginId) {
    const response = await api.get(`/plugins/${pluginId}/download`, {
      responseType: 'blob'
    })

    // 创建下载链接
    const url = window.URL.createObjectURL(new Blob([response]))
    const link = document.createElement('a')
    link.href = url

    // 获取文件名
    const contentDisposition = response.headers['content-disposition']
    let filename = `plugin_${pluginId}`
    if (contentDisposition) {
      const filenameMatch = contentDisposition.match(/filename="?([^"]+)"?/)
      if (filenameMatch) {
        filename = filenameMatch[1]
      }
    }

    link.setAttribute('download', filename)
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    window.URL.revokeObjectURL(url)
  },

  // 扫描插件目录
  async scanPlugins() {
    return api.post('/plugins/scan')
  },

  // 切换插件状态
  async togglePlugin(pluginId, enabled) {
    return api.get(`/plugins/${pluginId}/toggle?enabled=${enabled}`)
  },

  // 获取插件统计信息
  async getPluginStatistics() {
    // 这个可以通过获取所有插件来计算
    const response = await this.getPlugins({ limit: 1000 })
    const plugins = response.data || []

    const stats = {
      total: plugins.length,
      active: plugins.filter(p => p.is_active).length,
      inactive: plugins.filter(p => !p.is_active).length,
      by_type: {},
      total_size: 0
    }

    plugins.forEach(plugin => {
      // 按类型统计
      const type = plugin.plugin_type || 'unknown'
      stats.by_type[type] = (stats.by_type[type] || 0) + 1

      // 总大小
      stats.total_size += plugin.file_size || 0
    })

    return stats
  },

  // 验证插件文件
  validatePluginFile(file) {
    // 检查文件类型
    const allowedTypes = ['.zip', '.dll']
    const fileExtension = '.' + file.name.split('.').pop().toLowerCase()

    if (!allowedTypes.includes(fileExtension)) {
      throw new Error('只支持 .zip 和 .dll 文件格式')
    }

    // 检查文件大小（限制100MB）
    const maxSize = 100 * 1024 * 1024
    if (file.size > maxSize) {
      throw new Error('文件大小不能超过100MB')
    }

    return true
  },

  // 格式化文件大小
  formatFileSize(bytes) {
    if (bytes === 0) return '0 B'

    const k = 1024
    const sizes = ['B', 'KB', 'MB', 'GB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))

    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i]
  },

  // 获取插件状态文本
  getPluginStatusText(plugin) {
    if (plugin.is_outdated) {
      return { text: '已过时', class: 'badge-warning' }
    }
    if (plugin.is_active) {
      return { text: '正常', class: 'badge-success' }
    }
    return { text: '已禁用', class: 'badge-gray' }
  },

  // 获取插件类型文本
  getPluginTypeText(type) {
    const typeMap = {
      'storage': { text: '存储器', icon: '📦' },
      'service': { text: '服务', icon: '⚙️' },
      'tab': { text: '标签页', icon: '📑' },
      'unknown': { text: '未知', icon: '❓' }
    }
    return typeMap[type] || typeMap.unknown
  }
}

export default pluginService