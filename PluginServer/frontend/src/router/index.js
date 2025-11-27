import { createRouter, createWebHistory } from 'vue-router'
import PluginList from '../views/PluginList.vue'
import BackupList from '../views/BackupList.vue'

const routes = [
    {
        path: '/',
        name: 'PluginList',
        component: PluginList
    },
    {
        path: '/backups',
        name: 'BackupList',
        component: BackupList
    }
]

const router = createRouter({
    history: createWebHistory(),
    routes
})

export default router
