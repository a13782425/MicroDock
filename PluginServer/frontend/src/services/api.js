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

// 响应拦截器 - 解析统一响应格式 { success, message, data }
api.interceptors.response.use(
    response => {
        const result = response.data
        
        // 新的统一响应格式: { success, message, data }
        if (typeof result === 'object' && 'success' in result) {
            if (result.success) {
                // 返回 data 字段，保持向后兼容
                return result.data
            } else {
                // success=false，抛出错误
                return Promise.reject(new Error(result.message || '请求失败'))
            }
        }
        
        // 兼容旧格式或文件下载等特殊响应
        return result
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
        
        // 新的统一错误格式: { success: false, message: "...", data: null }
        const responseData = error.response?.data
        let message = '请求失败'
        
        if (responseData) {
            // 优先使用新格式的 message
            if (responseData.message) {
                message = responseData.message
            }
            // 兼容旧的 detail 格式
            else if (responseData.detail) {
                message = responseData.detail
            }
        } else if (error.message) {
            message = error.message
        }
        
        console.error('API Error:', message, error.response?.status)
        return Promise.reject(new Error(message))
    }
)

export default api
