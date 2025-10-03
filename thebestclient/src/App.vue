<script setup>
import router from './router';
import { useUserStore } from './stores/user';

let userStore = useUserStore();
userStore.verifyAuth();

async function logoutAsync() {
    try {
      await userStore.logoutAsync();
      router.push('/login');
    } catch (error) {
      console.error("Logout failed:", error);
    }
}
</script>

<template>
  <nav class="navbar navbar-expand-lg navbar-dark bg-dark">
    <div class="collapse navbar-collapse" id="navbarNavDropdown">
        <ul class="navbar-nav">
        <li class="nav-item"><router-link class="nav-link" to="/">Home</router-link></li>
        <li class="nav-item"><router-link class="nav-link" to="/categories">Categories</router-link></li>
        <template v-if="userStore.isLoggedIn">
          <li class="nav-item"><router-link class="nav-link" to="/profile">Profile</router-link></li>
          <li class="nav-item"><a class="nav-link" href="logout" @click.prevent="logoutAsync">Logout</a></li>
        </template>
        <template v-else>
          <li class="nav-item"><router-link class="nav-link" to="/login">Login</router-link></li>
          <li class="nav-item"><router-link class="nav-link" to="/register">Register</router-link></li>
        </template>
      </ul>
    </div>
    </nav>
  <div class="container">
    <router-view></router-view>
  </div>
</template>

<style scoped>


</style>
