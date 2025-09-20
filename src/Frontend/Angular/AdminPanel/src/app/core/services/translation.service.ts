import { Injectable, signal, computed, effect } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeEn from '@angular/common/locales/en';
import localeTr from '@angular/common/locales/tr';

export type Language = 'tr' | 'en';

interface TranslationSet {
  [key: string]: string | TranslationSet;
}

@Injectable({
  providedIn: 'root'
})
export class TranslationService {
  private currentLanguage = signal<Language>('tr');
  private translations = signal<TranslationSet>({});

  language = computed(() => this.currentLanguage());

  private readonly STORAGE_KEY = 'app_language';

  constructor() {
    // Register locales
    registerLocaleData(localeTr);
    registerLocaleData(localeEn);

    // Load saved language or default
    const savedLanguage = localStorage.getItem(this.STORAGE_KEY) as Language;
    if (savedLanguage && (savedLanguage === 'tr' || savedLanguage === 'en')) {
      this.currentLanguage.set(savedLanguage);
    }

    // Load translations
    this.loadTranslations(this.currentLanguage());

    // Save language preference when changed
    effect(() => {
      const lang = this.currentLanguage();
      localStorage.setItem(this.STORAGE_KEY, lang);
      document.documentElement.lang = lang;
    });
  }

  /**
   * Set the current language
   */
  setLanguage(language: Language): void {
    if (this.currentLanguage() !== language) {
      this.currentLanguage.set(language);
      this.loadTranslations(language);
    }
  }

  /**
   * Get translation for a key
   */
  translate(key: string, params?: { [key: string]: any }): string {
    const translation = this.getNestedTranslation(key);

    if (!translation) {
      console.warn(`Translation not found for key: ${key}`);
      return key;
    }

    // Replace parameters if provided
    if (params && typeof translation === 'string') {
      return this.interpolate(translation, params);
    }

    return typeof translation === 'string' ? translation : key;
  }

  /**
   * Alias for translate
   */
  t(key: string, params?: { [key: string]: any }): string {
    return this.translate(key, params);
  }

  /**
   * Get available languages
   */
  getAvailableLanguages(): { code: Language; label: string }[] {
    return [
      { code: 'tr', label: 'Türkçe' },
      { code: 'en', label: 'English' }
    ];
  }

  /**
   * Check if a translation key exists
   */
  hasTranslation(key: string): boolean {
    return this.getNestedTranslation(key) !== null;
  }

  /**
   * Load translations for a language
   */
  private async loadTranslations(language: Language): Promise<void> {
    try {
      const translations = await import(`../../../assets/i18n/${language}.json`);
      this.translations.set(translations.default);
    } catch (error) {
      console.error(`Failed to load translations for ${language}:`, error);
      // Fallback to inline translations
      this.translations.set(this.getFallbackTranslations(language));
    }
  }

  /**
   * Get nested translation value
   */
  private getNestedTranslation(key: string): string | null {
    const keys = key.split('.');
    let current: any = this.translations();

    for (const k of keys) {
      if (current && typeof current === 'object' && k in current) {
        current = current[k];
      } else {
        return null;
      }
    }

    return typeof current === 'string' ? current : null;
  }

  /**
   * Interpolate parameters in translation string
   */
  private interpolate(text: string, params: { [key: string]: any }): string {
    return text.replace(/\{\{(\w+)\}\}/g, (match, key) => {
      return params[key] !== undefined ? params[key] : match;
    });
  }

  /**
   * Fallback translations when files cannot be loaded
   */
  private getFallbackTranslations(language: Language): TranslationSet {
    if (language === 'tr') {
      return {
        common: {
          save: 'Kaydet',
          cancel: 'İptal',
          delete: 'Sil',
          edit: 'Düzenle',
          create: 'Oluştur',
          update: 'Güncelle',
          search: 'Ara',
          filter: 'Filtre',
          export: 'Dışa Aktar',
          import: 'İçe Aktar',
          loading: 'Yükleniyor...',
          noData: 'Veri bulunamadı',
          actions: 'İşlemler',
          yes: 'Evet',
          no: 'Hayır',
          confirm: 'Onayla',
          close: 'Kapat',
          back: 'Geri',
          next: 'İleri',
          previous: 'Önceki',
          first: 'İlk',
          last: 'Son',
          page: 'Sayfa',
          of: '/',
          items: 'öğe',
          selected: 'seçili',
          all: 'Tümü',
          none: 'Hiçbiri',
          select: 'Seç',
          clear: 'Temizle',
          reset: 'Sıfırla',
          apply: 'Uygula',
          success: 'Başarılı',
          error: 'Hata',
          warning: 'Uyarı',
          info: 'Bilgi'
        },
        menu: {
          dashboard: 'Kontrol Paneli',
          users: 'Kullanıcılar',
          roles: 'Roller',
          permissions: 'İzinler',
          groups: 'Gruplar',
          speedReading: 'Hızlı Okuma',
          texts: 'Metinler',
          exercises: 'Egzersizler',
          progress: 'İlerleme',
          analytics: 'Analitik',
          settings: 'Ayarlar',
          profile: 'Profil',
          logout: 'Çıkış'
        },
        auth: {
          login: 'Giriş Yap',
          logout: 'Çıkış Yap',
          register: 'Kayıt Ol',
          forgotPassword: 'Şifremi Unuttum',
          resetPassword: 'Şifre Sıfırla',
          email: 'E-posta',
          password: 'Şifre',
          confirmPassword: 'Şifre Tekrar',
          rememberMe: 'Beni Hatırla',
          loginSuccess: 'Giriş başarılı',
          loginError: 'Giriş başarısız',
          logoutSuccess: 'Çıkış başarılı',
          invalidCredentials: 'Geçersiz kimlik bilgileri',
          accountLocked: 'Hesap kilitli',
          sessionExpired: 'Oturum süresi doldu'
        },
        user: {
          title: 'Kullanıcı Yönetimi',
          list: 'Kullanıcı Listesi',
          create: 'Yeni Kullanıcı',
          edit: 'Kullanıcı Düzenle',
          delete: 'Kullanıcı Sil',
          details: 'Kullanıcı Detayları',
          firstName: 'Ad',
          lastName: 'Soyad',
          email: 'E-posta',
          phone: 'Telefon',
          role: 'Rol',
          status: 'Durum',
          active: 'Aktif',
          inactive: 'Pasif',
          blocked: 'Engelli',
          createdAt: 'Oluşturma Tarihi',
          updatedAt: 'Güncelleme Tarihi',
          lastLogin: 'Son Giriş',
          permissions: 'İzinler',
          groups: 'Gruplar',
          actions: 'İşlemler'
        },
        speedReading: {
          title: 'Hızlı Okuma',
          texts: {
            title: 'Metin Kütüphanesi',
            list: 'Metin Listesi',
            create: 'Yeni Metin',
            edit: 'Metin Düzenle',
            delete: 'Metin Sil',
            content: 'İçerik',
            wordCount: 'Kelime Sayısı',
            difficulty: 'Zorluk',
            category: 'Kategori',
            tags: 'Etiketler',
            language: 'Dil',
            beginner: 'Başlangıç',
            intermediate: 'Orta',
            advanced: 'İleri',
            expert: 'Uzman'
          },
          exercises: {
            title: 'Egzersizler',
            list: 'Egzersiz Listesi',
            create: 'Yeni Egzersiz',
            edit: 'Egzersiz Düzenle',
            delete: 'Egzersiz Sil',
            type: 'Tip',
            duration: 'Süre',
            speed: 'Hız',
            wpm: 'DKK',
            questions: 'Sorular',
            settings: 'Ayarlar',
            preview: 'Önizleme',
            start: 'Başla',
            pause: 'Duraklat',
            resume: 'Devam Et',
            stop: 'Durdur',
            complete: 'Tamamla'
          },
          progress: {
            title: 'İlerleme Takibi',
            overview: 'Genel Bakış',
            statistics: 'İstatistikler',
            history: 'Geçmiş',
            achievements: 'Başarımlar',
            totalExercises: 'Toplam Egzersiz',
            completedExercises: 'Tamamlanan',
            averageWPM: 'Ortalama DKK',
            averageAccuracy: 'Ortalama Doğruluk',
            totalTime: 'Toplam Süre',
            streak: 'Seri',
            level: 'Seviye',
            points: 'Puan',
            rank: 'Sıralama'
          }
        },
        validation: {
          required: 'Bu alan zorunludur',
          email: 'Geçerli bir e-posta adresi giriniz',
          minLength: 'En az {{min}} karakter olmalıdır',
          maxLength: 'En fazla {{max}} karakter olmalıdır',
          pattern: 'Geçersiz format',
          passwordMatch: 'Şifreler eşleşmiyor',
          unique: 'Bu değer zaten kullanılıyor',
          number: 'Sayı olmalıdır',
          date: 'Geçerli bir tarih giriniz',
          url: 'Geçerli bir URL giriniz',
          phone: 'Geçerli bir telefon numarası giriniz'
        },
        messages: {
          saveSuccess: 'Başarıyla kaydedildi',
          saveError: 'Kaydetme başarısız',
          deleteSuccess: 'Başarıyla silindi',
          deleteError: 'Silme başarısız',
          updateSuccess: 'Başarıyla güncellendi',
          updateError: 'Güncelleme başarısız',
          loadError: 'Veri yükleme başarısız',
          confirmDelete: 'Silmek istediğinize emin misiniz?',
          confirmAction: 'Bu işlemi yapmak istediğinize emin misiniz?',
          operationSuccess: 'İşlem başarılı',
          operationError: 'İşlem başarısız',
          networkError: 'Ağ bağlantısı hatası',
          serverError: 'Sunucu hatası',
          unauthorized: 'Yetkisiz erişim',
          forbidden: 'Erişim reddedildi',
          notFound: 'Bulunamadı',
          timeout: 'İstek zaman aşımına uğradı'
        }
      };
    } else {
      // English translations
      return {
        common: {
          save: 'Save',
          cancel: 'Cancel',
          delete: 'Delete',
          edit: 'Edit',
          create: 'Create',
          update: 'Update',
          search: 'Search',
          filter: 'Filter',
          export: 'Export',
          import: 'Import',
          loading: 'Loading...',
          noData: 'No data found',
          actions: 'Actions',
          yes: 'Yes',
          no: 'No',
          confirm: 'Confirm',
          close: 'Close',
          back: 'Back',
          next: 'Next',
          previous: 'Previous',
          first: 'First',
          last: 'Last',
          page: 'Page',
          of: 'of',
          items: 'items',
          selected: 'selected',
          all: 'All',
          none: 'None',
          select: 'Select',
          clear: 'Clear',
          reset: 'Reset',
          apply: 'Apply',
          success: 'Success',
          error: 'Error',
          warning: 'Warning',
          info: 'Info'
        },
        menu: {
          dashboard: 'Dashboard',
          users: 'Users',
          roles: 'Roles',
          permissions: 'Permissions',
          groups: 'Groups',
          speedReading: 'Speed Reading',
          texts: 'Texts',
          exercises: 'Exercises',
          progress: 'Progress',
          analytics: 'Analytics',
          settings: 'Settings',
          profile: 'Profile',
          logout: 'Logout'
        },
        auth: {
          login: 'Login',
          logout: 'Logout',
          register: 'Register',
          forgotPassword: 'Forgot Password',
          resetPassword: 'Reset Password',
          email: 'Email',
          password: 'Password',
          confirmPassword: 'Confirm Password',
          rememberMe: 'Remember Me',
          loginSuccess: 'Login successful',
          loginError: 'Login failed',
          logoutSuccess: 'Logout successful',
          invalidCredentials: 'Invalid credentials',
          accountLocked: 'Account locked',
          sessionExpired: 'Session expired'
        },
        user: {
          title: 'User Management',
          list: 'User List',
          create: 'New User',
          edit: 'Edit User',
          delete: 'Delete User',
          details: 'User Details',
          firstName: 'First Name',
          lastName: 'Last Name',
          email: 'Email',
          phone: 'Phone',
          role: 'Role',
          status: 'Status',
          active: 'Active',
          inactive: 'Inactive',
          blocked: 'Blocked',
          createdAt: 'Created At',
          updatedAt: 'Updated At',
          lastLogin: 'Last Login',
          permissions: 'Permissions',
          groups: 'Groups',
          actions: 'Actions'
        },
        speedReading: {
          title: 'Speed Reading',
          texts: {
            title: 'Text Library',
            list: 'Text List',
            create: 'New Text',
            edit: 'Edit Text',
            delete: 'Delete Text',
            content: 'Content',
            wordCount: 'Word Count',
            difficulty: 'Difficulty',
            category: 'Category',
            tags: 'Tags',
            language: 'Language',
            beginner: 'Beginner',
            intermediate: 'Intermediate',
            advanced: 'Advanced',
            expert: 'Expert'
          },
          exercises: {
            title: 'Exercises',
            list: 'Exercise List',
            create: 'New Exercise',
            edit: 'Edit Exercise',
            delete: 'Delete Exercise',
            type: 'Type',
            duration: 'Duration',
            speed: 'Speed',
            wpm: 'WPM',
            questions: 'Questions',
            settings: 'Settings',
            preview: 'Preview',
            start: 'Start',
            pause: 'Pause',
            resume: 'Resume',
            stop: 'Stop',
            complete: 'Complete'
          },
          progress: {
            title: 'Progress Tracking',
            overview: 'Overview',
            statistics: 'Statistics',
            history: 'History',
            achievements: 'Achievements',
            totalExercises: 'Total Exercises',
            completedExercises: 'Completed',
            averageWPM: 'Average WPM',
            averageAccuracy: 'Average Accuracy',
            totalTime: 'Total Time',
            streak: 'Streak',
            level: 'Level',
            points: 'Points',
            rank: 'Rank'
          }
        },
        validation: {
          required: 'This field is required',
          email: 'Please enter a valid email address',
          minLength: 'Must be at least {{min}} characters',
          maxLength: 'Must be at most {{max}} characters',
          pattern: 'Invalid format',
          passwordMatch: 'Passwords do not match',
          unique: 'This value is already in use',
          number: 'Must be a number',
          date: 'Please enter a valid date',
          url: 'Please enter a valid URL',
          phone: 'Please enter a valid phone number'
        },
        messages: {
          saveSuccess: 'Successfully saved',
          saveError: 'Save failed',
          deleteSuccess: 'Successfully deleted',
          deleteError: 'Delete failed',
          updateSuccess: 'Successfully updated',
          updateError: 'Update failed',
          loadError: 'Failed to load data',
          confirmDelete: 'Are you sure you want to delete?',
          confirmAction: 'Are you sure you want to perform this action?',
          operationSuccess: 'Operation successful',
          operationError: 'Operation failed',
          networkError: 'Network connection error',
          serverError: 'Server error',
          unauthorized: 'Unauthorized access',
          forbidden: 'Access denied',
          notFound: 'Not found',
          timeout: 'Request timed out'
        }
      };
    }
  }
}