/**
 * Validator error messages for Turkish locale
 */
export const VALIDATION_ERROR_MESSAGES: { [key: string]: (error: unknown) => string } = {
  // Built-in validators
  required: () => 'Bu alan zorunludur',
  email: () => 'Geçerli bir e-posta adresi giriniz',
  min: (error) => `Minimum değer ${error.min} olmalıdır`,
  max: (error) => `Maksimum değer ${error.max} olmalıdır`,
  minlength: (error) => `En az ${error.requiredLength} karakter olmalıdır`,
  maxlength: (error) => `En fazla ${error.requiredLength} karakter olmalıdır`,
  pattern: () => 'Geçersiz format',

  // Custom validators
  turkishName: () => 'Sadece Türkçe karakterler ve boşluk kullanılabilir',
  tcKimlikNo: (error) => error.message || 'Geçersiz TC Kimlik Numarası',
  turkishPhone: (error) => error.message || 'Geçersiz telefon numarası formatı',
  iban: (error) => error.message || 'Geçersiz IBAN formatı',
  strongPassword: (error) => {
    const errors = [];
    if (error.minLength) {
      errors.push(`En az ${error.minLength.requiredLength} karakter`);
    }
    if (error.requireLowercase) {
      errors.push('En az bir küçük harf');
    }
    if (error.requireUppercase) {
      errors.push('En az bir büyük harf');
    }
    if (error.requireNumbers) {
      errors.push('En az bir rakam');
    }
    if (error.requireSpecialChars) {
      errors.push('En az bir özel karakter');
    }
    return `Şifre şunları içermelidir: ${errors.join(', ')}`;
  },
  url: () => 'Geçerli bir URL giriniz',
  json: (error) => `Geçersiz JSON formatı: ${error.message}`,
  fileSize: (error) => `Dosya boyutu ${error.maxSize}MB'den küçük olmalıdır (${error.fileName}: ${error.actualSize}MB)`,
  fileType: (error) => `İzin verilen dosya türleri: ${error.allowedTypes.join(', ')} (${error.fileName})`,
  dateRange: (error) => error.message,
  uniqueEmail: () => 'Bu e-posta adresi zaten kullanılıyor',
  uniqueUsername: () => 'Bu kullanıcı adı zaten kullanılıyor',
  passwordMatch: () => 'Şifreler eşleşmiyor',
  minAge: (error) => `En az ${error.requiredAge} yaşında olmalısınız (Mevcut yaş: ${error.actualAge})`
};

/**
 * Get error message for a form control
 */
export function getValidationErrorMessage(controlName: string, errors: Record<string, unknown>): string {
  if (!errors) {
    return '';
  }

  const errorKey = Object.keys(errors)[0];
  const errorValue = errors[errorKey];

  if (VALIDATION_ERROR_MESSAGES[errorKey]) {
    return VALIDATION_ERROR_MESSAGES[errorKey](errorValue);
  }

  // Fallback for unknown errors
  return `${controlName} alanında hata var`;
}

/**
 * Get all error messages for a form control
 */
export function getAllValidationErrorMessages(controlName: string, errors: Record<string, unknown>): string[] {
  if (!errors) {
    return [];
  }

  return Object.keys(errors).map(errorKey => {
    const errorValue = errors[errorKey];
    if (VALIDATION_ERROR_MESSAGES[errorKey]) {
      return VALIDATION_ERROR_MESSAGES[errorKey](errorValue);
    }
    return `${controlName} alanında hata var`;
  });
}

/**
 * Validation message configuration
 */
export interface ValidationMessageConfig {
  showFirstErrorOnly?: boolean;
  separator?: string;
  prefix?: string;
  suffix?: string;
}

/**
 * Format validation error messages
 */
export function formatValidationErrorMessages(
  controlName: string,
  errors: Record<string, unknown>,
  config: ValidationMessageConfig = {}
): string {
  const {
    showFirstErrorOnly = true,
    separator = ', ',
    prefix = '',
    suffix = ''
  } = config;

  if (!errors) {
    return '';
  }

  const messages = getAllValidationErrorMessages(controlName, errors);

  if (messages.length === 0) {
    return '';
  }

  const displayMessages = showFirstErrorOnly ? [messages[0]] : messages;
  return prefix + displayMessages.join(separator) + suffix;
}