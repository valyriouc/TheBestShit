<script setup>
import { ApiHelper } from '@/api/apiHelper';
import { useUserStore } from '@/stores/user';
import { onMounted, ref } from 'vue';

const userStore = useUserStore();
let categories = ref([]);

async function fetchCategories() {
    try {
        const fetchedCategories = await ApiHelper.authorizedFetch(
            '/api/category/my',
            {
                method: 'GET',
                headers: {
                    'Authorization': `${userStore.getAuth?.tokenType} ${userStore.getAuth?.accessToken}`
                }
            }
        );
        categories.value = fetchedCategories;
        console.log('Fetched categories:', categories.value);
    } catch (error) {
        console.error('Error fetching categories:', error);
    }
}

onMounted(async () => {
    await fetchCategories();
});

</script>

<template>
    <h1>Profile - {{ userStore.getUser }}</h1>
    <div> 
        <router-link to="/profile/categories/create">Create category</router-link><br />
        <router-link to="/profile/section/create">Create section</router-link><br />
        <router-link to="/profile/resource/create">Create resource</router-link><br />

    </div>
    
</template>

<style scoped>

</style>