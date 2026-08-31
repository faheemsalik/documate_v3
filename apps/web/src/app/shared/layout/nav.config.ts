/** Nav B — Nanonets-style (Plan 07 lock). */

export type NavItem = {
  label: string;
  route: string;
  /** When true, page is a stub until a later DQ. */
  comingSoon?: boolean;
};

export type NavGroup = {
  label: string;
  items: NavItem[];
  defaultOpen?: boolean;
};

export const NAV_PINNED: NavItem[] = [
  { label: 'My agents', route: '/agents' },
  { label: 'New agent', route: '/agents/templates' },
];

export const NAV_GROUPS: NavGroup[] = [
  {
    label: 'Overview',
    defaultOpen: true,
    items: [
      { label: 'Dashboard', route: '/home' },
      { label: 'Usage stats', route: '/coming-soon/usage', comingSoon: true },
    ],
  },
  {
    label: 'Documents',
    items: [
      { label: 'Extract data', route: '/files' },
      { label: 'Schema search', route: '/files/search' },
    ],
  },
  {
    label: 'Workflow',
    items: [{ label: 'Channels & intake', route: '/queues' }],
  },
  {
    label: 'Settings',
    items: [
      { label: 'Business profile', route: '/business/profile' },
      { label: 'Switch business', route: '/business/switch' },
      { label: 'API keys', route: '/api-keys' },
    ],
  },
];

export const NAV_HELP: NavItem[] = [
  { label: 'Documentation', route: '/docs' },
  { label: 'Support requests', route: '/coming-soon/support', comingSoon: true },
  { label: 'Workflows · soon', route: '/coming-soon/workflows', comingSoon: true },
];
