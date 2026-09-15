# SES inbound runbook — docsintake.com (Plan 14 EI-5)

One-time ops setup. Portal mailbox create does **not** call SES APIs.

## 1. Domain identity

1. In AWS SES (same region as `EmailIntake:AwsRegion`), create identity for `docsintake.com`.
2. Publish DKIM CNAMEs + verification TXT to DNS.
3. Wait until identity status = Verified.

## 2. MX for inbound

Point MX for `docsintake.com` (or chosen inbound subdomain later) at SES inbound SMTP for the region, e.g.:

```text
10 inbound-smtp.us-west-2.amazonaws.com
```

(Use the MX SES shows for your region.)

## 3. S3 bucket for raw MIME

1. Create bucket (e.g. `documate-email-inbound`).
2. SES receipt rule needs a bucket policy allowing `ses.amazonaws.com` to `s3:PutObject`.
3. Set `EmailIntake:S3Bucket` / `S3Prefix` in Documate config (prefix optional).

## 4. Receipt rule

1. Rule set active for verified domain.
2. Recipient condition: domain `docsintake.com` (catch-all).
3. Action: **S3** → bucket/prefix (object key includes MIME).
4. Optional: SNS topic on receipt → HTTPS endpoint:

```text
POST https://{api-host}/api/internal/email-intake/sns
```

5. If `EmailIntake:InboundWebhookSecret` is set, send header `X-Documate-Inbound-Secret: {secret}` (custom via Lambda fan-out) or leave unset for SNS signature-only confirmation in early dogfood.

## 5. Documate config

```json
"Aws": {
  "AccessKey": null,
  "SecretKey": null
},
"EmailIntake": {
  "DefaultDomain": "docsintake.com",
  "AwsRegion": "us-east-1",
  "S3Bucket": "docsintake-dev-…",
  "S3Prefix": "inbound/",
  "MaxAttachmentBytes": 26214400,
  "MaxTotalAttachmentBytes": 52428800,
  "MaxAttachments": 20,
  "RateLimitPerMailboxPerMinute": 20,
  "RateLimitPerMailboxPerHour": 200,
  "InboundWebhookSecret": null
}
```

IAM for the API/worker: `s3:GetObject` on that bucket/prefix (shared `Aws` keys or IAM role).

## 6. Confirm

1. Create typed or multi mailbox in portal → copy address.
2. Send a test PDF to that address.
3. Hangfire job `EmailIntakeJobs.ProcessS3ObjectAsync` should create File(s) or IntakeRejection.
4. Unknown addresses are dropped (log + `email_intake.unknown_recipient` metric).

## 7. Simulate without SES

```http
POST /api/app/intake-mailboxes/{id}/simulate
```

Use for CI and dogfood before MX is live.

## 8. Before selling email hard (allowlist checklist)

- [ ] Mailbox allowlist mode = **`allowlist_enforced`** (Agent intake tab and/or Channels).
- [ ] At least one email or domain entry for expected senders.
- [ ] Smoke: unknown From → `OpsIntakeRejections` with `allowlist_rejected`.
- [ ] Smoke: allowlisted From with PDF → Files created.
- [ ] Rate limits reviewed for tenant volume (`RateLimitPerMailboxPerMinute` / `Hour`).
- [ ] Do **not** leave production mailboxes on `open` once partners rely on the address.

## 9. Raw MIME retention (Plan 14 F1)

Hangfire recurring job `email-intake-mime-retention` runs **daily** (`EmailIntakeJobs.PurgeExpiredMimeAsync`).

1. Reads `EmailIntake:MimeRetentionDays` from system settings (default **30**; seeded from appsettings).
2. Lists objects under `EmailIntake:S3Bucket` + `S3Prefix`.
3. Deletes objects whose `LastModified` is older than N days.
4. Metric: `email_intake.mime_deleted`.

IAM needs `s3:ListBucket` (prefix) + `s3:DeleteObject` on the intake prefix (in addition to `GetObject`).

Trigger manually from Hangfire dashboard (Development) or:

```csharp
BackgroundJob.Enqueue<EmailIntakeJobs>(j => j.PurgeExpiredMimeAsync());
```

Adjust retention via `PUT /api/admin/system-settings/EmailIntake%3AMimeRetentionDays` with admin credentials (`Auth:AdminGate`).
