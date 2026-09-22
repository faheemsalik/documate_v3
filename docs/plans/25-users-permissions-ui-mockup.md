# Users & Permissions UI Mockup (DQ-2011)

> **Status:** ✅ Approved for Band 20 batch execution (2026-09-22) — implement DQ-2012/2013 against this mockup  
> **Routes:** `/users`, `/users/:id`  
> **Gate:** Mockup approved with Phase 3 batch execute.

## `/users` — list

```
+--------------------------------------------------+
| Users                         [ Invite user ]    |
+--------------------------------------------------+
| Name            Email              Role    Status|
| Ali Hassan      ali@acme.com       Admin   Active|
| Fatima Khan     fatima@acme.com    Files   Active|
+--------------------------------------------------+
```

- Table columns: Display name, Email, Role template, Active
- Primary CTA: **Invite user** (opens dialog: email, display name, role select)
- Row click → `/users/:id`

## `/users/:id` — edit / permissions

```
+--------------------------------------------------+
| ← Users / Ali Hassan                             |
+--------------------------------------------------+
| Profile                                          |
|   Email: ali@acme.com                            |
|   Display name: Ali Hassan                       |
|   Status: Active          [ Deactivate ]         |
+--------------------------------------------------+
| Access                                           |
|   Role:  ( ) Admin  (•) Editor  ( ) Files op     |
|          ( ) Viewer                              |
|   [ Save role ]                                  |
+--------------------------------------------------+
| Effective permissions (from role — read-only)    |
|   ✓ Agents list/view/create/update               |
|   ✗ Agents delete                                |
|   ✓ Files list/view/upload/update                |
|   ✗ Files delete                                 |
|   …                                              |
+--------------------------------------------------+
```

### Rules reflected in UI

- Access is set by **role template**, not a free-form FeatureKey matrix (MVP).
- Effective FeatureKeys are informational (from Iden role grants).
- Requires FeatureKeys: `users.list`, `users.invite`, `users.permissions.manage`, `users.deactivate`.

## Approval

- [x] Developer approved mockup via Band 20 batch execute (2026-09-22)
