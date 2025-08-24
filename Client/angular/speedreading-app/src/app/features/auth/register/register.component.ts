import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ToastrService } from 'ngx-toastr';
import { AuthService } from '../../../core/services/auth.service';
import { RegisterRequest } from '../../../shared/models/auth.models';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule, 
    RouterModule,
    FormsModule
  ],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnInit {
  registerForm: FormGroup;
  isLoading = false;
  passwordVisible = false;
  confirmPasswordVisible = false;
  showTermsModal = false;

  ngOnInit(): void {
    // Any initialization logic
  }

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private toastr: ToastrService
  ) {
    this.registerForm = this.createForm();
  }

  private createForm(): FormGroup {
    return this.fb.group({
      userName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50), Validators.pattern('^[A-Za-z0-9]+$')]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(256)]],
      firstName: ['', [Validators.required, Validators.maxLength(50), Validators.pattern("^[a-zA-ZçğıöşüÇĞIİÖŞÜ\\s'-]+$")]],
      lastName: ['', [Validators.required, Validators.maxLength(50), Validators.pattern("^[a-zA-ZçğıöşüÇĞIİÖŞÜ\\s'-]+$")]],
      password: ['', [Validators.required, Validators.minLength(8), this.passwordComplexityValidator]],
      confirmPassword: ['', [Validators.required]],
      phoneNumber: ['', [Validators.required, Validators.pattern('^\\d{11}$')]],
      acceptTerms: [false, [Validators.requiredTrue]]
    }, { 
      validators: this.passwordMatchValidator 
    });
  }

  private passwordMatchValidator(group: FormGroup) {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  private passwordComplexityValidator(control: AbstractControl): ValidationErrors | null {
    const value: string = control.value || '';
    if (!value) return null;
    // En az 8 karakter, 1 küçük, 1 büyük, 1 rakam ve 1 özel karakter
    const complexity = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$/;
    return complexity.test(value) ? null : { passwordComplexity: true };
  }

  togglePasswordVisibility(field: 'password' | 'confirmPassword'): void {
    if (field === 'password') {
      this.passwordVisible = !this.passwordVisible;
    } else {
      this.confirmPasswordVisible = !this.confirmPasswordVisible;
    }
  }

  openTermsModal(event: Event): void {
    event.preventDefault();
    this.showTermsModal = true;
  }

  closeTermsModal(): void {
    this.showTermsModal = false;
  }

  onSubmit(): void {
    if (this.registerForm.valid && !this.isLoading) {
      this.isLoading = true;
      
      const formValue = this.registerForm.value;
      const registerRequest: RegisterRequest = {
        userName: formValue.userName,
        email: formValue.email,
        firstName: formValue.firstName,
        lastName: formValue.lastName,
        password: formValue.password,
        confirmPassword: formValue.confirmPassword,
        phoneNumber: formValue.phoneNumber && formValue.phoneNumber.trim().length > 0 ? formValue.phoneNumber.trim() : undefined,
        categories: ['SpeedReading'] // Default kategori
      };

      this.authService.register(registerRequest).subscribe({
        next: (response) => {
          this.isLoading = false;
          if (response.success) {
            this.toastr.success('Kayıt başarılı! Email adresinizi kontrol ederek hesabınızı doğrulayın.', 'Başarılı');
            this.router.navigate(['/auth/login']);
          } else {
            this.toastr.error(response.error?.message || 'Kayıt işlemi başarısız', 'Hata');
          }
        },
        error: (error) => {
          this.isLoading = false;
          console.error('Register error:', error);
          const responseBody = error.error;

          // Case 1: FluentValidation errors (e.g., invalid characters)
          if (responseBody?.errors) {
            const errors = responseBody.errors;
            Object.keys(errors).forEach(backendKey => {
              const formControlName = backendKey.charAt(0).toLowerCase() + backendKey.slice(1);
              const control = this.registerForm.get(formControlName);
              const messages = errors[backendKey] as string[];

              if (control && messages.length > 0) {
                const turkishMessage = this.translateBackendError(messages[0], formControlName);
                control.setErrors({ backendError: turkishMessage });
              }
            });
          // Case 2: Custom business logic errors (e.g., duplicate email/username)
          } else if (responseBody?.error?.message) {
            const message = responseBody.error.message;
            if (message.toLowerCase().includes('email')) {
              this.registerForm.get('email')?.setErrors({ backendError: 'Bu e-posta adresi zaten kullanılıyor.' });
            } else if (message.toLowerCase().includes('username')) {
              this.registerForm.get('userName')?.setErrors({ backendError: 'Bu kullanıcı adı zaten alınmış.' });
            } else {
              // For other unmapped business errors, show a toast
              this.toastr.error(message, 'Hata');
            }
          // Fallback for any other error structure
          } else {
            this.toastr.error(responseBody?.message || 'Kayıt işlemi sırasında bir hata oluştu', 'Hata');
          }
        }
      });
    } else {
      this.markFormGroupTouched();
    }
  }

  googleRegister(): void {
    this.authService.googleLogin(); // Same flow as login
  }

  private markFormGroupTouched(): void {
    Object.keys(this.registerForm.controls).forEach(key => {
      const control = this.registerForm.get(key);
      if (control) {
        control.markAsTouched();
      }
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.registerForm.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  getFieldError(fieldName: string): string {
    const field = this.registerForm.get(fieldName);
    if (field && field.errors && field.touched) {
      const errors = field.errors;
      if (errors['required']) return `${this.getFieldDisplayName(fieldName)} gereklidir`;
      if (errors['email']) return 'Geçerli bir email adresi girin';
      if (errors['minlength']) return `${this.getFieldDisplayName(fieldName)} en az ${errors['minlength'].requiredLength} karakter olmalıdır`;
      if (errors['maxlength']) return `${this.getFieldDisplayName(fieldName)} en fazla ${errors['maxlength'].requiredLength} karakter olmalıdır`;
      if (errors['pattern']) {
        if (fieldName === 'phoneNumber') return 'Telefon numarası 11 rakamdan oluşmalıdır (Örn: 05551234567)';
        if (fieldName === 'firstName' || fieldName === 'lastName') return 'Sadece harf, boşluk, tire (-) ve apostrof (\') kullanılabilir';
        if (fieldName === 'userName') return 'Kullanıcı adı sadece harf ve rakam içerebilir';
        return 'Geçersiz format';
      }
      if (errors['passwordComplexity']) return 'Şifre en az 8 karakter olmalı; en az bir büyük harf, bir küçük harf, bir rakam ve bir özel karakter içermelidir.';
      if (errors['requiredTrue']) return 'Kullanım şartlarını kabul etmelisiniz';
      if (errors['backendError']) return errors['backendError'];
    }

    if (fieldName === 'confirmPassword' && this.registerForm.errors?.['passwordMismatch']) {
      return 'Şifreler eşleşmiyor';
    }

    return '';
  }

  onPhoneInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const digitsOnly = (input.value || '').replace(/\D+/g, '').slice(0, 11);
    this.registerForm.get('phoneNumber')?.setValue(digitsOnly, { emitEvent: false });
  }

  private translateBackendError(message: string, field: string): string {
    if (message.includes('is already taken') || message.includes('is already in use')) {
      return `Bu ${field === 'userName' ? 'kullanıcı adı' : 'e-posta adresi'} zaten kullanılıyor.`;
    }
    if (message.includes('one uppercase letter')) {
      return 'Şifre en az bir büyük harf, bir küçük harf, bir rakam ve bir özel karakter içermelidir.';
    }
    if (message.includes('can only contain letters and spaces')) {
      const fieldName = this.getFieldDisplayName(field);
      return `${fieldName} sadece harf ve boşluk içerebilir.`;
    }
    // Bilinmeyen diğer hatalar için backend'den gelen mesajı direkt göster
    return message;
  }

  private getFieldDisplayName(fieldName: string): string {
    const displayNames: { [key: string]: string } = {
      userName: 'Kullanıcı adı',
      email: 'Email',
      firstName: 'Ad',
      lastName: 'Soyad',
      password: 'Şifre',
      confirmPassword: 'Şifre tekrarı',
      phoneNumber: 'Telefon numarası'
    };
    return displayNames[fieldName] || fieldName;
  }
}