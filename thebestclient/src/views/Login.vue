<script setup>
import { ref } from 'vue';
import { useUserStore } from '../stores/user';
import router from '../router'

const userStore = useUserStore();

if (userStore.verifyAuth()) {
    router.push('/'); // Redirect to home if already logged in
}

const email = ref('');
const password = ref('');

async function login() {
    if (email.value === '' || password.value === '') {
        alert('Please fill in all fields');
        return;
    }

    try {
        await userStore.loginAsync({
        email: email.value,
        password: password.value
    });
    console.log('Login successful:', userStore.getUser, userStore.getAuth);
    
        router.push('/profile'); // Redirect to home page after login
    } catch (error) {
        alert('Login failed: ' + error.message);    
    }
}
</script>

<template>
    <h1>Login</h1>
    <div>
        <input v-model="email" type="text" placeholder="Email" /> 
        <input v-model="password" type="password" placeholder="Password" />
        <button @click="login">Login</button>
    </div>
</template>

<style scoped>

</style>
