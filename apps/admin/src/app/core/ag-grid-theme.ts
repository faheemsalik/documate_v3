import { themeQuartz } from 'ag-grid-community';

export const documateAgTheme = themeQuartz.withParams({
  accentColor: 'var(--p-primary-color)',
  backgroundColor: 'var(--p-content-background)',
  foregroundColor: 'var(--p-text-color)',
  borderColor: 'var(--p-content-border-color)',
  headerBackgroundColor: 'var(--p-surface-100)',
  headerTextColor: 'var(--p-text-muted-color)',
  oddRowBackgroundColor: 'var(--p-surface-50)',
  rowHoverColor: 'var(--p-content-hover-background)',
  selectedRowBackgroundColor: 'var(--p-highlight-background)',
  fontFamily: 'var(--p-font-family)',
  fontSize: '13px',
  borderRadius: '6px',
  wrapperBorderRadius: '8px',
});
