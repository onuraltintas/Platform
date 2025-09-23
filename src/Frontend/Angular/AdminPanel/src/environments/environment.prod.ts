export const environment = {
  production: true,
  apiGateway: 'https://api.platformv1.com/api/v1',  // Production URL

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
    secure: true
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
    enableMFA: true
  },

  oauth: {
    google: {
      clientId: '419291322983-k4rl6v4set9caq2p3prk0q6hd03hngik.apps.googleusercontent.com',
      redirectUri: 'https://platform.yourdomain.com/auth/google/callback',
      backendRedirectUri: 'https://api.yourdomain.com/api/v1/auth/google/callback',
      scopes: ['openid', 'profile', 'email']
    }
  }
};