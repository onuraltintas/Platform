import { Pipe, PipeTransform, inject, LOCALE_ID } from '@angular/core';
import { DatePipe } from '@angular/common';

@Pipe({
  name: 'dateFormat',
  standalone: true
})
export class DateFormatPipe implements PipeTransform {
  private readonly locale = inject(LOCALE_ID);
  private readonly datePipe = new DatePipe(this.locale);

  transform(
    value: Date | string | number | null | undefined,
    format: string = 'short',
    timezone?: string,
    locale?: string
  ): string | null {
    if (!value) {
      return null;
    }

    // Türkçe özel formatlar
    const turkishFormats: { [key: string]: string } = {
      'tr-short': 'dd.MM.yyyy',
      'tr-medium': 'dd.MM.yyyy HH:mm',
      'tr-long': 'dd MMMM yyyy',
      'tr-full': 'dd MMMM yyyy HH:mm:ss',
      'tr-time': 'HH:mm',
      'tr-datetime': 'dd.MM.yyyy HH:mm:ss',
      'tr-date-only': 'dd.MM.yyyy',
      'relative': 'relative' // Özel işlem için
    };

    // Relative time için özel işlem
    if (format === 'relative') {
      return this.getRelativeTime(value);
    }

    // Türkçe format varsa kullan
    const actualFormat = turkishFormats[format] || format;
    const actualLocale = locale || this.locale;

    return this.datePipe.transform(value, actualFormat, timezone, actualLocale);
  }

  private getRelativeTime(value: Date | string | number): string {
    const date = new Date(value);
    const now = new Date();
    const diffInMs = now.getTime() - date.getTime();
    const diffInSeconds = Math.floor(diffInMs / 1000);
    const diffInMinutes = Math.floor(diffInSeconds / 60);
    const diffInHours = Math.floor(diffInMinutes / 60);
    const diffInDays = Math.floor(diffInHours / 24);
    const diffInMonths = Math.floor(diffInDays / 30);
    const diffInYears = Math.floor(diffInDays / 365);

    if (diffInSeconds < 0) {
      return 'Gelecekte';
    } else if (diffInSeconds < 60) {
      return 'Az önce';
    } else if (diffInMinutes < 60) {
      return `${diffInMinutes} dakika önce`;
    } else if (diffInHours < 24) {
      return `${diffInHours} saat önce`;
    } else if (diffInDays < 30) {
      return `${diffInDays} gün önce`;
    } else if (diffInMonths < 12) {
      return `${diffInMonths} ay önce`;
    } else {
      return `${diffInYears} yıl önce`;
    }
  }
}