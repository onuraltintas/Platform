import { Pipe, PipeTransform } from '@angular/core';

type SortDirection = 'asc' | 'desc';

@Pipe({
  name: 'sort',
  standalone: true,
  pure: false // Değişiklikleri yakalamak için
})
export class SortPipe implements PipeTransform {
  transform<T>(
    items: T[] | null | undefined,
    property?: keyof T | ((item: T) => unknown),
    direction: SortDirection = 'asc'
  ): T[] {
    if (!items || !Array.isArray(items)) {
      return [];
    }

    if (!property) {
      return items;
    }

    const sortedItems = [...items].sort((a, b) => {
      let valueA: unknown;
      let valueB: unknown;

      if (typeof property === 'function') {
        valueA = property(a);
        valueB = property(b);
      } else {
        valueA = a[property];
        valueB = b[property];
      }

      // Null/undefined değerleri son sıraya koy
      if (valueA == null && valueB == null) return 0;
      if (valueA == null) return 1;
      if (valueB == null) return -1;

      // Tarih karşılaştırması
      if (valueA instanceof Date && valueB instanceof Date) {
        return this.compareValues(valueA.getTime(), valueB.getTime(), direction);
      }

      // String karşılaştırması (case-insensitive)
      if (typeof valueA === 'string' && typeof valueB === 'string') {
        valueA = valueA.toLowerCase();
        valueB = valueB.toLowerCase();
      }

      return this.compareValues(valueA, valueB, direction);
    });

    return sortedItems;
  }

  private compareValues(a: unknown, b: unknown, direction: SortDirection): number {
    let result = 0;

    if (a < b) {
      result = -1;
    } else if (a > b) {
      result = 1;
    }

    return direction === 'desc' ? -result : result;
  }
}