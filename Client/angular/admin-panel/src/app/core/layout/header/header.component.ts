import { Component, HostListener, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../features/access-control/data-access/auth.service';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent implements OnInit, OnDestroy {
  private authService = inject(AuthService);
  private router = inject(Router);
  private userSubscription?: Subscription;
  
  currentUser: any;
  avatarLetter = 'U';
  avatarSrc = signal<string | null>(null);
  menuOpen = signal(false);

  ngOnInit(): void {
    this.userSubscription = this.authService.currentUser$.subscribe(user => {
      if (user) {
        this.currentUser = user;
        const base = user.fullName || user.userName || user.email || 'User';
        this.avatarLetter = (String(base).trim()[0] || 'U').toUpperCase();
      } else {
        this.currentUser = null;
        this.avatarLetter = 'U';
      }
    });
  }

  ngOnDestroy(): void {
    this.userSubscription?.unsubscribe();
  }

  @HostListener('document:click') onDocClick() {
    if (this.menuOpen()) this.menuOpen.set(false);
  }

  toggleMenu(event: Event) {
    event.stopPropagation();
    this.menuOpen.set(!this.menuOpen());
  }

  closeMenu() {
    this.menuOpen.set(false);
  }

  navigate(path: string): void {
    this.router.navigate([path]);
  }

  logout(): void {
    this.authService.logout().subscribe({
      complete: () => {
        // Don't clear logout flag immediately, let login page handle it
        window.location.href = '/login';
      }
    });
  }
}