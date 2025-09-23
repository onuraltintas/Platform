import { Pipe, PipeTransform } from '@angular/core';
import { Observable, of } from 'rxjs';

// Basit çeviri servisi interface'i
interface TranslationService {
  get(key: string, params?: Record<string, unknown>): Observable<string>;
  instant(key: string, params?: Record<string, unknown>): string;
}

// Basit çeviri servisi implementasyonu
class SimpleTranslationService implements TranslationService {
  private translations: { [key: string]: string } = {
    // Genel
    'common.save': 'Kaydet',
    'common.cancel': 'İptal',
    'common.delete': 'Sil',
    'common.edit': 'Düzenle',
    'common.view': 'Görüntüle',
    'common.add': 'Ekle',
    'common.search': 'Ara',
    'common.filter': 'Filtrele',
    'common.export': 'Dışa Aktar',
    'common.import': 'İçe Aktar',
    'common.refresh': 'Yenile',
    'common.loading': 'Yükleniyor...',
    'common.no-data': 'Veri bulunamadı',
    'common.error': 'Hata',
    'common.success': 'Başarılı',
    'common.warning': 'Uyarı',
    'common.info': 'Bilgi',
    'common.yes': 'Evet',
    'common.no': 'Hayır',
    'common.ok': 'Tamam',
    'common.close': 'Kapat',
    'common.back': 'Geri',
    'common.next': 'İleri',
    'common.previous': 'Önceki',
    'common.first': 'İlk',
    'common.last': 'Son',
    'common.page': 'Sayfa',
    'common.of': '/',
    'common.total': 'Toplam',
    'common.selected': 'Seçili',
    'common.all': 'Tümü',
    'common.none': 'Hiçbiri',

    // Formlar
    'form.required': 'Bu alan zorunludur',
    'form.email': 'Geçerli bir e-posta adresi giriniz',
    'form.min-length': 'En az {{min}} karakter olmalıdır',
    'form.max-length': 'En fazla {{max}} karakter olmalıdır',
    'form.pattern': 'Geçersiz format',
    'form.number': 'Sayısal değer giriniz',
    'form.date': 'Geçerli bir tarih giriniz',
    'form.password-mismatch': 'Şifreler eşleşmiyor',

    // Kullanıcı Yönetimi
    'user.title': 'Kullanıcı Yönetimi',
    'user.list': 'Kullanıcı Listesi',
    'user.add': 'Kullanıcı Ekle',
    'user.edit': 'Kullanıcı Düzenle',
    'user.delete': 'Kullanıcı Sil',
    'user.name': 'Kullanıcı Adı',
    'user.email': 'E-posta',
    'user.role': 'Rol',
    'user.status': 'Durum',
    'user.created-at': 'Oluşturulma Tarihi',
    'user.last-login': 'Son Giriş',

    // Roller
    'role.title': 'Rol Yönetimi',
    'role.list': 'Rol Listesi',
    'role.add': 'Rol Ekle',
    'role.edit': 'Rol Düzenle',
    'role.delete': 'Rol Sil',
    'role.name': 'Rol Adı',
    'role.description': 'Açıklama',
    'role.permissions': 'Yetkiler',

    // Gruplar
    'group.title': 'Grup Yönetimi',
    'group.list': 'Grup Listesi',
    'group.add': 'Grup Ekle',
    'group.edit': 'Grup Düzenle',
    'group.delete': 'Grup Sil',
    'group.name': 'Grup Adı',
    'group.description': 'Açıklama',
    'group.members': 'Üyeler',

    // İzinler
    'permission.title': 'İzin Yönetimi',
    'permission.list': 'İzin Listesi',
    'permission.name': 'İzin Adı',
    'permission.description': 'Açıklama',
    'permission.resource': 'Kaynak',
    'permission.action': 'Eylem',

    // Dashboard
    'dashboard.title': 'Dashboard',
    'dashboard.welcome': 'Hoş Geldiniz',
    'dashboard.users': 'Kullanıcılar',
    'dashboard.roles': 'Roller',
    'dashboard.groups': 'Gruplar',
    'dashboard.permissions': 'İzinler',

    // Menü
    'menu.dashboard': 'Dashboard',
    'menu.users': 'Kullanıcılar',
    'menu.roles': 'Roller',
    'menu.groups': 'Gruplar',
    'menu.permissions': 'İzinler',
    'menu.settings': 'Ayarlar',
    'menu.profile': 'Profil',
    'menu.logout': 'Çıkış',

    // Mesajlar
    'message.save-success': 'Başarıyla kaydedildi',
    'message.save-error': 'Kaydetme sırasında hata oluştu',
    'message.delete-success': 'Başarıyla silindi',
    'message.delete-error': 'Silme sırasında hata oluştu',
    'message.delete-confirmation': 'Bu öğeyi silmek istediğinizden emin misiniz?',
    'message.unsaved-changes': 'Kaydedilmemiş değişiklikleriniz var',
    'message.operation-success': 'İşlem başarıyla tamamlandı',
    'message.operation-error': 'İşlem sırasında hata oluştu',

    // Durum
    'status.active': 'Aktif',
    'status.inactive': 'Pasif',
    'status.pending': 'Beklemede',
    'status.approved': 'Onaylandı',
    'status.rejected': 'Reddedildi',
    'status.draft': 'Taslak',
    'status.published': 'Yayınlandı',

    // Tarih
    'date.today': 'Bugün',
    'date.yesterday': 'Dün',
    'date.tomorrow': 'Yarın',
    'date.this-week': 'Bu Hafta',
    'date.last-week': 'Geçen Hafta',
    'date.this-month': 'Bu Ay',
    'date.last-month': 'Geçen Ay',

    // Dosya
    'file.upload': 'Dosya Yükle',
    'file.download': 'Dosya İndir',
    'file.delete': 'Dosya Sil',
    'file.size': 'Dosya Boyutu',
    'file.type': 'Dosya Türü',
    'file.invalid-type': 'Geçersiz dosya türü',
    'file.too-large': 'Dosya çok büyük',

    // Sayfalama
    'pagination.items-per-page': 'Sayfa başına öğe',
    'pagination.next-page': 'Sonraki sayfa',
    'pagination.previous-page': 'Önceki sayfa',
    'pagination.first-page': 'İlk sayfa',
    'pagination.last-page': 'Son sayfa',
    'pagination.page-info': '{{total}} öğeden {{start}}-{{end}} arası'
  };

  get(key: string, params?: Record<string, unknown>): Observable<string> {
    return of(this.instant(key, params));
  }

  instant(key: string, params?: Record<string, unknown>): string {
    let translation = this.translations[key] || key;

    // Parametreleri yerine koy
    if (params) {
      Object.keys(params).forEach(param => {
        const regex = new RegExp(`{{${param}}}`, 'g');
        translation = translation.replace(regex, params[param]);
      });
    }

    return translation;
  }
}

@Pipe({
  name: 'translate',
  standalone: true,
  pure: false // Observable değişiklikleri için
})
export class TranslatePipe implements PipeTransform {
  // Basit çeviri servisi
  private readonly translationService = new SimpleTranslationService();
  private lastKey = '';
  private lastValue = '';

  transform(key: string, params?: Record<string, unknown>): string {
    if (!key) {
      return '';
    }

    // Cache için basit kontrol
    const cacheKey = `${key}_${JSON.stringify(params)}`;
    if (this.lastKey === cacheKey) {
      return this.lastValue;
    }

    const translation = this.translationService.instant(key, params);

    this.lastKey = cacheKey;
    this.lastValue = translation;

    return translation;
  }
}