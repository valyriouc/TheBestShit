import { useUserStore } from '@/stores/user';
import router from '../router';

export class ApiHelper {
    static baseUrl = 'http://localhost:5190';

    static async authorizedFetch(url, options = {}) {
        const userStore = useUserStore();

        // Ensure headers object exists
        if (!options.headers) {
            options.headers = {};
        }

        // Add Authorization header if user is logged in
        if (userStore.isLoggedIn && userStore.getAuth) {
            options.headers['Authorization'] = `${userStore.getAuth.tokenType} ${userStore.getAuth.accessToken}`;
        }

        const response = await fetch(`${this.baseUrl}${url}`, options);

        if (response.status === 401) {
            await userStore.logoutAsync();
            router.push('/login');
            throw new Error('Unauthorized');
        }

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        return await response.json();
    }
}