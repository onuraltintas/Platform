import { Component, inject, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { UserAdminService, RoleDto, CategoryDto } from '../../data-access/user-admin.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  standalone: true,
  selector: 'app-user-form',
  templateUrl: './user-form.component.html',
  styleUrls: ['./user-form.component.scss'],
  imports: [CommonModule, ReactiveFormsModule, RouterModule]
})
export class UserFormComponent implements OnDestroy {
  private fb = inject(FormBuilder);
  private admin = inject(UserAdminService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private toastr = inject(ToastrService);

  userId: string | null = null;
  loading = false;
  error: string | null = null;
  successMessage: string | null = null;
  roles: RoleDto[] = [];
  categories: CategoryDto[] = [];
  passwordFieldType: 'password' | 'text' = 'password';
  confirmPasswordFieldType: 'password' | 'text' = 'password';
  private redirectTimeout?: number;

  form = this.fb.group({
    userName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50), Validators.pattern('^[A-Za-z0-9]+$')]],
    email: ['', [Validators.required, Validators.email]],
    firstName: ['', [Validators.required, Validators.pattern("^[a-zA-ZçğıöşüÇĞIİÖŞÜ\\s'-]+$")]],
    lastName: ['', [Validators.required, Validators.pattern("^[a-zA-ZçğıöşüÇĞIİÖŞÜ\\s'-]+$")]],
    phoneNumber: ['', [Validators.required, Validators.pattern('^[0-9]{11}$')]],
    isActive: [true],
    isEmailConfirmed: [false],
    password: [''],
    confirmPassword: [''], // Yeni alan eklendi
    roleIds: [[] as string[]],
    categoryIds: [[] as string[]]
  }, { validators: this.passwordMatchValidator });

  ngOnInit() {
    this.userId = this.route.snapshot.paramMap.get('id');
    
    if (this.userId) {
      // edit mode
      this.form.get('password')?.disable();
      this.loading = true;
      
      // Roller ve kategorileri yükle, sonra kullanıcıyı yükle
      this.loadRolesAndCategories().then(() => {
        this.loadUserData();
      });
    } else {
      // create mode - require password
      this.form.get('password')?.addValidators([Validators.required, Validators.minLength(8), this.passwordComplexityValidator]);
      this.form.get('confirmPassword')?.addValidators(Validators.required);
      this.loadRolesAndCategories().then(() => {
        // Yeni kullanıcı oluştururken herhangi bir rol/kategori ön-seçimi olmasın
        this.form.patchValue({ roleIds: [], categoryIds: [] }, { emitEvent: false });
      });
    }
  }

  private passwordComplexityValidator(control: AbstractControl): ValidationErrors | null {
    const value: string = control.value || '';
    if (!value) return null;
    const complexity = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$/;
    return complexity.test(value) ? null : { passwordComplexity: true };
  }
  
  private loadUserData() {
    if (!this.userId) return;
    
    this.admin.getUser(this.userId).subscribe({
      next: (res) => {
        const u = (res as any).data ?? res;
        
        // Rol ve kategori ID'lerini bulma
        const roleIds = this.roles.filter(r => u.roles.includes(r.name)).map(r => r.id);
        const categoryIds = this.categories.filter(c => u.categories.includes(c.name)).map(c => c.id);
        
        this.form.patchValue({
          userName: u.userName,
          email: u.email,
          firstName: u.firstName,
          lastName: u.lastName,
          phoneNumber: u.phoneNumber || '', // phoneNumber null/undefined ise boş string olarak set et
          isActive: u.isActive,
          isEmailConfirmed: u.isEmailConfirmed,
          roleIds: roleIds,
          categoryIds: categoryIds
        });
      },
      error: (err) => this.error = err?.message ?? 'Kullanıcı yüklenemedi',
      complete: () => this.loading = false
    });
  }

  save() {
    if (this.form.invalid) {
      this.markAllFieldsAsTouched();
      this.toastr.error('Lütfen formdaki hataları düzeltin.', 'Doğrulama Hatası');
      return;
    }
    this.loading = true;
    this.error = null;
    this.successMessage = null;
    const value = this.form.getRawValue();
    
    // Telefon numarasını doğru şekilde işle
    let phoneNumber: string | null = null;
    if (value.phoneNumber && typeof value.phoneNumber === 'string') {
      const trimmed = value.phoneNumber.trim();
      phoneNumber = trimmed.length > 0 ? trimmed : null;
    }
    
    // roleIds/categoryIds temizle (boş/tekrarlı değerleri filtrele)
    const cleanRoleIds: string[] = Array.isArray(value.roleIds)
      ? Array.from(new Set(
          (value.roleIds as string[])
            .filter(id => typeof id === 'string')
            .map(id => id.trim())
            .filter(id => id.length > 0)
        ))
      : [];
    const cleanCategoryIds: string[] = Array.isArray(value.categoryIds)
      ? Array.from(new Set(
          (value.categoryIds as string[])
            .filter(id => typeof id === 'string')
            .map(id => id.trim())
            .filter(id => id.length > 0)
        ))
      : [];
    
    // Payload'ları hazırla - phoneNumber her zaman dahil edilsin
    const updatePayload: any = {
      userName: value.userName!, 
      email: value.email!, 
      firstName: value.firstName!, 
      lastName: value.lastName!,
      phoneNumber: phoneNumber, 
      isActive: !!value.isActive, 
      isEmailConfirmed: !!value.isEmailConfirmed,
      roleIds: cleanRoleIds,
      categoryIds: cleanCategoryIds
    };
    
    const createPayload: any = {
      userName: value.userName!, 
      email: value.email!, 
      firstName: value.firstName!, 
      lastName: value.lastName!,
      password: value.password!,
      phoneNumber: phoneNumber,
      isActive: !!value.isActive, 
      isEmailConfirmed: !!value.isEmailConfirmed,
      roleIds: cleanRoleIds,
      categoryIds: cleanCategoryIds
    };
    
    const req$ = this.userId
      ? this.admin.updateUser(this.userId!, updatePayload)
      : this.admin.createUser(createPayload);

    req$.subscribe({
      next: (response) => {
        const message = this.userId ? 'Kullanıcı başarıyla güncellendi!' : 'Kullanıcı başarıyla oluşturuldu!';
        this.toastr.success(message, 'Başarılı!');
        this.loading = false;
        this.error = null;
        this.redirectTimeout = window.setTimeout(() => this.router.navigate(['/users']), 2000);
      },
      error: (err) => {
        this.loading = false;
        if (err.status === 400 && err.error?.errors) {
          // Sunucudan gelen validasyon hatalarını işle
          const serverErrors = err.error.errors;
          for (const key in serverErrors) {
            if (serverErrors.hasOwnProperty(key)) {
              // Backend (PascalCase) ve Frontend (camelCase) key'lerini eşle
              const formKey = key.charAt(0).toLowerCase() + key.slice(1);
              const control = this.form.get(formKey);
              if (control) {
                const messages = serverErrors[key];
                control.setErrors({ serverError: messages.join(', ') });
              }
            }
          }
          this.toastr.error('Lütfen formdaki hataları düzeltin.', 'Doğrulama Hatası');
          this.error = null; // Genel hata mesajını gösterme
        } else {
          // Diğer hatalar (500, 403 vb.) interceptor tarafından zaten toaster ile gösteriliyor.
          // İsterseniz burada da özel bir mesaj gösterebilirsiniz.
          this.error = err.error?.message || 'Bilinmeyen bir hata oluştu. Lütfen tekrar deneyin.';
        }
      }
    });
  }

  private async loadRolesAndCategories(): Promise<void> {
    try {
      const rolesResponse = await firstValueFrom(this.admin.listRoles({ page: 1, pageSize: 100, isActive: true }));
      this.roles = (rolesResponse as any).data ?? rolesResponse;
      
      const categoriesResponse = await firstValueFrom(this.admin.listCategories({ page: 1, pageSize: 100, isActive: true }));
      this.categories = (categoriesResponse as any).data ?? categoriesResponse;
    } catch (err) {
      console.error('Roller ve kategoriler yüklenemedi:', err);
    }
  }

  getSelectedCount(controlName: string): number {
    const control = this.form.get(controlName);
    return control?.value?.length || 0;
  }
  
  isFieldInvalid(fieldName: string): boolean {
    const field = this.form.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }
  
  getFieldError(fieldName: string): string {
    const control = this.form.get(fieldName);
    if (!control || !control.errors) return '';
    const errors = control.errors;
    
    if (errors['serverError']) {
      return errors['serverError'];
    }
    if (errors['required']) {
      return 'Bu alan zorunludur';
    }
    if (errors['minlength']) {
      return `En az ${errors['minlength'].requiredLength} karakter olmalıdır`;
    }
    if (errors['email']) {
      return 'Geçersiz e-posta adresi';
    }
    if (errors['passwordMismatch']) {
      return 'Şifreler eşleşmiyor';
    }
    if (errors['pattern']) {
      // Hangi alan olduğuna göre mesajı özelleştir
      if (fieldName === 'phoneNumber') return 'Telefon numarası 11 rakamdan oluşmalıdır';
      if (fieldName === 'firstName' || fieldName === 'lastName') return 'Sadece harf, boşluk, tire (-) ve apostrof (\') kullanılabilir';
      if (fieldName === 'userName') return 'Kullanıcı adı sadece harf ve rakamlardan oluşmalıdır';
      return 'Geçersiz format';
    }
    if (errors['passwordComplexity']) {
      return 'Şifre en az 8 karakter olmalı; en az bir büyük harf, bir küçük harf, bir rakam ve bir özel karakter içermelidir.';
    }
    return 'Geçersiz değer';
  }
  
  private getFieldDisplayName(fieldName: string): string {
    const displayNames: { [key: string]: string } = {
      userName: 'Kullanıcı adı',
      email: 'E-posta',
      firstName: 'Ad',
      lastName: 'Soyad',
      phoneNumber: 'Telefon numarası',
      password: 'Şifre'
    };
    return displayNames[fieldName] || fieldName;
  }
  
  private markAllFieldsAsTouched(): void {
    Object.keys(this.form.controls).forEach(key => {
      const control = this.form.get(key);
      if (control) {
        control.markAsTouched();
        control.markAsDirty();
      }
    });
  }
  
  ngOnDestroy() {
    if (this.redirectTimeout) {
      clearTimeout(this.redirectTimeout);
    }
  }

  togglePasswordVisibility() {
    this.passwordFieldType = this.passwordFieldType === 'password' ? 'text' : 'password';
  }
  
  toggleConfirmPasswordVisibility() {
    this.confirmPasswordFieldType = this.confirmPasswordFieldType === 'password' ? 'text' : 'password';
  }

  private passwordMatchValidator(group: AbstractControl): ValidationErrors | null {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    const confirmCtrl = group.get('confirmPassword');

    // Her iki alan da boşsa (özellikle düzenleme modunda) hata oluşturma
    if (!password && !confirmPassword) {
      if (confirmCtrl && confirmCtrl.errors) {
        const { passwordMismatch, ...rest } = confirmCtrl.errors;
        confirmCtrl.setErrors(Object.keys(rest).length ? rest : null);
      }
      return null;
    }

    if (password !== confirmPassword) {
      // Mevcut hataları koruyarak passwordMismatch ekle
      if (confirmCtrl) {
        const existingErrors = confirmCtrl.errors || {};
        confirmCtrl.setErrors({ ...existingErrors, passwordMismatch: true });
      }
      return null; // Grup hatası döndürmeyelim; hatayı kontrol üzerinde tuttuk
    }

    // Eşleşiyorsa confirmPassword üzerindeki passwordMismatch'ı temizle
    if (confirmCtrl && confirmCtrl.errors) {
      const { passwordMismatch, ...rest } = confirmCtrl.errors;
      confirmCtrl.setErrors(Object.keys(rest).length ? rest : null);
    }
    return null;
  }
}

