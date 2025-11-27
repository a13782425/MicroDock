import { createRouter, createWebHistory } from 'vue-router'
import PluginList from '../views/PluginList.vue'
import BackupList from '../views/BackupList.vue'
import Login from '../views/Login.vue'

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
    },
    {
        path: '/login',
        name: 'Login',
        component: Login
    }
]

const router = createRouter({
    history: createWebHistory(),
    routes
})

export default router
