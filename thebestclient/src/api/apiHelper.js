import { useUserStore } from '@/stores/user';
import router from '../router';

export class ApiHelper {
    static baseUrl = 'http://localhost:5190';

    static async authorizedFetch(url, options) {
        const userStore = useUserStore();
        const response = await fetch(`${this.baseUrl}${url}`, options);
            if (response.status === 401) {
                await userStore.logoutAsync();
                router.push('/login');
            }
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            return await response.json();
    }
}