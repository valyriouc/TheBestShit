<script setup>
import { ApiHelper } from '@/api/apiHelper';
import { useUserStore } from '@/stores/user';
import { onMounted, ref } from 'vue';

const userStore = useUserStore();
let categories = ref([]);
let sections = ref([]);
let resources = ref([]);

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
        console.log('Fetched categories response:', fetchedCategories);

            categories.value = fetchedCategories;
        console.log('Fetched categories:', categories.value);

        // const fetchedSections = await ApiHelper.authorizedFetch(
        //     '/api/section/my',
        //     {
        //         method: 'GET',
        //         headers: {
        //             'Authorization': `${userStore.getAuth?.tokenType} ${userStore.getAuth?.accessToken}`
        //         }
        //     }
        // )

        // sections.value = fetchedSections;
        // console.log('Fetched sections:', sections.value);

        // const fetchedResources = await ApiHelper.authorizedFetch(
        //     '/api/resource/my',
        //     {
        //         method: 'GET',
        //         headers: {
        //             'Authorization': `${userStore.getAuth?.tokenType} ${userStore.getAuth?.accessToken}`
        //         }
        //     }
        // )

        // resources.value = fetchedResources;
        console.log('Fetched resources:', resources.value);
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
    <div class="container">
        <div class="table-container"> 
            <h2>Categories</h2>
            <table class="table">
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Description</th>
                        <th>Edit</th>
                        <th>Delete</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="category in categories" :key="category.id">
                        <td>{{ category.name }}</td>
                        <td>{{ category.description }}</td>
                        <td><button class="btn btn-secondary">Edit</button></td>
                        <td><button class="btn btn-danger">Delete</button></td>
                    </tr>
                </tbody>
            </table>
        </div>
        <div class="table-container"> 
            <h2>Sections</h2>
            <table class="table">
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Description</th>
                        <th>Edit</th>
                        <th>Delete</th>
                    </tr>
                </thead>
            </table>
        </div>
    </div>
</template>

<style scoped>

</style>