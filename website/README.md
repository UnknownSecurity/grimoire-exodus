# Grimoire Exodus website

This is a static-friendly Nuxt site for the official Grimoire Exodus download and support entry point. It uses Tailwind CSS, Lucide icons, and repository-owned artwork without duplicating those assets.

## Local development

```powershell
cd website
pnpm install
pnpm dev
```

## Production output

```powershell
pnpm generate
```

The generated site is written to `.output/public`. Set `NUXT_APP_BASE_URL` when deploying below a subpath, such as `/grimoire-exodus/` on a project GitHub Pages site.

Pushes to `main` that change the website or its referenced artwork are deployed by `.github/workflows/deploy-pages.yml` to:

<https://unknownsecurity.github.io/grimoire-exodus/>

Text-only support reports are sent through the Cloudflare Worker in `worker/`, which keeps the repository-scoped GitHub credential out of the static site and browser. The Worker validates and bounds every field, restricts browser origins, and applies a per-address rate limit before creating an issue.

The support form does not collect credentials or upload files. A selected GBot always remains local and uses the authenticated GitHub issue composer because GitHub does not provide an anonymous issue-attachment API. See `worker/README.md` for credential scope, validation, and deployment instructions.
