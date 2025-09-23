import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'filter',
  standalone: true,
  pure: false // Değişiklikleri yakalamak için
})
export class FilterPipe implements PipeTransform {
  transform<T>(
    items: T[] | null | undefined,
    filterFn: (item: T, index: number, array: T[]) => boolean
  ): T[] {
    if (!items || !Array.isArray(items)) {
      return [];
    }

    if (!filterFn || typeof filterFn !== 'function') {
      return items;
    }

    return items.filter(filterFn);
  }
}