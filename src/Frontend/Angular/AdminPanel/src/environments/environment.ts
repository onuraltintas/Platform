export const environment = {
  production: false,
  apiGateway: 'http://localhost:5001/api/v1',

  endpoints: {
    auth: '/auth',
    users: '/users',
    roles: '/roles',
    permissions: '/permissions',
    groups: '/groups',
    userProfile: '/user-profiles',
    speedReading: '/exercises',
    audit: '/audit'
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

  company: {
    name: 'OnAl Otomasyon',
    email: 'info@onalotomasyon.com',
    phone: '+90 (212) 555-0123',
    address: 'İstanbul, Türkiye',
    website: 'https://www.onalotomasyon.com'
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