<script setup>
import { useUserStore } from '@/stores/user';
import { onBeforeMount, onMounted, watch, ref } from 'vue';
const userStore = useUserStore();
let items = ref([]);
onBeforeMount(async () => {
  const response = await fetch("http://localhost:5190/api/category/all");
  if (response.ok) {
    items.value = await response.json();
    console.log("Fetched items:", items);
  } else {
    console.error("Failed to fetch items:", response.statusText);
  }
})
</script>   

<template>
  <h1>Categories</h1>
  <article v-for="item in items" :key="item.id">
    <h2><router-link :to="{ name: 'category', params: { category: item.name }}">{{ item.name }}</router-link></h2>
    <p>{{ item.description }}</p>
  </article>
</template>

<style scoped></style>
