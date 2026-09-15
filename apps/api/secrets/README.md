# Local secret staging (gitignored payloads)

Payload files matching `*.secret.json` are **gitignored**. Use them only to create/update AWS Secrets Manager.

## Create / update `documate/dev/api`

1. Ensure IAM user/role can call Secrets Manager (`CreateSecret`, `PutSecretValue`, `GetSecretValue`).
2. From repo root (PowerShell):

```powershell
aws secretsmanager create-secret `
  --name documate/dev/api `
  --region us-east-1 `
  --secret-string file://apps/api/secrets/documate-dev-api.secret.json
```

If the secret already exists:

```powershell
aws secretsmanager put-secret-value `
  --secret-id documate/dev/api `
  --region us-east-1 `
  --secret-string file://apps/api/secrets/documate-dev-api.secret.json
```

3. Local API bootstrap for **calling** SM uses the AWS default credential chain (`~/.aws/credentials` or env `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY`). That is separate from `Aws:*` keys **inside** the secret (used by Textract/S3 after load).

See [Plan 17](../../../docs/plans/17-platform-secrets-store.md) and [example JSON](../../../docs/ops/secrets-manager-secret.example.json).
