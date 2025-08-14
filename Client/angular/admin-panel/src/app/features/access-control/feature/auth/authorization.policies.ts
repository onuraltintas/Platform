export interface AuthorizationPolicy {
  match: RegExp; // URL path regex, e.g. /^\/users(\/.*)?$/
  requiredRoles?: string[];
  requiredPermissions?: string[];
}

// Merkezi yetkilendirme politikaları
export const AUTHORIZATION_POLICIES: AuthorizationPolicy[] = [
  // Kullanıcı yönetimi
  { match: /^\/users$/, requiredRoles: ['Admin', 'Support'], requiredPermissions: ['Users.Read'] },
  { match: /^\/users\/(new|[^/]+)$/, requiredRoles: ['Admin'], requiredPermissions: ['Users.Write'] },

  // Roller ve izinler
  { match: /^\/roles(\/.*)?$/, requiredRoles: ['Admin'], requiredPermissions: ['Users.ManageRoles'] },
  { match: /^\/permissions(\/.*)?$/, requiredRoles: ['Admin'], requiredPermissions: ['Users.ManagePermissions'] },

  // Kategoriler
  { match: /^\/categories(\/.*)?$/, requiredRoles: ['Admin'], requiredPermissions: ['Content.ManageLibrary'] },

  // Raporlar (ileride kullanılırsa örnek)
  { match: /^\/reports(\/.*)?$/, requiredRoles: ['Admin', 'Moderator', 'Support'], requiredPermissions: ['Reports.View'] }
  ,
  // Hızlı Okuma (Speed Reading) yönetimi
  { match: /^\/sr\/texts(\/.*)?$/, requiredRoles: ['Admin'], requiredPermissions: ['sr.content.manage'] },
  { match: /^\/sr\/exercises(\/.*)?$/, requiredRoles: ['Admin'], requiredPermissions: ['sr.content.manage'] },
  { match: /^\/sr\/questions(\/.*)?$/, requiredRoles: ['Admin'], requiredPermissions: ['sr.content.manage'] },
  { match: /^\/sr\/levels(\/.*)?$/, requiredRoles: ['Admin'], requiredPermissions: ['sr.content.manage'] },
  { match: /^\/sr\/profiles(\/.*)?$/, requiredRoles: ['Admin'], requiredPermissions: ['sr.profile.manage'] },
  { match: /^\/sr\/reports(\/.*)?$/, requiredRoles: ['Admin', 'Support'], requiredPermissions: ['sr.progress.read.all'] }
];

