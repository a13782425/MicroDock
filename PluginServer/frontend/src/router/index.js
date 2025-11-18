import { createRouter, createWebHistory } from 'vue-router'

const routes = [
  {
    path: '/',
    name: 'Dashboard',
    component: () => import('@/views/Dashboard.vue'),
    meta: {
      title: '仪表板'
    }
  },
  {
    path: '/plugins',
    name: 'Plugins',
    component: () => import('@/views/Plugins.vue'),
    meta: {
      title: '插件管理'
    }
  },
  {
    path: '/plugins/:id',
    name: 'PluginDetail',
    component: () => import('@/views/PluginDetail.vue'),
    meta: {
      title: '插件详情'
    }
  },
  {
    path: '/versions',
    name: 'Versions',
    component: () => import('@/views/Versions.vue'),
    meta: {
      title: '版本管理'
    }
  },
  {
    path: '/backups',
    name: 'Backups',
    component: () => import('@/views/Backups.vue'),
    meta: {
      title: '备份管理'
    }
  },
  {
    path: '/settings',
    name: 'Settings',
    component: () => import('@/views/Settings.vue'),
    meta: {
      title: '系统设置'
    }
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: () => import('@/views/NotFound.vue'),
    meta: {
      title: '页面不存在'
    }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior(to, from, savedPosition) {
    if (savedPosition) {
      return savedPosition
    } else {
      return { top: 0 }
    }
  }
})

// 全局前置守卫
router.beforeEach((to, from, next) => {
  // 设置页面标题
  if (to.meta.title) {
    document.title = `${to.meta.title} - MicroDock 插件管理服务器`
  }

  // 这里可以添加认证检查等逻辑
  next()
})

export default router