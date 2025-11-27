import api from './api'

export const pluginService = {
    // 获取所有插件
    getAllPlugins() {
        return api.get('/plugins/list')
    },

    // 获取插件详情（使用插件名）
    getPlugin(name) {
        return api.post('/plugins/detail', { name })
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

    // 启用插件（使用插件名）
    enablePlugin(name) {
        return api.post('/plugins/enable', { name })
    },

    // 禁用插件（使用插件名）
    disablePlugin(name) {
        return api.post('/plugins/disable', { name })
    },

    // 标记为过时（使用插件名）
    deprecatePlugin(name) {
        return api.post('/plugins/deprecate', { name })
    },

    // 删除插件（使用插件名）
    deletePlugin(name) {
        return api.post('/plugins/delete', { name })
    },

    // 下载插件（使用插件名）
    downloadPlugin(name) {
        return api.post('/plugins/download', { name }, {
            responseType: 'blob'
        })
    },

    // 获取插件版本列表（使用插件名）
    getPluginVersions(name) {
        return api.post('/plugins/versions', { name })
    },

    // 获取版本详情（使用插件名 + 版本号）
    getVersionDetail(name, version) {
        return api.post('/plugins/version/detail', { name, version })
    },

    // 下载指定版本（使用插件名 + 版本号）
    downloadVersion(name, version) {
        return api.post('/plugins/version/download', { name, version }, {
            responseType: 'blob'
        })
    },

    // 标记版本为过时（使用插件名 + 版本号）
    deprecateVersion(name, version) {
        return api.post('/plugins/version/deprecate', { name, version })
    }
}

// 备份服务
export const backupService = {
    // 上传备份
    uploadBackup(file, userKey, backupType, description = '', pluginName = null, onProgress) {
        const formData = new FormData()
        formData.append('file', file)
        formData.append('user_key', userKey)
        formData.append('backup_type', backupType)
        formData.append('description', description)
        if (pluginName) {
            formData.append('plugin_name', pluginName)
        }

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
