import { createApp } from 'vue'
import { createPinia } from 'pinia'
import router from './router'
import App from './App.vue'

// 导入样式
import './style.css'

// 创建应用实例
const app = createApp(App)

// 使用插件
app.use(createPinia())
app.use(router)

// 全局错误处理
app.config.errorHandler = (err, vm, info) => {
  console.error('Vue Error:', err, info)
  // 这里可以添加错误上报逻辑
}

// 挂载应用
app.mount('#app')

// 隐藏加载屏幕
document.body.classList.add('app-ready')