import Home from '@/views/Home.vue'
import Register from '@/views/Register.vue'
import Login from '@/views/Login.vue'
import Top5 from '@/views/Top5.vue'

import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      "path": "/",
      "name": "home",
      "component": Home
    },
    {
      "path": "/top5",
      "name": "top5",
      "component": Top5
    },
    {
      "path": "/register",
      "name": "register",
      "component": Register
    },
    {
      "path": "/login",
      "name": "login",
      "component": Login
    }
  ],
})

export default router
