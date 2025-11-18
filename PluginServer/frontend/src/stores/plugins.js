import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import pluginService from '@/services/pluginService'

export const usePluginStore = defineStore('plugins', () => {
  // 状态
  const plugins = ref([])
  const loading = ref(false)
  const error = ref(null)
  const pagination = ref({
    total: 0,
    page: 1,
    limit: 20,
    totalPages: 0
  })

  // 过滤和搜索状态
  const filters = ref({
    search: '',
    plugin_type: '',
    is_active: null
  })

  // 计算属性
  const activePlugins = computed(() =>
    plugins.value.filter(p => p.is_active)
  )

  const inactivePlugins = computed(() =>
    plugins.value.filter(p => !p.is_active)
  )

  const outdatedPlugins = computed(() =>
    plugins.value.filter(p => p.is_outdated)
  )

  const pluginsByType = computed(() => {
    const grouped = {}
    plugins.value.forEach(plugin => {
      const type = plugin.plugin_type || 'unknown'
      if (!grouped[type]) {
        grouped[type] = []
      }
      grouped[type].push(plugin)
    })
    return grouped
  })

  const statistics = computed(() => ({
    total: plugins.value.length,
    active: activePlugins.value.length,
    inactive: inactivePlugins.value.length,
    outdated: outdatedPlugins.value.length,
    by_type: Object.keys(pluginsByType.value).map(type => ({
      type,
      count: pluginsByType.value[type].length
    })),
    total_size: plugins.value.reduce((sum, p) => sum + (p.file_size || 0), 0)
  }))

  // 方法
  async function fetchPlugins(params = {}) {
    try {
      loading.value = true
      error.value = null

      const response = await pluginService.getPlugins({
        skip: (pagination.value.page - 1) * pagination.value.limit,
        limit: pagination.value.limit,
        ...filters.value,
        ...params
      })

      plugins.value = response.data || response

      // 更新分页信息（如果API返回了分页信息）
      if (response.pagination) {
        pagination.value = { ...pagination.value, ...response.pagination }
      } else {
        // 计算本地分页
        pagination.value.total = plugins.value.length
        pagination.value.totalPages = Math.ceil(pagination.value.total / pagination.value.limit)
      }

      return response
    } catch (err) {
      error.value = err.message || '获取插件列表失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function fetchPluginWithVersions(pluginId) {
    try {
      loading.value = true
      const response = await pluginService.getPluginWithVersions(pluginId)
      return response
    } catch (err) {
      error.value = err.message || '获取插件详情失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function uploadPlugin(formData) {
    try {
      loading.value = true
      const response = await pluginService.uploadPlugin(formData)

      // 刷新插件列表
      await fetchPlugins()

      return response
    } catch (err) {
      error.value = err.message || '上传插件失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function updatePlugin(pluginId, data) {
    try {
      loading.value = true
      const response = await pluginService.updatePlugin(pluginId, data)

      // 更新本地状态
      const index = plugins.value.findIndex(p => p.id === pluginId)
      if (index > -1) {
        plugins.value[index] = { ...plugins.value[index], ...response }
      }

      return response
    } catch (err) {
      error.value = err.message || '更新插件失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function deletePlugin(pluginId) {
    try {
      loading.value = true
      await pluginService.deletePlugin(pluginId)

      // 从本地状态中移除
      const index = plugins.value.findIndex(p => p.id === pluginId)
      if (index > -1) {
        plugins.value.splice(index, 1)
      }

      return true
    } catch (err) {
      error.value = err.message || '删除插件失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function togglePlugin(pluginId, enabled) {
    try {
      const response = await pluginService.togglePlugin(pluginId, enabled)

      // 更新本地状态
      const plugin = plugins.value.find(p => p.id === pluginId)
      if (plugin) {
        plugin.is_active = enabled
      }

      return response
    } catch (err) {
      error.value = err.message || '切换插件状态失败'
      throw err
    }
  }

  async function scanPlugins() {
    try {
      loading.value = true
      const response = await pluginService.scanPlugins()

      // 刷新插件列表
      await fetchPlugins()

      return response
    } catch (err) {
      error.value = err.message || '扫描插件失败'
      throw err
    } finally {
      loading.value = false
    }
  }

  async function downloadPlugin(pluginId) {
    try {
      return await pluginService.downloadPlugin(pluginId)
    } catch (err) {
      error.value = err.message || '下载插件失败'
      throw err
    }
  }

  function setFilters(newFilters) {
    filters.value = { ...filters.value, ...newFilters }
    pagination.value.page = 1 // 重置到第一页
  }

  function setPage(page) {
    pagination.value.page = page
  }

  function setLimit(limit) {
    pagination.value.limit = limit
    pagination.value.page = 1 // 重置到第一页
  }

  function clearError() {
    error.value = null
  }

  function getPluginById(pluginId) {
    return plugins.value.find(p => p.id === pluginId)
  }

  function searchPlugins(query) {
    filters.value.search = query
    pagination.value.page = 1
    return fetchPlugins()
  }

  return {
    // 状态
    plugins,
    loading,
    error,
    pagination,
    filters,

    // 计算属性
    activePlugins,
    inactivePlugins,
    outdatedPlugins,
    pluginsByType,
    statistics,

    // 方法
    fetchPlugins,
    fetchPluginWithVersions,
    uploadPlugin,
    updatePlugin,
    deletePlugin,
    togglePlugin,
    scanPlugins,
    downloadPlugin,
    setFilters,
    setPage,
    setLimit,
    clearError,
    getPluginById,
    searchPlugins
  }
})