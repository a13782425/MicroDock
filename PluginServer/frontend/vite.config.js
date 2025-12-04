/**
 * Vite 配置文件
 * 
 * 支持通过 .env 文件配置以下环境变量：
 * - VITE_PORT: 前端开发服务器端口，默认 3000
 * - VITE_API_URL: 后端 API 地址，默认 http://localhost:8000
 * 
 * 注意：Vite 环境变量必须以 VITE_ 前缀开头才能在配置中使用
 */
import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig(({ mode }) => {
    // 加载环境变量，第三个参数 '' 表示加载所有环境变量（不仅限于 VITE_ 前缀）
    const env = loadEnv(mode, process.cwd(), '')
    
    const host = env.VITE_HOST || '0.0.0.0'

    // 前端服务端口，默认 3000
    const port = parseInt(env.VITE_PORT || '3000')
    
    // 后端 API 地址，默认 http://localhost:8000
    const apiUrl = env.VITE_API_URL || 'http://localhost:8000'
    
    console.log(`[Vite] 前端端口: ${port}`)
    console.log(`[Vite] 后端 API: ${apiUrl}`)
    
    return {
        plugins: [vue()],
        server: {
            // 前端开发服务器端口
            port: port,
            host: host,
            // API 代理配置，将 /api 请求转发到后端
            proxy: {
                '/api': {
                    target: apiUrl,
                    changeOrigin: true
                }
            }
        }
    }
})
