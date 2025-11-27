import { defineStore } from 'pinia'
import { ref } from 'vue'
import { pluginService } from '../services/pluginService'

export const usePluginStore = defineStore('plugin', () => {
    const plugins = ref([])
    const loading = ref(false)
    const error = ref(null)

    async function fetchPlugins() {
        loading.value = true
        error.value = null
        try {
            plugins.value = await pluginService.getAllPlugins()
        } catch (e) {
            error.value = e.message
            throw e
        } finally {
            loading.value = false
        }
    }

    async function uploadPlugin(file) {
        loading.value = true
        error.value = null
        try {
            await pluginService.uploadPlugin(file)
            await fetchPlugins()
        } catch (e) {
            error.value = e.message
            throw e
        } finally {
            loading.value = false
        }
    }

    async function deletePlugin(name) {
        try {
            await pluginService.deletePlugin(name)
            await fetchPlugins()
        } catch (e) {
            error.value = e.message
            throw e
        }
    }

    async function togglePlugin(name, enabled) {
        try {
            if (enabled) {
                await pluginService.enablePlugin(name)
            } else {
                await pluginService.disablePlugin(name)
            }
            await fetchPlugins()
        } catch (e) {
            error.value = e.message
            throw e
        }
    }

    return {
        plugins,
        loading,
        error,
        fetchPlugins,
        uploadPlugin,
        deletePlugin,
        togglePlugin
    }
})
