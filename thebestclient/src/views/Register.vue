<script setup>
import router from '@/router';
import { ref } from 'vue';

const email = ref('');
const username = ref('');
const password = ref('');

const register = async () => {
    // Handle registration logic here

    if (email.value === '' || username.value === '' || password.value === '') {
        alert('Please fill in all fields');
        return;
    }

    try {
        const response = await fetch('http://localhost:5190/register', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                email: email.value,
                userName: username.value,
                password: password.value
            })
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || 'Registration failed');
        }

        alert('Registration successful! You can now log in.');
        router.push("/login");
    } catch (error) {
        alert('Registration failed: ' + error.message);
    }
}

</script>

<template>
    <h1>Register</h1>
    <input v-model="email" type="text" placeholder="Email"/><br/>
    <input v-model="username" type="text" placeholder="Username"/><br/>
    <input v-model="password" type="password" placeholder="Password"/><br/>
    <p>Already have an account? <router-link to="/login">Login</router-link></p>
    <button @click="register" class="btn btn-primary">Register</button>
</template>

<style scoped>

</style>