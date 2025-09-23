import { AbstractControl, ValidationErrors, ValidatorFn, AsyncValidatorFn } from '@angular/forms';
import { Observable, of, timer } from 'rxjs';
import { switchMap, catchError } from 'rxjs/operators';

export class CustomValidators {
  /**
   * Türkçe karakterlere izin veren isim validator'ı
   */
  static turkishName(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null;
      }

      const turkishNameRegex = /^[a-zA-ZçğıöşüÇĞIİÖŞÜ\s]+$/;

      if (!turkishNameRegex.test(control.value)) {
        return { turkishName: { actualValue: control.value } };
      }

      return null;
    };
  }

  /**
   * Türk Kimlik Numarası validator'ı
   */
  static tcKimlikNo(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null;
      }

      const tcNo = control.value.toString().trim();

      // 11 haneli olmalı
      if (tcNo.length !== 11) {
        return { tcKimlikNo: { message: 'TC Kimlik No 11 haneli olmalıdır' } };
      }

      // Sadece rakam olmalı
      if (!/^\d+$/.test(tcNo)) {
        return { tcKimlikNo: { message: 'TC Kimlik No sadece rakamlardan oluşmalıdır' } };
      }

      // İlk hane 0 olamaz
      if (tcNo[0] === '0') {
        return { tcKimlikNo: { message: 'TC Kimlik No 0 ile başlayamaz' } };
      }

      // TC algoritması kontrolü
      if (!this.validateTcAlgorithm(tcNo)) {
        return { tcKimlikNo: { message: 'Geçersiz TC Kimlik Numarası' } };
      }

      return null;
    };
  }

  private static validateTcAlgorithm(tcNo: string): boolean {
    const digits = tcNo.split('').map(Number);

    // İlk 10 hanenin toplamı
    const sum = digits.slice(0, 10).reduce((acc, digit) => acc + digit, 0);

    // 11. hane kontrolü
    if (sum % 10 !== digits[10]) {
      return false;
    }

    // Çift ve tek hanelerin toplamı
    const oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
    const evenSum = digits[1] + digits[3] + digits[5] + digits[7];

    // 10. hane kontrolü
    const tenthDigit = ((oddSum * 7) - evenSum) % 10;

    return tenthDigit === digits[9];
  }

  /**
   * Türk telefon numarası validator'ı
   */
  static turkishPhone(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null;
      }

      // Sadece rakamları al
      const phone = control.value.replace(/\D/g, '');

      // Türk telefon numarası formatları:
      // 05xxxxxxxxx (11 haneli)
      // +905xxxxxxxxx (+90 ile 13 haneli)
      // 5xxxxxxxxx (10 haneli, başında 0 yok)

      const patterns = [
        /^05\d{9}$/, // 05xxxxxxxxx
        /^905\d{9}$/, // +905xxxxxxxxx (+ olmadan)
        /^5\d{9}$/ // 5xxxxxxxxx
      ];

      const isValid = patterns.some(pattern => pattern.test(phone));

      if (!isValid) {
        return { turkishPhone: { message: 'Geçersiz Türk telefon numarası formatı' } };
      }

      return null;
    };
  }

  /**
   * IBAN validator'ı (Türkiye için özelleştirilmiş)
   */
  static iban(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null;
      }

      const iban = control.value.replace(/\s/g, '').toUpperCase();

      // Türkiye IBAN formatı: TR + 2 kontrol hanesi + 5 banka kodu + 1 rezerv + 16 hesap numarası = 26 karakter
      if (!iban.startsWith('TR')) {
        return { iban: { message: 'IBAN TR ile başlamalıdır' } };
      }

      if (iban.length !== 26) {
        return { iban: { message: 'Türk IBAN 26 karakter olmalıdır' } };
      }

      // IBAN checksum algoritması
      if (!this.validateIBANChecksum(iban)) {
        return { iban: { message: 'Geçersiz IBAN kontrol kodu' } };
      }

      return null;
    };
  }

  private static validateIBANChecksum(iban: string): boolean {
    // IBAN'ı yeniden düzenle (ülke kodu ve kontrol kodunu sona taşı)
    const rearranged = iban.slice(4) + iban.slice(0, 4);

    // Harfleri sayılara çevir (A=10, B=11, ..., Z=35)
    let numeric = '';
    for (const char of rearranged) {
      if (char >= '0' && char <= '9') {
        numeric += char;
      } else {
        numeric += (char.charCodeAt(0) - 55).toString();
      }
    }

    // Mod 97 kontrolü
    return this.mod97(numeric) === 1;
  }

  private static mod97(numericString: string): number {
    let remainder = 0;
    for (let i = 0; i < numericString.length; i++) {
      remainder = (remainder * 10 + parseInt(numericString[i])) % 97;
    }
    return remainder;
  }

  /**
   * Güçlü şifre validator'ı
   */
  static strongPassword(options: {
    minLength?: number;
    requireLowercase?: boolean;
    requireUppercase?: boolean;
    requireNumbers?: boolean;
    requireSpecialChars?: boolean;
  } = {}): ValidatorFn {
    const {
      minLength = 8,
      requireLowercase = true,
      requireUppercase = true,
      requireNumbers = true,
      requireSpecialChars = true
    } = options;

    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null;
      }

      const password = control.value;
      const errors: Record<string, unknown> = {};

      if (password.length < minLength) {
        errors.minLength = { requiredLength: minLength, actualLength: password.length };
      }

      if (requireLowercase && !/[a-z]/.test(password)) {
        errors.requireLowercase = true;
      }

      if (requireUppercase && !/[A-Z]/.test(password)) {
        errors.requireUppercase = true;
      }

      if (requireNumbers && !/\d/.test(password)) {
        errors.requireNumbers = true;
      }

      if (requireSpecialChars && !/[!@#$%^&*(),.?":{}|<>]/.test(password)) {
        errors.requireSpecialChars = true;
      }

      return Object.keys(errors).length > 0 ? { strongPassword: errors } : null;
    };
  }

  /**
   * URL validator'ı
   */
  static url(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null;
      }

      try {
        new URL(control.value);
        return null;
      } catch {
        return { url: { actualValue: control.value } };
      }
    };
  }

  /**
   * JSON validator'ı
   */
  static json(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null;
      }

      try {
        JSON.parse(control.value);
        return null;
      } catch (error: unknown) {
        const errorMessage = error instanceof Error ? error.message : 'Invalid JSON';
        return { json: { message: errorMessage } };
      }
    };
  }

  /**
   * Dosya boyutu validator'ı
   */
  static fileSize(maxSizeInMB: number): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null;
      }

      const files = control.value as FileList;
      const maxSizeInBytes = maxSizeInMB * 1024 * 1024;

      for (let i = 0; i < files.length; i++) {
        if (files[i].size > maxSizeInBytes) {
          return {
            fileSize: {
              maxSize: maxSizeInMB,
              actualSize: Math.round(files[i].size / 1024 / 1024 * 100) / 100,
              fileName: files[i].name
            }
          };
        }
      }

      return null;
    };
  }

  /**
   * Dosya türü validator'ı
   */
  static fileType(allowedTypes: string[]): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null;
      }

      const files = control.value as FileList;

      for (let i = 0; i < files.length; i++) {
        const fileType = files[i].type.toLowerCase();
        const fileName = files[i].name.toLowerCase();

        const isAllowed = allowedTypes.some(type => {
          if (type.includes('/')) {
            // MIME type kontrolü
            return fileType === type.toLowerCase();
          } else {
            // Dosya uzantısı kontrolü
            return fileName.endsWith(type.toLowerCase());
          }
        });

        if (!isAllowed) {
          return {
            fileType: {
              allowedTypes,
              actualType: fileType,
              fileName: files[i].name
            }
          };
        }
      }

      return null;
    };
  }

  /**
   * Tarih aralığı validator'ı
   */
  static dateRange(minDate?: Date, maxDate?: Date): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null;
      }

      const date = new Date(control.value);

      if (isNaN(date.getTime())) {
        return { dateRange: { message: 'Geçersiz tarih formatı' } };
      }

      if (minDate && date < minDate) {
        return {
          dateRange: {
            message: `Tarih ${minDate.toLocaleDateString()} tarihinden önce olamaz`,
            minDate
          }
        };
      }

      if (maxDate && date > maxDate) {
        return {
          dateRange: {
            message: `Tarih ${maxDate.toLocaleDateString()} tarihinden sonra olamaz`,
            maxDate
          }
        };
      }

      return null;
    };
  }

  /**
   * Async email benzersizlik validator'ı (örnek)
   */
  static uniqueEmail(): AsyncValidatorFn {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      if (!control.value) {
        return of(null);
      }

      // Simulate API call
      return timer(500).pipe(
        switchMap(() => {
          // Mock API response - gerçek uygulamada HTTP service kullanılacak
          const existingEmails = ['admin@example.com', 'user@example.com'];
          const isUnique = !existingEmails.includes(control.value);

          return of(isUnique ? null : { uniqueEmail: { actualValue: control.value } });
        }),
        catchError(() => of(null))
      );
    };
  }

  /**
   * Async kullanıcı adı benzersizlik validator'ı (örnek)
   */
  static uniqueUsername(): AsyncValidatorFn {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      if (!control.value) {
        return of(null);
      }

      return timer(500).pipe(
        switchMap(() => {
          // Mock API response
          const existingUsernames = ['admin', 'user', 'test'];
          const isUnique = !existingUsernames.includes(control.value.toLowerCase());

          return of(isUnique ? null : { uniqueUsername: { actualValue: control.value } });
        }),
        catchError(() => of(null))
      );
    };
  }

  /**
   * Şifre eşleşme validator'ı (form seviyesi için)
   */
  static passwordMatch(passwordField: string, confirmPasswordField: string): ValidatorFn {
    return (formGroup: AbstractControl): ValidationErrors | null => {
      const password = formGroup.get(passwordField);
      const confirmPassword = formGroup.get(confirmPasswordField);

      if (!password || !confirmPassword) {
        return null;
      }

      if (password.value !== confirmPassword.value) {
        confirmPassword.setErrors({ passwordMatch: true });
        return { passwordMatch: true };
      } else {
        // Sadece passwordMatch hatasını temizle, diğer hataları koru
        if (confirmPassword.errors) {
          delete confirmPassword.errors['passwordMatch'];
          if (Object.keys(confirmPassword.errors).length === 0) {
            confirmPassword.setErrors(null);
          }
        }
      }

      return null;
    };
  }

  /**
   * Minimum yaş validator'ı
   */
  static minAge(minAge: number): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null;
      }

      const birthDate = new Date(control.value);
      const today = new Date();
      const age = today.getFullYear() - birthDate.getFullYear();
      const monthDiff = today.getMonth() - birthDate.getMonth();

      let actualAge = age;
      if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
        actualAge--;
      }

      if (actualAge < minAge) {
        return {
          minAge: {
            requiredAge: minAge,
            actualAge
          }
        };
      }

      return null;
    };
  }
}