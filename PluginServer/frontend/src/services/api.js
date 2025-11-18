import axios from 'axios'
import { useNotificationStore } from '@/stores/notification'

// 导出API基础URL供其他模块使用
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || '/api'

// 创建axios实例
const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  }
})

// 请求拦截器
api.interceptors.request.use(
  (config) => {
    // 添加认证token（如果有）
    const token = localStorage.getItem('auth_token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// 响应拦截器
api.interceptors.response.use(
  (response) => {
    return response.data
  },
  (error) => {
    const notificationStore = useNotificationStore()

    // 处理不同类型的错误
    if (error.response) {
      const { status, data } = error.response

      // 优先使用自定义错误消息
      let errorMessage = '请求失败'

      // 检查是否是我们的自定义错误格式 {success: false, error: "..."}
      if (data && data.success === false && data.error) {
        errorMessage = data.error
      }
      // FastAPI 默认错误格式
      else if (data && data.detail) {
        errorMessage = data.detail
      }
      // 其他自定义错误消息
      else if (data && data.message) {
        errorMessage = data.message
      }

      switch (status) {
        case 400:
          notificationStore.showError(errorMessage)
          break
        case 401:
          notificationStore.showError('认证失败，请重新登录')
          // 清除token并跳转到登录页
          localStorage.removeItem('auth_token')
          // router.push('/login')
          break
        case 403:
          notificationStore.showError('权限不足')
          break
        case 404:
          notificationStore.showError('请求的资源不存在')
          break
        case 422:
          notificationStore.showError(errorMessage)
          break
        case 500:
          notificationStore.showError('服务器内部错误')
          break
        default:
          notificationStore.showError(errorMessage)
      }
    } else if (error.request) {
      // 网络错误
      notificationStore.showError('网络连接失败，请检查网络设置')
    } else {
      // 其他错误
      notificationStore.showError('请求配置错误')
    }

    return Promise.reject(error)
  }
)

export default api