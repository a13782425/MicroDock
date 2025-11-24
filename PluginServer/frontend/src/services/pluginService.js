import api from './api'

export const pluginService = {
    // 获取所有插件
    getAllPlugins() {
        return api.get('/plugins')
    },

    // 获取插件详情
    getPlugin(id) {
        return api.get(`/plugins/${id}`)
    },

    // 上传插件
    uploadPlugin(file, onProgress) {
        const formData = new FormData()
        formData.append('file', file)

        return api.post('/plugins', formData, {
            headers: {
                'Content-Type': 'multipart/form-data'
            },
            onUploadProgress: onProgress
        })
    },

    // 启用插件
    enablePlugin(id) {
        return api.patch(`/plugins/${id}/enable`)
    },

    // 禁用插件
    disablePlugin(id) {
        return api.patch(`/plugins/${id}/disable`)
    },

    // 标记为过时
    deprecatePlugin(id) {
        return api.patch(`/plugins/${id}/deprecate`)
    },

    // 删除插件
    deletePlugin(id) {
        return api.delete(`/plugins/${id}`)
    },

    // 下载插件
    downloadPlugin(id) {
        return api.get(`/plugins/${id}/download`, {
            responseType: 'blob'
        })
    },

    // 获取插件版本列表
    getPluginVersions(id) {
        return api.get(`/plugins/${id}/versions`)
    }
}
