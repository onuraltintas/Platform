import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export class CustomValidators {
  static email(control: AbstractControl): ValidationErrors | null {
    const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;

    if (!control.value) {
      return null;
    }

    return emailRegex.test(control.value) ? null : { email: true };
  }

  static password(control: AbstractControl): ValidationErrors | null {
    const value = control.value;

    if (!value) {
      return null;
    }

    const hasNumber = /[0-9]/.test(value);
    const hasUpper = /[A-Z]/.test(value);
    const hasLower = /[a-z]/.test(value);
    const hasSpecial = /[!@#$%^&*(),.?":{}|<>]/.test(value);
    const hasMinLength = value.length >= 8;

    const errors: ValidationErrors = {};

    if (!hasMinLength) {
      errors['minLength'] = { requiredLength: 8, actualLength: value.length };
    }

    if (!hasNumber) {
      errors['requiresNumber'] = true;
    }

    if (!hasUpper) {
      errors['requiresUppercase'] = true;
    }

    if (!hasLower) {
      errors['requiresLowercase'] = true;
    }

    if (!hasSpecial) {
      errors['requiresSpecial'] = true;
    }

    return Object.keys(errors).length === 0 ? null : { password: errors };
  }

  static phoneNumber(control: AbstractControl): ValidationErrors | null {
    const phoneRegex = /^(\+90|90)?[1-9][0-9]{9}$/;

    if (!control.value) {
      return null;
    }

    const cleaned = control.value.replace(/[\s()-]/g, '');
    return phoneRegex.test(cleaned) ? null : { phoneNumber: true };
  }

  static passwordMatch(passwordField: string, confirmPasswordField: string): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const password = control.get(passwordField);
      const confirmPassword = control.get(confirmPasswordField);

      if (!password || !confirmPassword) {
        return null;
      }

      if (password.value !== confirmPassword.value) {
        confirmPassword.setErrors({ passwordMismatch: true });
        return { passwordMismatch: true };
      } else {
        const errors = confirmPassword.errors;
        if (errors) {
          delete errors['passwordMismatch'];
          if (Object.keys(errors).length === 0) {
            confirmPassword.setErrors(null);
          }
        }
        return null;
      }
    };
  }

  static noWhitespace(control: AbstractControl): ValidationErrors | null {
    if (!control.value) {
      return null;
    }

    const hasWhitespace = /\s/.test(control.value);
    return hasWhitespace ? { whitespace: true } : null;
  }

  static requiredIf(condition: () => boolean): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!condition()) {
        return null;
      }

      return control.value ? null : { required: true };
    };
  }

  static minAge(age: number): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      if (!control.value) {
        return null;
      }

      const birthDate = new Date(control.value);
      const today = new Date();
      const userAge = today.getFullYear() - birthDate.getFullYear();
      const monthDiff = today.getMonth() - birthDate.getMonth();

      if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
        return userAge - 1 >= age ? null : { minAge: { requiredAge: age, actualAge: userAge - 1 } };
      }

      return userAge >= age ? null : { minAge: { requiredAge: age, actualAge: userAge } };
    };
  }
}

export function getPasswordStrength(password: string): { strength: number; label: string; color: string } {
  if (!password) {
    return { strength: 0, label: 'Çok Zayıf', color: 'danger' };
  }

  let score = 0;
  const checks = [
    password.length >= 8,
    /[a-z]/.test(password),
    /[A-Z]/.test(password),
    /[0-9]/.test(password),
    /[!@#$%^&*(),.?":{}|<>]/.test(password),
    password.length >= 12
  ];

  score = checks.filter(Boolean).length;

  if (score <= 2) {
    return { strength: score, label: 'Çok Zayıf', color: 'danger' };
  } else if (score <= 3) {
    return { strength: score, label: 'Zayıf', color: 'warning' };
  } else if (score <= 4) {
    return { strength: score, label: 'Orta', color: 'info' };
  } else if (score <= 5) {
    return { strength: score, label: 'Güçlü', color: 'success' };
  } else {
    return { strength: score, label: 'Çok Güçlü', color: 'success' };
  }
}