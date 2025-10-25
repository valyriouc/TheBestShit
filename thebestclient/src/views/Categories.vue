<script setup>
import { ApiHelper } from '@/api/apiHelper';
import { onBeforeMount, ref } from 'vue';

let items = ref([]);
onBeforeMount(async () => {
  try {
    const json = await ApiHelper.authorizedFetch("/api/category/all", {
      method: 'GET'
    });
    console.log("Fetched JSON:", json);
    items.value = json;
  } catch (error) {
    console.error("Failed to fetch categories:", error);
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
