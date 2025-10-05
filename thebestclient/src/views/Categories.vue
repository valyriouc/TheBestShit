<script setup>
import { onBeforeMount, onMounted, watch, ref } from 'vue';

let items = ref([]);
onBeforeMount(async () => {
  const response = await fetch("/api/category/all");
  if (response.ok) {
    const json = await response.json();
    console.log("Fetched JSON:", json);
    items.value = json;
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
