import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useNotificationStore = defineStore('notification', () => {
  const notifications = ref([])

  function addNotification(message, type = 'info', duration = 5000) {
    const id = Date.now() + Math.random()
    const notification = {
      id,
      message,
      type,
      duration,
      show: false
    }

    notifications.value.push(notification)

    // 触发动画
    setTimeout(() => {
      notification.show = true
    }, 10)

    // 自动移除
    if (duration > 0) {
      setTimeout(() => {
        removeNotification(id)
      }, duration)
    }

    return id
  }

  function removeNotification(id) {
    const index = notifications.value.findIndex(n => n.id === id)
    if (index > -1) {
      notifications.value[index].show = false
      setTimeout(() => {
        notifications.value.splice(index, 1)
      }, 300)
    }
  }

  function showSuccess(message, duration = 5000) {
    return addNotification(message, 'success', duration)
  }

  function showError(message, duration = 0) {
    return addNotification(message, 'danger', duration)
  }

  function showWarning(message, duration = 5000) {
    return addNotification(message, 'warning', duration)
  }

  function showInfo(message, duration = 5000) {
    return addNotification(message, 'info', duration)
  }

  function clearAll() {
    notifications.value.forEach(n => {
      n.show = false
    })
    setTimeout(() => {
      notifications.value = []
    }, 300)
  }

  return {
    notifications,
    addNotification,
    removeNotification,
    showSuccess,
    showError,
    showWarning,
    showInfo,
    clearAll
  }
})