import { defineStore } from 'pinia'

export const useUserStore = defineStore('user', {
  state: () => ({ username: '', tokenType: null, accessToken: null, refreshToken: null, loggedIn: false}),
  getters: {
    isLoggedIn: (state) => state.loggedIn,
    getUser: (state) => state.username,
    getAuth: (state) => ({ tokenType: state.tokenType, accessToken: state.accessToken, refreshToken: state.refreshToken }),
  },
  actions: {
    async loginAsync(credentials) {
      try {
        const response = await fetch('http://localhost:5190/login', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(credentials)
        })

        const data = await response.json()

        localStorage.setItem('auth', JSON.stringify(data))

        this.accessToken = data.accessToken;
        this.refreshToken = data.refreshToken;
        this.tokenType = data.tokenType;
        this.loggedIn = true;

        const userResponse = await fetch('http://localhost:5190/api/user/me', {
          method: 'GET',
          headers: { 'Authorization': `${data.tokenType} ${data.accessToken}` }
        })

        const userData = await userResponse.json()
        localStorage.setItem('user', JSON.stringify(userData))
        this.username = userData.userName;

      } catch (error) {
        throw new Error(error)
      }
    },
    async logoutAsync() {
        localStorage.removeItem('auth')
        localStorage.removeItem('user')
        this.username = null
        this.accessToken = null
        this.refreshToken = null
        this.tokenType = null
        this.loggedIn = false
    },
    verifyAuth() {
      const auth = localStorage.getItem('auth')
      const user = localStorage.getItem('user')
      if (auth && user) {
        this.tokenType = JSON.parse(auth).tokenType;
        this.accessToken = JSON.parse(auth).accessToken;
        this.refreshToken = JSON.parse(auth).refreshToken;
        this.username = JSON.parse(user).userName;
        this.loggedIn = true
      }

      return this.loggedIn;
    }
  }
})