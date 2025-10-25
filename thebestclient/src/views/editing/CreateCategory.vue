<script setup>
import { ApiHelper } from '@/api/apiHelper';
import router from '@/router';
import { useUserStore } from '@/stores/user';
import { ref, onMounted } from 'vue';

let categoryName = ref('');
let categoryDescription = ref('');

const userStore = useUserStore();

async function checkAuth() {
    await userStore.checkAuthAsync();
}

async function createCategory() {
    const transferObject = {
        name: categoryName.value,
        description: categoryDescription.value
    };

    try {
        const response = await ApiHelper.authorizedFetch(
            '/api/category/create',
            {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(transferObject)
            }
        );

        console.log('Category created:', response);
        router.push('/profile');

    } catch (error) {
        console.error("Error creating category:", error);
        alert('Failed to create category: ' + error.message);
    }
}

onMounted(checkAuth);

</script>

<template>
    <h1>Create Category</h1>
    <p>This is where you can create a new category.</p>

    <div>
        <input type="text" placeholder="Name" v-model="categoryName" /><br />
        <textarea placeholder="Description" v-model="categoryDescription"></textarea><br />
        <button @click.prevent="createCategory" class="btn btn-primary">Create</button>
    </div>

</template>

<style scoped>

</style>