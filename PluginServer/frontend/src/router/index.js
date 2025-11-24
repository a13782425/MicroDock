import { createRouter, createWebHistory } from 'vue-router'
import PluginList from '../views/PluginList.vue'

const routes = [
    {
        path: '/',
        name: 'PluginList',
        component: PluginList
    }
]

const router = createRouter({
    history: createWebHistory(),
    routes
})

export default router
