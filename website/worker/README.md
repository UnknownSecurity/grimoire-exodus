# GitHub issue relay Worker

This Cloudflare Worker lets the public website create text-only GitHub issues without exposing a repository credential to visitors. It accepts requests only from the official GitHub Pages origin and local development origins, validates and bounds every field, rejects GBot attachments, and applies a per-address Cloudflare rate limit.

## Credential

Create a fine-grained GitHub personal access token with access only to `UnknownSecurity/grimoire-exodus` and these repository permissions:

- Metadata: read
- Issues: read and write

Store it only as an encrypted Cloudflare secret:

```powershell
cd website
pnpm exec wrangler secret put GITHUB_TOKEN --config worker/wrangler.jsonc
```

Never put the token in source, `.dev.vars`, GitHub Pages configuration, browser storage, logs, or issue content.

## Validate and deploy

```powershell
cd website
pnpm worker:test
pnpm worker:check
pnpm worker:deploy
```

The safe health check is `GET /health`. Do not create a test issue on the live endpoint unless a maintainer explicitly wants that public issue created.

GBot files remain local. GitHub's issue API does not provide a safe anonymous attachment flow, so selecting a GBot continues through GitHub's authenticated issue composer.
