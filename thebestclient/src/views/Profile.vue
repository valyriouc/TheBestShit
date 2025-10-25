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
            { method: 'GET' }
        );
        console.log('Fetched categories response:', fetchedCategories);

        categories.value = fetchedCategories;
        console.log('Fetched categories:', categories.value);

        const fetchedSections = await ApiHelper.authorizedFetch(
            '/api/section/my',
            { method: 'GET' }
        )

        sections.value = fetchedSections;
        console.log('Fetched sections:', sections.value);

        const fetchedResources = await ApiHelper.authorizedFetch(
            '/api/resource/my',
            { method: 'GET' }
        )

        resources.value = fetchedResources;
        console.log('Fetched resources:', resources.value);
    } catch (error) {
        console.error('Error fetching data:', error);
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
                <tbody>
                    <tr v-for="section in sections" :key="section.id">
                        <td>{{ section.name }}</td>
                        <td>{{ section.description }}</td>
                        <td><button class="btn btn-secondary">Edit</button></td>
                        <td><button class="btn btn-danger">Delete</button></td>
                    </tr>
                </tbody>
            </table>
        </div>
         <div class="table-container">
            <h2>Resources</h2>
            <table class="table">
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>URL</th>
                        <th>Edit</th>
                        <th>Delete</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-for="resource in resources" :key="resource.id">
                        <td>{{ resource.name }}</td>
                        <td><a :href="resource.url" target="_blank">{{ resource.url }}</a></td>
                        <td><button class="btn btn-secondary">Edit</button></td>
                        <td><button class="btn btn-danger">Delete</button></td>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>
</template>

<style scoped>

</style>