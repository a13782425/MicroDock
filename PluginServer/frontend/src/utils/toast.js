/**
 * Toast 通知工具函数
 * 
 * 封装 vue-toastification 的常用方法，方便在组件中调用
 */
import { useToast } from 'vue-toastification'

/**
 * 获取通知实例
 * 
 * @returns {Object} 包含 success, error, warning, info 方法的对象
 * 
 * @example
 * const notify = useNotify()
 * notify.success('操作成功')
 * notify.error('操作失败')
 * notify.warning('请注意')
 * notify.info('提示信息')
 */
export function useNotify() {
    const toast = useToast()
    
    return {
        /**
         * 成功通知 (绿色)
         * @param {string} message - 通知内容
         */
        success: (message) => toast.success(message),
        
        /**
         * 错误通知 (红色)
         * @param {string} message - 通知内容
         */
        error: (message) => toast.error(message),
        
        /**
         * 警告通知 (黄色)
         * @param {string} message - 通知内容
         */
        warning: (message) => toast.warning(message),
        
        /**
         * 信息通知 (蓝色)
         * @param {string} message - 通知内容
         */
        info: (message) => toast.info(message),
        
        /**
         * 清除所有通知
         */
        clear: () => toast.clear()
    }
}

