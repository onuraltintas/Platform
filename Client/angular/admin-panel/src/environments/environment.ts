export const environment = {
  production: false,
  apiUrl: '/api', // proxy.conf.json ile dev ortamında gateway'e yönlenir
  identityApiUrl: 'http://localhost:5002/api',
  userApiUrl: 'http://localhost:5004/api', 
  notificationApiUrl: 'http://localhost:5006/api',
  featureFlagApiUrl: 'http://localhost:5008/api',
  googleClientId: '419291322983-k4rl6v4set9caq2p3prk0q6hd03hngik.apps.googleusercontent.com',
  googleRedirectUri: 'http://localhost:4200/auth/google/callback',
  backendGoogleRedirectUri: 'http://localhost:5002/api/auth/google/callback'
};