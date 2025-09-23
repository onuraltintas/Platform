import { Pipe, PipeTransform, inject, LOCALE_ID } from '@angular/core';
import { CurrencyPipe } from '@angular/common';

@Pipe({
  name: 'currencyFormat',
  standalone: true
})
export class CurrencyFormatPipe implements PipeTransform {
  private readonly locale = inject(LOCALE_ID);
  private readonly currencyPipe = new CurrencyPipe(this.locale);

  transform(
    value: number | string | null | undefined,
    currencyCode: string = 'TRY',
    display: 'code' | 'symbol' | 'symbol-narrow' | string = 'symbol',
    digitsInfo?: string,
    locale?: string
  ): string | null {
    if (value == null || value === '') {
      return null;
    }

    const numericValue = typeof value === 'string' ? parseFloat(value) : value;

    if (isNaN(numericValue)) {
      return null;
    }

    // Türk Lirası için özel format
    if (currencyCode === 'TRY') {
      const formatted = this.currencyPipe.transform(
        numericValue,
        'TRY',
        display,
        digitsInfo || '1.2-2',
        locale || 'tr-TR'
      );
      return formatted;
    }

    // Diğer para birimleri için
    return this.currencyPipe.transform(
      numericValue,
      currencyCode,
      display,
      digitsInfo || '1.2-2',
      locale || this.locale
    );
  }
}