<script setup>
import { useUserStore } from '../stores/user';
    const userStore = useUserStore();

</script>

<template>
  <h1>Home Page</h1>
  <div v-if="userStore.isLoggedIn">
    <p>Welcome back, {{userStore.username }}!</p>
  </div>
</template>

<style scoped></style>
