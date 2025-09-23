import { Pipe, PipeTransform, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

@Pipe({
  name: 'highlight',
  standalone: true
})
export class HighlightPipe implements PipeTransform {
  private readonly sanitizer = inject(DomSanitizer);

  transform(
    text: string | null | undefined,
    search: string | null | undefined,
    cssClass: string = 'highlight',
    caseSensitive: boolean = false
  ): SafeHtml {
    if (!text || !search) {
      return text || '';
    }

    const flags = caseSensitive ? 'g' : 'gi';
    const regex = new RegExp(`(${this.escapeRegExp(search)})`, flags);

    const highlightedText = text.replace(
      regex,
      `<span class="${cssClass}">$1</span>`
    );

    return this.sanitizer.bypassSecurityTrustHtml(highlightedText);
  }

  private escapeRegExp(string: string): string {
    return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  }
}