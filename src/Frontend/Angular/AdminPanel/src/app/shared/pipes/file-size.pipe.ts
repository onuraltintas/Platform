import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'fileSize',
  standalone: true
})
export class FileSizePipe implements PipeTransform {
  transform(
    bytes: number | string | null | undefined,
    precision: number = 2,
    longForm: boolean = false
  ): string {
    if (bytes == null || bytes === '') {
      return '0 B';
    }

    const numericBytes = typeof bytes === 'string' ? parseFloat(bytes) : bytes;

    if (isNaN(numericBytes) || numericBytes < 0) {
      return '0 B';
    }

    const units = longForm
      ? ['Bytes', 'Kilobytes', 'Megabytes', 'Gigabytes', 'Terabytes', 'Petabytes']
      : ['B', 'KB', 'MB', 'GB', 'TB', 'PB'];

    let unitIndex = 0;
    let size = numericBytes;

    while (size >= 1024 && unitIndex < units.length - 1) {
      size /= 1024;
      unitIndex++;
    }

    // Precision'ı uygula
    const formattedSize = unitIndex === 0
      ? size.toString() // Bytes için decimal gösterme
      : size.toFixed(precision);

    return `${formattedSize} ${units[unitIndex]}`;
  }
}