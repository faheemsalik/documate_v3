export type StatusSeverity = 'success' | 'warn' | 'danger' | 'info' | 'secondary';

const STATUS_SEVERITY: Record<string, StatusSeverity> = {
  ready: 'success',
  partial_ready: 'warn',
  processing: 'info',
  received: 'secondary',
  failed: 'danger',
  rejected: 'danger',
  cancelled: 'secondary',
};

export function statusSeverity(statusKey?: string | null): StatusSeverity {
  if (!statusKey) return 'secondary';
  return STATUS_SEVERITY[statusKey] ?? 'secondary';
}

export function formatStatusLabel(statusKey?: string | null): string {
  if (!statusKey) return 'Unknown';
  return statusKey.replace(/_/g, ' ');
}

export const FILE_STATUS_FILTER_OPTIONS = [
  { label: 'All statuses', value: null },
  { label: 'Received', value: 'received' },
  { label: 'Processing', value: 'processing' },
  { label: 'Ready', value: 'ready' },
  { label: 'Partial ready', value: 'partial_ready' },
  { label: 'Failed', value: 'failed' },
  { label: 'Rejected', value: 'rejected' },
  { label: 'Cancelled', value: 'cancelled' },
] as { label: string; value: string | null }[];
