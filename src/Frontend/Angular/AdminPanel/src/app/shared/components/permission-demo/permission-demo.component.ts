import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PermissionService } from '../../../core/services/permission.service';
import { PermissionConstants } from '../../../core/constants/permissions.constants';
import {
  HasPermissionDirective,
  HasAnyPermissionDirective,
  HasRoleDirective,
  PermissionDisabledDirective
} from '../../directives';

interface SampleUser {
  id: string;
  name: string;
  email: string;
  role: string;
  isActive: boolean;
}

@Component({
  selector: 'app-permission-demo',
  standalone: true,
  imports: [
    CommonModule,
    HasPermissionDirective,
    HasAnyPermissionDirective,
    HasRoleDirective,
    PermissionDisabledDirective
  ],
  templateUrl: './permission-demo.component.html',
  styleUrls: ['./permission-demo.component.scss']
})
export class PermissionDemoComponent implements OnInit {
  permissions = PermissionConstants;

  sampleUsers: SampleUser[] = [
    {
      id: '1',
      name: 'John Doe',
      email: 'john.doe@example.com',
      role: 'Admin',
      isActive: true
    },
    {
      id: '2',
      name: 'Jane Smith',
      email: 'jane.smith@example.com',
      role: 'User',
      isActive: true
    },
    {
      id: '3',
      name: 'Bob Johnson',
      email: 'bob.johnson@example.com',
      role: 'Moderator',
      isActive: false
    }
  ];

  constructor(private permissionService: PermissionService) {}

  ngOnInit(): void {
    // Initialize permissions if needed
    this.permissionService.initializePermissions();
  }

  getIdentityPermissions(): string[] {
    return [
      this.permissions.IDENTITY.USERS.READ,
      this.permissions.IDENTITY.USERS.CREATE,
      this.permissions.IDENTITY.USERS.UPDATE,
      this.permissions.IDENTITY.USERS.DELETE,
      this.permissions.IDENTITY.ROLES.READ,
      this.permissions.IDENTITY.ROLES.CREATE,
      this.permissions.IDENTITY.GROUPS.READ
    ];
  }

  getSystemPermissions(): string[] {
    return [
      this.permissions.SYSTEM.ADMIN.USER_MANAGEMENT,
      this.permissions.SYSTEM.ADMIN.SECURITY_AUDIT,
      this.permissions.SYSTEM.ADMIN.SYSTEM_CONFIG,
      this.permissions.SYSTEM.ADMIN.FULL_ACCESS
    ];
  }

  getSpeedReadingPermissions(): string[] {
    return [
      this.permissions.SPEED_READING.READING_TEXTS.READ,
      this.permissions.SPEED_READING.READING_TEXTS.CREATE,
      this.permissions.SPEED_READING.EXERCISES.READ,
      this.permissions.SPEED_READING.ANALYTICS.READ
    ];
  }

  hasPermissionSync(permission: string): boolean {
    return this.permissionService.canAccessWithWildcard(permission);
  }

  onCreateUser(): void {
    console.log('Creating new user...');
  }

  onEditUser(user: SampleUser): void {
    console.log('Editing user:', user.name);
  }

  onDeleteUser(user: SampleUser): void {
    if (confirm(`Are you sure you want to delete ${user.name}?`)) {
      console.log('Deleting user:', user.name);
    }
  }

  onManageRoles(user: SampleUser): void {
    console.log('Managing roles for user:', user.name);
  }

  onExportUsers(): void {
    console.log('Exporting users...');
  }
}