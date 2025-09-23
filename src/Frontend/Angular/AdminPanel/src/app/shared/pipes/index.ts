// Pipes barrel exports
export * from './safe-html.pipe';
export * from './truncate.pipe';
export * from './date-format.pipe';
export * from './translate.pipe';
export * from './currency-format.pipe';
export * from './file-size.pipe';
export * from './highlight.pipe';
export * from './filter.pipe';
export * from './sort.pipe';

// Pipe array for easy importing
import { SafeHtmlPipe } from './safe-html.pipe';
import { TruncatePipe } from './truncate.pipe';
import { DateFormatPipe } from './date-format.pipe';
import { TranslatePipe } from './translate.pipe';
import { CurrencyFormatPipe } from './currency-format.pipe';
import { FileSizePipe } from './file-size.pipe';
import { HighlightPipe } from './highlight.pipe';
import { FilterPipe } from './filter.pipe';
import { SortPipe } from './sort.pipe';

export const SHARED_PIPES = [
  SafeHtmlPipe,
  TruncatePipe,
  DateFormatPipe,
  TranslatePipe,
  CurrencyFormatPipe,
  FileSizePipe,
  HighlightPipe,
  FilterPipe,
  SortPipe
] as const;