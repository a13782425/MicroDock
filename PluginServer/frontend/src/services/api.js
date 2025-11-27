import axios from 'axios'

// localStorage key（与 auth store 保持一致）
const TOKEN_KEY = 'admin_token'

const api = axios.create({
    baseURL: '/api',
    timeout: 30000
})

// 请求拦截器 - 自动添加 Authorization header
api.interceptors.request.use(
    config => {
        const token = localStorage.getItem(TOKEN_KEY)
        if (token) {
            config.headers.Authorization = `Bearer ${token}`
        }
        return config
    },
    error => {
        return Promise.reject(error)
    }
)

// 响应拦截器 - 处理错误和 401 状态
api.interceptors.response.use(
    response => {
        return response.data
    },
    error => {
        // 401 未授权 - token 过期或无效
        if (error.response?.status === 401) {
            // 清除本地 token
            localStorage.removeItem(TOKEN_KEY)
            localStorage.removeItem('admin_username')
            
            // 如果不是登录请求，可以考虑跳转到登录页
            // 但这里只做简单处理，由组件自行决定
        }
        
        // FastAPI 返回的错误格式是 { "detail": "错误信息" }
        const detail = error.response?.data?.detail
        const message = detail || error.response?.data?.message || error.message || '请求失败'
        console.error('API Error:', message, error.response?.status)
        return Promise.reject(new Error(message))
    }
)

export default api
