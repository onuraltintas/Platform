import { TestBed } from '@angular/core/testing';
import { TranslationService, Language } from './translation.service';

describe('TranslationService', () => {
  let service: TranslationService;
  let localStorageMock: any;

  beforeEach(() => {
    localStorageMock = {
      getItem: jest.fn(),
      setItem: jest.fn(),
      removeItem: jest.fn(),
      clear: jest.fn()
    };

    Object.defineProperty(window, 'localStorage', {
      value: localStorageMock
    });

    TestBed.configureTestingModule({});
    service = TestBed.inject(TranslationService);
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should initialize with Turkish as default language', () => {
    expect(service.language()).toBe('tr');
  });

  describe('setLanguage', () => {
    it('should change language to English', () => {
      service.setLanguage('en');
      expect(service.language()).toBe('en');
    });

    it('should change language to Turkish', () => {
      service.setLanguage('tr');
      expect(service.language()).toBe('tr');
    });

    it('should save language preference to localStorage', () => {
      service.setLanguage('en');
      expect(localStorageMock.setItem).toHaveBeenCalledWith('app_language', 'en');
    });
  });

  describe('translate', () => {
    it('should return translation for existing key', () => {
      const result = service.translate('common.save');
      expect(result).toBe('Kaydet'); // Turkish default
    });

    it('should return key when translation not found', () => {
      const result = service.translate('non.existing.key');
      expect(result).toBe('non.existing.key');
    });

    it('should interpolate parameters', () => {
      const result = service.translate('validation.minLength', { min: 5 });
      expect(result).toContain('5');
    });

    it('should return nested translations', () => {
      const result = service.translate('menu.dashboard');
      expect(result).toBe('Kontrol Paneli');
    });
  });

  describe('t (alias)', () => {
    it('should work as alias for translate', () => {
      const translateResult = service.translate('common.save');
      const aliasResult = service.t('common.save');
      expect(aliasResult).toBe(translateResult);
    });
  });

  describe('getAvailableLanguages', () => {
    it('should return Turkish and English options', () => {
      const languages = service.getAvailableLanguages();
      expect(languages).toHaveLength(2);
      expect(languages).toContainEqual({ code: 'tr', label: 'Türkçe' });
      expect(languages).toContainEqual({ code: 'en', label: 'English' });
    });
  });

  describe('hasTranslation', () => {
    it('should return true for existing translation', () => {
      const result = service.hasTranslation('common.save');
      expect(result).toBe(true);
    });

    it('should return false for non-existing translation', () => {
      const result = service.hasTranslation('non.existing.key');
      expect(result).toBe(false);
    });
  });

  describe('language switching', () => {
    it('should provide different translations for different languages', () => {
      // Turkish
      service.setLanguage('tr');
      const trSave = service.translate('common.save');
      expect(trSave).toBe('Kaydet');

      // English
      service.setLanguage('en');
      const enSave = service.translate('common.save');
      expect(enSave).toBe('Save');

      expect(trSave).not.toBe(enSave);
    });
  });

  describe('authentication translations', () => {
    it('should translate auth keys correctly in Turkish', () => {
      service.setLanguage('tr');
      expect(service.translate('auth.login')).toBe('Giriş Yap');
      expect(service.translate('auth.logout')).toBe('Çıkış Yap');
      expect(service.translate('auth.email')).toBe('E-posta');
      expect(service.translate('auth.password')).toBe('Şifre');
    });

    it('should translate auth keys correctly in English', () => {
      service.setLanguage('en');
      expect(service.translate('auth.login')).toBe('Login');
      expect(service.translate('auth.logout')).toBe('Logout');
      expect(service.translate('auth.email')).toBe('Email');
      expect(service.translate('auth.password')).toBe('Password');
    });
  });

  describe('menu translations', () => {
    it('should translate menu items correctly', () => {
      service.setLanguage('tr');
      expect(service.translate('menu.dashboard')).toBe('Kontrol Paneli');
      expect(service.translate('menu.users')).toBe('Kullanıcılar');
      expect(service.translate('menu.speedReading')).toBe('Hızlı Okuma');
    });
  });

  describe('validation translations', () => {
    it('should translate validation messages with parameters', () => {
      service.setLanguage('tr');
      const result = service.translate('validation.minLength', { min: 8 });
      expect(result).toBe('En az 8 karakter olmalıdır');
    });

    it('should handle missing parameters gracefully', () => {
      service.setLanguage('tr');
      const result = service.translate('validation.minLength');
      expect(result).toContain('{{min}}'); // Parameter not replaced
    });
  });

  describe('error handling', () => {
    it('should handle deeply nested translation keys', () => {
      const result = service.translate('speedReading.texts.difficulty');
      expect(result).toBe('Zorluk');
    });

    it('should return original key for malformed keys', () => {
      const result = service.translate('');
      expect(result).toBe('');
    });
  });
});