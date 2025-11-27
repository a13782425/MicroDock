/**
 * 认证状态管理
 */
import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '../services/api'

// localStorage key
const TOKEN_KEY = 'admin_token'
const USERNAME_KEY = 'admin_username'

export const useAuthStore = defineStore('auth', () => {
    // 状态
    const token = ref(localStorage.getItem(TOKEN_KEY) || '')
    const username = ref(localStorage.getItem(USERNAME_KEY) || '')
    const loading = ref(false)
    const error = ref(null)

    // 计算属性
    const isLoggedIn = computed(() => !!token.value)

    /**
     * 登录
     * @param {string} inputUsername - 用户名
     * @param {string} password - 密码
     * @returns {Promise<boolean>} - 登录是否成功
     */
    async function login(inputUsername, password) {
        loading.value = true
        error.value = null
        
        try {
            const response = await api.post('/auth/login', {
                username: inputUsername,
                password: password
            })
            
            // 保存 token
            token.value = response.token
            username.value = inputUsername
            
            // 持久化到 localStorage
            localStorage.setItem(TOKEN_KEY, response.token)
            localStorage.setItem(USERNAME_KEY, inputUsername)
            
            return true
        } catch (e) {
            error.value = e.message || '登录失败'
            return false
        } finally {
            loading.value = false
        }
    }

    /**
     * 登出
     */
    function logout() {
        // 清除状态
        token.value = ''
        username.value = ''
        error.value = null
        
        // 清除 localStorage
        localStorage.removeItem(TOKEN_KEY)
        localStorage.removeItem(USERNAME_KEY)
    }

    /**
     * 检查登录状态
     * @returns {Promise<boolean>} - 是否已登录
     */
    async function checkAuth() {
        if (!token.value) {
            return false
        }
        
        try {
            const response = await api.get('/auth/me')
            return response.is_logged_in
        } catch (e) {
            // token 无效，清除状态
            logout()
            return false
        }
    }

    /**
     * 获取 token（供 API 服务使用）
     * @returns {string} - JWT token
     */
    function getToken() {
        return token.value
    }

    return {
        // 状态
        token,
        username,
        loading,
        error,
        // 计算属性
        isLoggedIn,
        // 方法
        login,
        logout,
        checkAuth,
        getToken
    }
})

