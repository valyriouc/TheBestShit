<script setup>
    import { ApiHelper } from '@/api/apiHelper';
import router from '@/router';
import { useUserStore } from '@/stores/user';
import { ref } from 'vue';

    let categoryName = ref('');
    let categoryDescription = ref('');
    let allowPublicEdit = ref(false);

    const userStore = useUserStore();

    async function onMounted() {
        await userStore.checkAuthAsync();
    }

    async function onButtonClick() {
        const payload = {
            name: categoryName.value,
            description: categoryDescription.value,
            publicEdit: allowPublicEdit.value
        };

        try {
            const response = await ApiHelper.authorizedFetch(
                '/api/section/create/',
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': `${userStore.getAuth?.tokenType} ${userStore.getAuth?.accessToken}`
                    },
                    body: JSON.stringify(payload)
                }
            );

            alert(JSON.stringify(response));
            router.push('/profile');

        } catch (error) {
            console.error("Error creating category:", error);
        }
    }

    onMounted(onMounted);

</script>

<template>
    <h1>Create Category</h1>
    <p>This is where you can create a new category.</p>

    <div>
        <input type="text" placeholder="Name" v-model="categoryName" /><br />
        <textarea placeholder="Description" v-model="categoryDescription"></textarea><br />
        <label for="public-edit">Allow public editing</label>
        <input type="checkbox" v-model="allowPublicEdit" id="public-edit" />
        <button @click.prevent="onButtonClick" class="btn btn-primary">Create</button>
    </div>

</template>

<style scoped>

</style>