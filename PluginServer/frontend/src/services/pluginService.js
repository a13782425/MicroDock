import api from './api'

export const pluginService = {
    // 获取所有插件
    getAllPlugins() {
        return api.get('/plugins/list')
    },

    // 获取插件详情
    getPlugin(id) {
        return api.post('/plugins/detail', { id })
    },

    // 上传插件（需要 plugin_key）
    uploadPlugin(file, pluginKey, onProgress) {
        const formData = new FormData()
        formData.append('file', file)
        formData.append('plugin_key', pluginKey)

        return api.post('/plugins/upload', formData, {
            headers: {
                'Content-Type': 'multipart/form-data'
            },
            onUploadProgress: onProgress
        })
    },

    // 启用插件
    enablePlugin(id) {
        return api.post('/plugins/enable', { id })
    },

    // 禁用插件
    disablePlugin(id) {
        return api.post('/plugins/disable', { id })
    },

    // 标记为过时
    deprecatePlugin(id) {
        return api.post('/plugins/deprecate', { id })
    },

    // 删除插件
    deletePlugin(id) {
        return api.post('/plugins/delete', { id })
    },

    // 下载插件
    downloadPlugin(id) {
        return api.post('/plugins/download', { id }, {
            responseType: 'blob'
        })
    },

    // 获取插件版本列表
    getPluginVersions(id) {
        return api.post('/plugins/versions', { id })
    },

    // 获取版本详情
    getVersionDetail(id) {
        return api.post('/versions/detail', { id })
    },

    // 下载指定版本
    downloadVersion(id) {
        return api.post('/versions/download', { id }, {
            responseType: 'blob'
        })
    },

    // 标记版本为过时
    deprecateVersion(id) {
        return api.post('/versions/deprecate', { id })
    }
}

// 备份服务
export const backupService = {
    // 上传备份
    uploadBackup(file, userKey, backupType, description = '', onProgress) {
        const formData = new FormData()
        formData.append('file', file)
        formData.append('user_key', userKey)
        formData.append('backup_type', backupType)
        formData.append('description', description)

        return api.post('/backups/upload', formData, {
            headers: {
                'Content-Type': 'multipart/form-data'
            },
            onUploadProgress: onProgress
        })
    },

    // 获取备份列表
    getBackups(userKey) {
        return api.post('/backups/list', { user_key: userKey })
    },

    // 下载备份
    downloadBackup(userKey, id) {
        return api.post('/backups/download', { user_key: userKey, id }, {
            responseType: 'blob'
        })
    },

    // 删除备份
    deleteBackup(userKey, id) {
        return api.post('/backups/delete', { user_key: userKey, id })
    }
}
