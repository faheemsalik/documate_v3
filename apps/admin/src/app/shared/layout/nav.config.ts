export type NavItem = {
  label: string;
  route: string;
};

export type NavGroup = {
  label: string;
  items: NavItem[];
  defaultOpen?: boolean;
};

export const NAV_GROUPS: NavGroup[] = [
  {
    label: 'Overview',
    defaultOpen: true,
    items: [
      { label: 'Dashboard', route: '/dashboard' },
      { label: 'Files & documents', route: '/ops' },
      { label: 'Support workspace', route: '/support' },
    ],
  },
  {
    label: 'Customers',
    defaultOpen: true,
    items: [
      { label: 'Tenants', route: '/tenants' },
      { label: 'Businesses', route: '/businesses' },
    ],
  },
  {
    label: 'Platform',
    defaultOpen: true,
    items: [
      { label: 'API monitoring', route: '/monitoring' },
      { label: 'System settings', route: '/settings' },
    ],
  },
];
