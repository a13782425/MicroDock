import axios from 'axios'

const api = axios.create({
    baseURL: '/api',
    timeout: 30000
})

// 请求拦截器
api.interceptors.request.use(
    config => {
        return config
    },
    error => {
        return Promise.reject(error)
    }
)

// 响应拦截器
api.interceptors.response.use(
    response => {
        return response.data
    },
    error => {
        // FastAPI 返回的错误格式是 { "detail": "错误信息" }
        const detail = error.response?.data?.detail
        const message = detail || error.response?.data?.message || error.message || '请求失败'
        console.error('API Error:', message, error.response?.status)
        return Promise.reject(new Error(message))
    }
)

export default api
