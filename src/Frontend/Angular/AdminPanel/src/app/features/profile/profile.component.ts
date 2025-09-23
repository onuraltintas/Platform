import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent {

  profileForm: FormGroup;
  saving = false;

  userInfo = {
    name: 'John Doe',
    email: 'john.doe@company.com',
    loginCount: 156,
    lastLoginDays: 2,
    activeGroups: 3
  };

  constructor() {
    this.profileForm = new FormBuilder().group({
      firstName: ['John', [Validators.required, Validators.minLength(2)]],
      lastName: ['Doe', [Validators.required, Validators.minLength(2)]],
      email: ['john.doe@company.com', [Validators.required, Validators.email]],
      phoneNumber: ['+90 555 123 45 67'],
      department: ['it'],
      position: ['Senior Software Developer'],
      bio: ['Experienced software developer with expertise in full-stack development.'],
      emailNotifications: [true],
      smsNotifications: [false],
      pushNotifications: [true]
    });
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.profileForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  onSave(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    this.saving = true;

    // Simulate API call
    setTimeout(() => {
      this.saving = false;
      // Here you would typically call a service to save the profile
      console.log('Profile saved:', this.profileForm.value);
    }, 1500);
  }
}