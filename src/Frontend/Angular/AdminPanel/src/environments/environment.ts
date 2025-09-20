export const environment = {
  production: false,
  apiGateway: 'http://localhost:5001/api/v1',

  endpoints: {
    auth: '/auth',
    users: '/identity/users',
    roles: '/identity/roles',
    permissions: '/identity/permissions',
    groups: '/identity/groups',
    userProfile: '/user/profile',
    speedReading: '/speed-reading',
    audit: '/identity/audit'
  },

  auth: {
    tokenKey: 'access_token',
    refreshKey: 'refresh_token',
    userKey: 'current_user',
    permissionsKey: 'user_permissions',
    tokenExpiry: 15 * 60 * 1000,              // 15 minutes
    refreshExpiry: 7 * 24 * 60 * 60 * 1000,   // 7 days
    refreshBeforeExpiry: 2 * 60 * 1000        // Refresh 2 minutes before expiry
  },

  storage: {
    prefix: 'platformv1_',
    secure: false
  },

  ui: {
    defaultLanguage: 'tr',
    availableLanguages: ['tr', 'en'],
    defaultTheme: 'light',
    pageSize: 10,
    pageSizeOptions: [10, 25, 50, 100]
  },

  features: {
    enableDarkMode: true,
    enableNotifications: true,
    enableAuditLog: true,
    enableGoogleAuth: true,
    enableMFA: false
  },

  oauth: {
    google: {
      clientId: '', // Will be loaded from backend or secrets.env
      redirectUri: 'http://localhost:4200/auth/google/callback',
      backendRedirectUri: 'http://localhost:5001/api/v1/auth/google/callback',
      scopes: ['openid', 'profile', 'email']
    }
  }
};