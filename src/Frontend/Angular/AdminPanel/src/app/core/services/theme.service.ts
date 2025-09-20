import { Injectable, signal } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export type Theme = 'light' | 'dark';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly THEME_KEY = 'admin-panel-theme';

  private themeSubject = new BehaviorSubject<Theme>(this.getInitialTheme());
  isDarkTheme$ = this.themeSubject.asObservable();

  currentTheme = signal<Theme>(this.getInitialTheme());

  constructor() {
    this.applyTheme(this.currentTheme());
  }

  private getInitialTheme(): Theme {
    // Check localStorage first
    const savedTheme = localStorage.getItem(this.THEME_KEY) as Theme;
    if (savedTheme) {
      return savedTheme;
    }

    // Check system preference
    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
      return 'dark';
    }

    return 'light';
  }

  toggleTheme(): void {
    const newTheme: Theme = this.currentTheme() === 'light' ? 'dark' : 'light';
    this.setTheme(newTheme);
  }

  setTheme(theme: Theme): void {
    this.currentTheme.set(theme);
    this.themeSubject.next(theme);
    this.applyTheme(theme);
    localStorage.setItem(this.THEME_KEY, theme);
  }

  private applyTheme(theme: Theme): void {
    const htmlElement = document.documentElement;

    // Remove existing theme classes
    htmlElement.classList.remove('light-theme', 'dark-theme');

    // Add new theme class
    htmlElement.classList.add(`${theme}-theme`);

    // Update CSS custom properties
    if (theme === 'dark') {
      this.setDarkThemeProperties();
    } else {
      this.setLightThemeProperties();
    }
  }

  private setLightThemeProperties(): void {
    const root = document.documentElement;
    root.style.setProperty('--bs-body-bg', '#ffffff');
    root.style.setProperty('--bs-body-color', '#212529');
    root.style.setProperty('--bs-card-bg', '#ffffff');
    root.style.setProperty('--bs-border-color', '#dee2e6');
    root.style.setProperty('--bs-sidebar-bg', '#ffffff');
    root.style.setProperty('--bs-header-bg', '#ffffff');
    root.style.setProperty('--bs-content-bg', '#f8f9fa');
    root.style.setProperty('--bs-nav-link-color', '#6c757d');
    root.style.setProperty('--bs-nav-link-hover-color', '#0d6efd');
  }

  private setDarkThemeProperties(): void {
    const root = document.documentElement;
    root.style.setProperty('--bs-body-bg', '#1a1d21');
    root.style.setProperty('--bs-body-color', '#ffffff');
    root.style.setProperty('--bs-card-bg', '#2d3339');
    root.style.setProperty('--bs-border-color', '#495057');
    root.style.setProperty('--bs-sidebar-bg', '#2d3339');
    root.style.setProperty('--bs-header-bg', '#2d3339');
    root.style.setProperty('--bs-content-bg', '#1a1d21');
    root.style.setProperty('--bs-nav-link-color', '#adb5bd');
    root.style.setProperty('--bs-nav-link-hover-color', '#74c0fc');
  }

  isDarkMode(): boolean {
    return this.currentTheme() === 'dark';
  }

  isLightMode(): boolean {
    return this.currentTheme() === 'light';
  }
}