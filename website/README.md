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

The support form does not collect credentials, upload files, or embed a GitHub token. It creates a prefilled official GitHub issue composer URL. A selected GBot remains local and must be attached by the reporter on GitHub after reviewing it for private data.
