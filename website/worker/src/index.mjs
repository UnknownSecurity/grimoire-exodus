const allowedCategories = new Set(['Bug', 'Feature', 'Support'])
const maximumRequestBytes = 24 * 1024
const minimumSubmitDelayMs = 1500
const maximumFormAgeMs = 24 * 60 * 60 * 1000

export default {
  async fetch(request, env) {
    const url = new URL(request.url)
    const origin = request.headers.get('Origin') || ''

    if (request.method === 'GET' && url.pathname === '/health') {
      return jsonResponse({ status: 'ok' }, 200, '*')
    }

    if (url.pathname !== '/issues') {
      return jsonResponse({ ok: false, error: 'Not found.' }, 404, allowedOrigin(origin, env))
    }

    const responseOrigin = allowedOrigin(origin, env)
    if (!responseOrigin) {
      return jsonResponse({ ok: false, error: 'Origin is not allowed.' }, 403, '')
    }

    if (request.method === 'OPTIONS') {
      return new Response(null, {
        status: 204,
        headers: corsHeaders(responseOrigin)
      })
    }

    if (request.method !== 'POST') {
      return jsonResponse({ ok: false, error: 'Method is not allowed.' }, 405, responseOrigin, {
        Allow: 'POST, OPTIONS'
      })
    }

    if (!request.headers.get('Content-Type')?.toLowerCase().startsWith('application/json')) {
      return jsonResponse({ ok: false, error: 'Expected a JSON request.' }, 415, responseOrigin)
    }

    const declaredLength = Number(request.headers.get('Content-Length') || 0)
    if (Number.isFinite(declaredLength) && declaredLength > maximumRequestBytes) {
      return jsonResponse({ ok: false, error: 'The report is too large.' }, 413, responseOrigin)
    }

    const clientAddress = request.headers.get('CF-Connecting-IP') || 'unknown'
    if (env.ISSUE_RATE_LIMITER?.limit) {
      const rateLimit = await env.ISSUE_RATE_LIMITER.limit({ key: clientAddress })
      if (!rateLimit.success) {
        return jsonResponse({ ok: false, error: 'Too many reports. Try again in a minute.' }, 429, responseOrigin, {
          'Retry-After': '60'
        })
      }
    }

    let payload
    try {
      const rawBody = await request.text()
      if (new TextEncoder().encode(rawBody).byteLength > maximumRequestBytes) {
        return jsonResponse({ ok: false, error: 'The report is too large.' }, 413, responseOrigin)
      }
      payload = JSON.parse(rawBody)
    } catch {
      return jsonResponse({ ok: false, error: 'The report could not be read.' }, 400, responseOrigin)
    }

    const validation = validateSubmission(payload)
    if (!validation.ok) {
      return jsonResponse({ ok: false, code: validation.code, error: validation.error }, 400, responseOrigin)
    }

    const owner = cleanRepositoryPart(env.GITHUB_OWNER)
    const repository = cleanRepositoryPart(env.GITHUB_REPO)
    if (!env.GITHUB_TOKEN || !owner || !repository) {
      return jsonResponse({ ok: false, error: 'Issue reporting is temporarily unavailable.' }, 503, responseOrigin)
    }

    const issue = validation.value
    const requestId = request.headers.get('CF-Ray') || crypto.randomUUID()
    const githubResponse = await fetch(`https://api.github.com/repos/${owner}/${repository}/issues`, {
      method: 'POST',
      headers: {
        Accept: 'application/vnd.github+json',
        Authorization: `Bearer ${env.GITHUB_TOKEN}`,
        'Content-Type': 'application/json',
        'User-Agent': 'grimoire-exodus-issue-relay',
        'X-GitHub-Api-Version': '2022-11-28'
      },
      body: JSON.stringify({
        title: `[${issue.category}] ${issue.subject}`,
        body: buildIssueBody(issue)
      })
    })

    if (!githubResponse.ok) {
      console.warn('GitHub issue creation failed.', { status: githubResponse.status, requestId })
      return jsonResponse({ ok: false, error: 'GitHub could not create the issue. Try again later.', requestId }, 502, responseOrigin)
    }

    const createdIssue = await githubResponse.json()
    const issueUrl = typeof createdIssue.html_url === 'string' ? createdIssue.html_url : ''
    const expectedPrefix = `https://github.com/${owner}/${repository}/issues/`
    if (!issueUrl.startsWith(expectedPrefix)) {
      console.warn('GitHub returned an unexpected issue URL.', { requestId })
      return jsonResponse({ ok: false, error: 'GitHub returned an unexpected response.', requestId }, 502, responseOrigin)
    }

    return jsonResponse({
      ok: true,
      issueUrl,
      issueNumber: Number(createdIssue.number) || null
    }, 201, responseOrigin)
  }
}

export function validateSubmission(payload, now = Date.now()) {
  if (!payload || typeof payload !== 'object' || Array.isArray(payload)) {
    return invalid('invalid_report', 'Enter the report details again.')
  }

  if (typeof payload.website !== 'string' || payload.website.trim()) {
    return invalid('invalid_report', 'The report could not be accepted.')
  }

  if (!Number.isFinite(payload.startedAt)) {
    return invalid('expired_form', 'Refresh the page and try again.')
  }

  const formAge = now - payload.startedAt
  if (formAge < minimumSubmitDelayMs) {
    return invalid('too_fast', 'Wait a moment, review the report, and try again.')
  }
  if (formAge > maximumFormAgeMs || formAge < 0) {
    return invalid('expired_form', 'Refresh the page and try again.')
  }

  if (payload.includeGbot === true) {
    return invalid('gbot_requires_github', 'GBot attachments must be submitted through GitHub.')
  }

  const category = typeof payload.category === 'string' ? payload.category.trim() : ''
  if (!allowedCategories.has(category)) {
    return invalid('invalid_category', 'Choose a valid report type.')
  }

  const subject = cleanInline(payload.subject)
  const details = cleanMultiline(payload.details)
  const steps = cleanMultiline(payload.steps)
  const version = cleanInline(payload.version)

  if (subject.length < 5 || subject.length > 100) {
    return invalid('invalid_subject', 'Use 5 to 100 characters for the subject.')
  }
  if (details.length < 20 || details.length > 4000) {
    return invalid('invalid_details', 'Use 20 to 4000 characters for the details.')
  }
  if (steps.length > 2000) {
    return invalid('invalid_steps', 'Additional context must be 2000 characters or fewer.')
  }
  if (version.length > 40) {
    return invalid('invalid_version', 'The version must be 40 characters or fewer.')
  }

  return {
    ok: true,
    value: { category, subject, details, steps, version }
  }
}

export function buildIssueBody(issue) {
  return [
    'Submitted anonymously through the official Grimoire Exodus website.',
    '',
    '## Report type',
    issue.category,
    '',
    '## Grimoire version',
    issue.version || 'Not specified',
    '',
    '## Details',
    issue.details,
    '',
    '## Steps or additional context',
    issue.steps || 'Not provided',
    '',
    '## GBot',
    'No GBot is included with this anonymous report.',
    '',
    '## Privacy checklist',
    '- [ ] The report does not contain passwords, account-vault data, tokens, private room names, or other sensitive information.',
    '- [ ] Grimoire Exodus was downloaded from the official repository or built from source.'
  ].join('\n')
}

function allowedOrigin(origin, env) {
  const allowed = String(env.ALLOWED_ORIGINS || '')
    .split(',')
    .map(value => value.trim())
    .filter(Boolean)
  return allowed.includes(origin) ? origin : ''
}

function cleanInline(value) {
  return typeof value === 'string'
    ? value.replace(/[\r\n`<>]/g, ' ').replace(/\s+/g, ' ').trim()
    : ''
}

function cleanMultiline(value) {
  return typeof value === 'string'
    ? value.replace(/\u0000/g, '').trim()
    : ''
}

function cleanRepositoryPart(value) {
  const candidate = typeof value === 'string' ? value.trim() : ''
  return /^[A-Za-z0-9_.-]+$/.test(candidate) ? candidate : ''
}

function invalid(code, error) {
  return { ok: false, code, error }
}

function corsHeaders(origin) {
  const headers = {
    'Access-Control-Allow-Headers': 'Content-Type',
    'Access-Control-Allow-Methods': 'POST, OPTIONS',
    'Cache-Control': 'no-store',
    'Content-Type': 'application/json; charset=utf-8',
    Vary: 'Origin'
  }
  if (origin) headers['Access-Control-Allow-Origin'] = origin
  return headers
}

function jsonResponse(body, status, origin, extraHeaders = {}) {
  return new Response(JSON.stringify(body), {
    status,
    headers: {
      ...corsHeaders(origin),
      ...extraHeaders
    }
  })
}
