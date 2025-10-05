import Home from '@/views/Home.vue'
import Register from '@/views/Register.vue'
import Login from '@/views/Login.vue'
import Top5 from '@/views/Top5.vue'

import { createRouter, createWebHistory } from 'vue-router'
import Profile from '@/views/Profile.vue'
import Categories from '@/views/Categories.vue'
import CreateCategory from '@/views/editing/CreateCategory.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      "path": "/",
      "name": "home",
      "component": Home
    },
    {
      "path": "/categories",
      "name": "categories",
      "component": Categories
    },
    {
      "path": "/categories/:category",
      "name": "category",
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
    },
    {
      "path": "/profile",
      "name": "profile",
      "component": Profile
    },
    {
      "path": "/profile/categories/create",
      "name": "create-category",
      "component": CreateCategory
    }
  ],
})

export default router
