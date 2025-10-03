<script setup>
import { useUserStore } from './stores/user';

let userStore = useUserStore();
</script>

<template>
  <nav>
    <ul>
      <li><router-link to="/">Home</router-link></li>
      <li><router-link to="/top5">Top 5</router-link></li>
      <div v-if="userStore.isLoggedIn">
        <li><router-link to="/profile">{{userStore.username}}</router-link></li>
        <li><a href="#" @click="logout">Logout</a></li>
      </div>
      <div v-else> 
        <li><router-link to="/login">Login</router-link></li>
        <li><router-link to="/register">Register</router-link></li>
      </div>
    </ul>
  </nav>
  <router-view></router-view>
</template>

<style scoped>


</style>
