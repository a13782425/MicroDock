import { createApp } from 'vue'
import { createPinia } from 'pinia'
import router from './router'
import Toast from 'vue-toastification'
import 'vue-toastification/dist/index.css'
import App from './App.vue'
import './style.css'

const app = createApp(App)

// Toast 通知配置
const toastOptions = {
    position: 'top-right',          // 右上角显示
    timeout: 3000,                  // 3秒后自动消失
    closeOnClick: true,             // 点击关闭
    pauseOnFocusLoss: true,         // 失去焦点时暂停
    pauseOnHover: true,             // 鼠标悬停时暂停
    draggable: true,                // 可拖拽
    draggablePercent: 0.6,          // 拖拽关闭阈值
    showCloseButtonOnHover: false,  // 悬停时显示关闭按钮
    hideProgressBar: false,         // 显示进度条
    closeButton: 'button',          // 关闭按钮类型
    icon: true,                     // 显示图标
    rtl: false,                     // 从右到左
    transition: 'Vue-Toastification__fade',  // 淡入淡出动画
    maxToasts: 5,                   // 最多同时显示5个
    newestOnTop: true               // 最新的在顶部
}

app.use(createPinia())
app.use(router)
app.use(Toast, toastOptions)
app.mount('#app')
