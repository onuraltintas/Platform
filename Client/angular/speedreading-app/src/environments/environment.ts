export const environment = {
  production: false,
  apiUrl: '/api',  // Uses Angular proxy to API Gateway
  googleClientId: '419291322983-k4rl6v4set9caq2p3prk0q6hd03hngik.apps.googleusercontent.com',
  googleRedirectUri: 'http://localhost:4202/auth/google/callback',  // Frontend port
  backendGoogleRedirectUri: 'http://localhost:5002/api/auth/google/callback'
};