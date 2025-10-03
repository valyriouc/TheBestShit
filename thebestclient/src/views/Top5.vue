<script setup>
    import { useRoute } from 'vue-router';

    const route = useRoute().params.category;
</script>

<template>
    <h1>Top 5 - {{ route}}</h1>
</template>
<style scoped>
</style>