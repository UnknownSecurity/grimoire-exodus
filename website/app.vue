<script setup lang="ts">
import {
  Bug,
  CircleCheck,
  Download,
  ExternalLink,
  FileArchive,
  FlaskConical,
  GitFork,
  Lightbulb,
  MessageCircle,
  Paperclip,
  Send,
  ShieldCheck,
  Sparkles
} from '@lucide/vue'
import bannerUrl from '../docs/assets/grimoire-github-banner.png'
import aqwHeaderUrl from '../app/Resources/Home/images/aqworlds-master-header.jpg'
import ashfallUrl from '../app/Resources/Home/images/ashfall-dragons.png'
import faviconUrl from '../app/Resources/Brand/grimoire-exodus.ico'

useHead({
  link: [{ rel: 'icon', type: 'image/x-icon', href: faviconUrl }]
})

interface GitHubAsset {
  name: string
  browser_download_url: string
}

interface GitHubRelease {
  name?: string
  tag_name?: string
  assets?: GitHubAsset[]
}

const repositoryUrl = 'https://github.com/UnknownSecurity/grimoire-exodus'
const releasesUrl = repositoryUrl + '/releases'
const stableReleaseUrl = releasesUrl + '/latest'
const securityUrl = repositoryUrl + '/security/advisories/new'

const downloadUrl = ref(stableReleaseUrl)
const releaseLabel = ref('Latest stable Windows release')
const releaseDetail = ref('Official installer and portable ZIP')
const attachment = ref<File | null>(null)
const formError = ref('')

const categories = [
  { value: 'Bug', label: 'Bug report', icon: Bug },
  { value: 'Feature', label: 'Feature request', icon: Lightbulb },
  { value: 'Support', label: 'General support', icon: MessageCircle }
]

const form = reactive({
  category: 'Bug',
  subject: '',
  version: '',
  details: '',
  steps: '',
  includeGbot: false
})

const canSubmit = computed(() =>
  form.subject.trim().length >= 5
  && form.details.trim().length >= 20
  && (!form.includeGbot || attachment.value !== null)
)

onMounted(async () => {
  try {
    const response = await fetch('https://api.github.com/repos/UnknownSecurity/grimoire-exodus/releases/latest', {
      headers: { Accept: 'application/vnd.github+json' }
    })
    if (!response.ok) return

    const release = await response.json() as GitHubRelease
    const installer = (release.assets || []).find(asset =>
      asset.name.toLowerCase().includes('setup') && asset.name.toLowerCase().endsWith('.msi')
    )
    if (installer) downloadUrl.value = installer.browser_download_url
    releaseLabel.value = release.name || release.tag_name || releaseLabel.value
    releaseDetail.value = installer ? 'Official Windows installer' : 'Official release downloads'
  } catch {
    // The official releases page remains a safe fallback when GitHub's API is unavailable.
  }
})

function onGbotSelected(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0] || null
  formError.value = ''

  if (!file) {
    attachment.value = null
    return
  }

  const lowerName = file.name.toLowerCase()
  if (!lowerName.endsWith('.gbot') && !lowerName.endsWith('.zip')) {
    attachment.value = null
    input.value = ''
    formError.value = 'Choose a .gbot file or a ZIP containing the GBot.'
    return
  }

  if (file.size > 2 * 1024 * 1024) {
    attachment.value = null
    input.value = ''
    formError.value = 'The selected GBot package must be 2 MB or smaller.'
    return
  }

  attachment.value = file
}

function cleanInline(value: string) {
  return value.replace(/[\r\n`<>]/g, ' ').replace(/\s+/g, ' ').trim()
}

function submitIssue() {
  formError.value = ''
  if (!canSubmit.value) {
    formError.value = form.includeGbot && !attachment.value
      ? 'Choose the GBot you want to reference before continuing.'
      : 'Add a subject and at least 20 characters of detail before continuing.'
    return
  }

  const selectedFile = attachment.value
  const attachmentSection = form.includeGbot && selectedFile
    ? [
        '## GBot',
        `Selected file: \`${cleanInline(selectedFile.name)}\` (${Math.max(1, Math.ceil(selectedFile.size / 1024))} KB)`,
        '',
        '> The website does not upload local files or store bot contents. Please drag this file into the GitHub issue before submitting it. Remove credentials, usernames, private room names, and other private data first.'
      ].join('\n')
    : '## GBot\nNo GBot is included with this report.'

  const body = [
    'Submitted from the Grimoire Exodus website.',
    '',
    '## Report type',
    form.category,
    '',
    '## Grimoire version',
    form.version.trim() || 'Not specified',
    '',
    '## Details',
    form.details.trim(),
    '',
    '## Steps or additional context',
    form.steps.trim() || 'Not provided',
    '',
    attachmentSection,
    '',
    '## Privacy checklist',
    '- [ ] I removed passwords, account-vault data, tokens, private room names, and other sensitive information.',
    '- [ ] I downloaded Grimoire Exodus from the official repository or built it from source.'
  ].join('\n')

  const parameters = new URLSearchParams({
    template: 'website_contact.md',
    title: `[${form.category}] ${form.subject.trim()}`,
    body
  })
  window.location.assign(`${repositoryUrl}/issues/new?${parameters.toString()}`)
}
</script>

<template>
  <main class="relative min-h-screen overflow-hidden bg-[#0d0b12] text-zinc-100">
    <div
      class="pointer-events-none absolute inset-x-0 top-0 h-[34rem] bg-cover bg-center opacity-30 [mask-image:linear-gradient(to_bottom,black,transparent)]"
      :style="{ backgroundImage: `linear-gradient(to bottom, rgba(13,11,18,.1), #0d0b12), url(${aqwHeaderUrl})` }"
      aria-hidden="true"
    />
    <div class="pointer-events-none absolute left-1/2 top-40 h-96 w-96 -translate-x-1/2 rounded-full bg-exodus-600/20 blur-[110px]" aria-hidden="true" />

    <div class="relative mx-auto flex w-full max-w-5xl flex-col items-center px-5 py-8 sm:px-8 sm:py-12">
      <nav class="flex w-full items-center justify-between" aria-label="Main navigation">
        <a href="#download" class="flex items-center gap-2 text-sm font-semibold tracking-wide text-zinc-200">
          <Sparkles class="h-4 w-4 text-exodus-300" aria-hidden="true" />
          Grimoire Exodus
        </a>
        <a
          :href="repositoryUrl"
          target="_blank"
          rel="noreferrer"
          class="inline-flex items-center gap-2 rounded-full border border-white/10 bg-black/30 px-4 py-2 text-sm text-zinc-300 transition hover:border-exodus-400/50 hover:text-white focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-exodus-300"
        >
          <GitFork class="h-4 w-4" aria-hidden="true" />
          GitHub
        </a>
      </nav>

      <section id="download" class="mt-12 w-full text-center sm:mt-16">
        <div class="mx-auto mb-5 inline-flex items-center gap-2 rounded-full border border-exodus-400/25 bg-exodus-900/50 px-3 py-1 text-xs font-semibold uppercase tracking-[.2em] text-exodus-200">
          <ShieldCheck class="h-3.5 w-3.5" aria-hidden="true" />
          Official source
        </div>

        <img
          :src="bannerUrl"
          alt="Grimoire Exodus — an enchanted book between opposing dragons"
          class="mx-auto w-full max-w-4xl rounded-2xl border border-exodus-400/20 object-cover shadow-arcane"
        >

        <div class="mx-auto -mt-8 max-w-2xl rounded-2xl border border-white/10 bg-[#17131f]/95 px-6 py-8 shadow-2xl backdrop-blur sm:-mt-12 sm:px-10">
          <p class="text-xs font-semibold uppercase tracking-[.28em] text-exodus-300">Windows desktop client</p>
          <h1 class="mt-3 text-3xl font-black tracking-tight text-white sm:text-5xl">Your adventure. One command away.</h1>
          <p class="mx-auto mt-4 max-w-xl text-sm leading-6 text-zinc-400 sm:text-base">
            Download Grimoire Exodus only from the official GitHub repository. Every release includes verified Windows packages and preserves legacy GBot compatibility.
          </p>

          <div class="mt-7 flex flex-col items-center justify-center gap-3 sm:flex-row">
            <a
              :href="downloadUrl"
              class="inline-flex min-h-12 w-full items-center justify-center gap-2 rounded-xl bg-exodus-500 px-6 py-3 font-bold text-white shadow-lg shadow-exodus-600/25 transition hover:-translate-y-0.5 hover:bg-exodus-400 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-exodus-200 sm:w-auto"
            >
              <Download class="h-5 w-5" aria-hidden="true" />
              Download Grimoire Exodus
            </a>
            <a
              :href="releasesUrl"
              target="_blank"
              rel="noreferrer"
              class="inline-flex min-h-12 w-full items-center justify-center gap-2 rounded-xl border border-white/10 bg-white/5 px-5 py-3 font-semibold text-zinc-200 transition hover:border-exodus-400/40 hover:bg-white/10 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-exodus-200 sm:w-auto"
            >
              All releases
              <ExternalLink class="h-4 w-4" aria-hidden="true" />
            </a>
          </div>

          <div class="mt-5 flex flex-wrap items-center justify-center gap-x-5 gap-y-2 text-xs text-zinc-500">
            <span class="inline-flex items-center gap-1.5"><CircleCheck class="h-3.5 w-3.5 text-emerald-400" aria-hidden="true" />{{ releaseLabel }}</span>
            <span class="inline-flex items-center gap-1.5"><FileArchive class="h-3.5 w-3.5 text-exodus-300" aria-hidden="true" />{{ releaseDetail }}</span>
          </div>
        </div>
      </section>

      <section class="mt-16 grid w-full gap-4 sm:grid-cols-3" aria-label="Download assurances">
        <article class="rounded-2xl border border-white/10 bg-white/[.035] p-5">
          <ShieldCheck class="h-6 w-6 text-emerald-400" aria-hidden="true" />
          <h2 class="mt-4 font-bold text-white">Verified releases</h2>
          <p class="mt-2 text-sm leading-6 text-zinc-500">Official packages include published SHA-256 checksums and the built-in verified updater.</p>
        </article>
        <article class="rounded-2xl border border-white/10 bg-white/[.035] p-5">
          <FlaskConical class="h-6 w-6 text-exodus-300" aria-hidden="true" />
          <h2 class="mt-4 font-bold text-white">Stable by default</h2>
          <p class="mt-2 text-sm leading-6 text-zinc-500">Experimental releases stay opt-in from the desktop app's Advanced settings.</p>
        </article>
        <article class="rounded-2xl border border-white/10 bg-white/[.035] p-5">
          <FileArchive class="h-6 w-6 text-amber-300" aria-hidden="true" />
          <h2 class="mt-4 font-bold text-white">Legacy GBots</h2>
          <p class="mt-2 text-sm leading-6 text-zinc-500">Existing GBot files remain a compatibility contract across desktop updates.</p>
        </article>
      </section>

      <section id="contact" class="relative mt-20 w-full overflow-hidden rounded-3xl border border-exodus-400/20 bg-[#17131f] shadow-arcane">
        <div
          class="pointer-events-none absolute right-0 top-0 h-72 w-96 bg-cover bg-center opacity-15 [mask-image:linear-gradient(to_left,black,transparent)]"
          :style="{ backgroundImage: `url(${ashfallUrl})` }"
          aria-hidden="true"
        />

        <div class="relative grid gap-10 p-6 sm:p-10 lg:grid-cols-[.78fr_1.22fr] lg:p-12">
          <div>
            <div class="inline-flex items-center gap-2 text-xs font-semibold uppercase tracking-[.22em] text-exodus-300">
              <MessageCircle class="h-4 w-4" aria-hidden="true" />
              Contact &amp; issues
            </div>
            <h2 class="mt-4 text-3xl font-black tracking-tight text-white">Tell us what needs attention.</h2>
            <p class="mt-4 text-sm leading-7 text-zinc-400">
              Fill out the report here and continue to GitHub, where you can review and submit the issue. Your GitHub account becomes the contact—this site never asks for an email address or GitHub token.
            </p>

            <div class="mt-7 rounded-2xl border border-amber-300/15 bg-amber-300/[.04] p-4 text-sm leading-6 text-zinc-400">
              <strong class="text-amber-200">Keep reports public-safe.</strong>
              Remove passwords, character credentials, account-vault data, private room names, tokens, and personal information before continuing.
            </div>

            <a
              :href="securityUrl"
              target="_blank"
              rel="noreferrer"
              class="mt-5 inline-flex items-center gap-2 text-sm font-semibold text-exodus-300 hover:text-exodus-200 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-4 focus-visible:outline-exodus-300"
            >
              Report a security issue privately
              <ExternalLink class="h-4 w-4" aria-hidden="true" />
            </a>
          </div>

          <form class="space-y-5" @submit.prevent="submitIssue">
            <div class="grid gap-5 sm:grid-cols-2">
              <label class="block text-sm font-semibold text-zinc-200">
                Report type
                <select
                  v-model="form.category"
                  class="mt-2 min-h-12 w-full rounded-xl border border-white/10 bg-[#211b2b] px-4 text-zinc-100 outline-none transition focus:border-exodus-400 focus:ring-2 focus:ring-exodus-500/20"
                >
                  <option v-for="category in categories" :key="category.value" :value="category.value">{{ category.label }}</option>
                </select>
              </label>

              <label class="block text-sm font-semibold text-zinc-200">
                Grimoire version <span class="font-normal text-zinc-500">(optional)</span>
                <input
                  v-model="form.version"
                  maxlength="40"
                  placeholder="Example: 1.4.0"
                  class="mt-2 min-h-12 w-full rounded-xl border border-white/10 bg-[#211b2b] px-4 text-zinc-100 outline-none placeholder:text-zinc-600 focus:border-exodus-400 focus:ring-2 focus:ring-exodus-500/20"
                >
              </label>
            </div>

            <label class="block text-sm font-semibold text-zinc-200">
              Subject
              <input
                v-model="form.subject"
                required
                minlength="5"
                maxlength="100"
                placeholder="A short summary"
                class="mt-2 min-h-12 w-full rounded-xl border border-white/10 bg-[#211b2b] px-4 text-zinc-100 outline-none placeholder:text-zinc-600 focus:border-exodus-400 focus:ring-2 focus:ring-exodus-500/20"
              >
            </label>

            <label class="block text-sm font-semibold text-zinc-200">
              What happened or what should change?
              <textarea
                v-model="form.details"
                required
                minlength="20"
                maxlength="4000"
                rows="6"
                placeholder="Describe the issue, expected behavior, or feature idea."
                class="mt-2 w-full resize-y rounded-xl border border-white/10 bg-[#211b2b] px-4 py-3 leading-6 text-zinc-100 outline-none placeholder:text-zinc-600 focus:border-exodus-400 focus:ring-2 focus:ring-exodus-500/20"
              />
            </label>

            <label class="block text-sm font-semibold text-zinc-200">
              Steps or additional context <span class="font-normal text-zinc-500">(optional)</span>
              <textarea
                v-model="form.steps"
                maxlength="2000"
                rows="4"
                placeholder="List reproduction steps, relevant settings, or other context."
                class="mt-2 w-full resize-y rounded-xl border border-white/10 bg-[#211b2b] px-4 py-3 leading-6 text-zinc-100 outline-none placeholder:text-zinc-600 focus:border-exodus-400 focus:ring-2 focus:ring-exodus-500/20"
              />
            </label>

            <div class="rounded-2xl border border-white/10 bg-black/15 p-4">
              <label class="flex cursor-pointer items-start gap-3">
                <input v-model="form.includeGbot" type="checkbox" class="mt-1 h-4 w-4 accent-[#8c4bd3]">
                <span>
                  <span class="block text-sm font-semibold text-zinc-200">Include a GBot with this report</span>
                  <span class="mt-1 block text-xs leading-5 text-zinc-500">The selected file stays on your computer. GitHub will ask you to attach it after the issue composer opens.</span>
                </span>
              </label>

              <label v-if="form.includeGbot" class="mt-4 flex cursor-pointer items-center gap-3 rounded-xl border border-dashed border-exodus-400/35 bg-exodus-900/20 p-4 text-sm text-zinc-300 transition hover:border-exodus-300/60">
                <Paperclip class="h-5 w-5 text-exodus-300" aria-hidden="true" />
                <span class="min-w-0 flex-1 truncate">{{ attachment ? attachment.name : 'Choose a .gbot or .zip file' }}</span>
                <span class="rounded-lg bg-white/5 px-3 py-1.5 text-xs font-semibold text-zinc-300">Browse</span>
                <input class="sr-only" type="file" accept=".gbot,.zip" @change="onGbotSelected">
              </label>
            </div>

            <p v-if="formError" role="alert" class="rounded-xl border border-red-400/20 bg-red-500/10 px-4 py-3 text-sm text-red-200">{{ formError }}</p>

            <button
              type="submit"
              class="inline-flex min-h-12 w-full items-center justify-center gap-2 rounded-xl bg-exodus-500 px-6 py-3 font-bold text-white transition hover:bg-exodus-400 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-exodus-200 disabled:cursor-not-allowed disabled:opacity-50"
              :disabled="!canSubmit"
            >
              <Send class="h-5 w-5" aria-hidden="true" />
              Continue to GitHub
            </button>
          </form>
        </div>
      </section>

      <footer class="mt-12 flex w-full flex-col items-center justify-between gap-4 border-t border-white/10 py-7 text-center text-xs text-zinc-600 sm:flex-row sm:text-left">
        <p>Grimoire Exodus is an independent community project. Download only from the official repository.</p>
        <a :href="repositoryUrl" target="_blank" rel="noreferrer" class="inline-flex items-center gap-2 text-zinc-500 hover:text-zinc-300">
          <GitFork class="h-4 w-4" aria-hidden="true" />
          Source on GitHub
        </a>
      </footer>
    </div>
  </main>
</template>
