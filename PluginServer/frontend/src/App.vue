<template>
  <div class="min-h-screen bg-gray-100">
    <nav class="bg-white shadow-lg">
      <div class="max-w-7xl mx-auto px-4">
        <div class="flex justify-between h-16">
          <div class="flex items-center">
            <h1 class="text-2xl font-bold text-gray-800">MicroDock 插件管理</h1>
          </div>
          <div class="flex space-x-4 items-center">
            <router-link 
              to="/" 
              class="px-3 py-2 rounded-md text-sm font-medium hover:bg-gray-200"
              :class="$route.path === '/' ? 'bg-gray-200' : ''"
            >
              插件列表
            </router-link>
            <router-link 
              to="/backups" 
              class="px-3 py-2 rounded-md text-sm font-medium hover:bg-gray-200"
              :class="$route.path === '/backups' ? 'bg-gray-200' : ''"
            >
              用户备份
            </router-link>
          </div>
        </div>
      </div>
    </nav>
    
    <main class="max-w-7xl mx-auto py-6 px-4">
      <router-view />
    </main>
  </div>
</template>

<script setup>
</script>
