import assert from 'node:assert/strict'
import test from 'node:test'
import { buildIssueBody, validateSubmission } from '../src/index.mjs'

const now = 2_000_000

function validSubmission(overrides = {}) {
  return {
    category: 'Bug',
    subject: 'Bot stops during combat',
    version: 'v1.4.0',
    details: 'The bot stops while processing a kill-for command.',
    steps: 'Load the bot and start it in the expected room.',
    includeGbot: false,
    website: '',
    startedAt: now - 5000,
    ...overrides
  }
}

test('accepts a valid anonymous report', () => {
  const result = validateSubmission(validSubmission(), now)
  assert.equal(result.ok, true)
  assert.equal(result.value.subject, 'Bot stops during combat')
})

test('rejects reports submitted unrealistically quickly', () => {
  const result = validateSubmission(validSubmission({ startedAt: now - 500 }), now)
  assert.deepEqual(result, {
    ok: false,
    code: 'too_fast',
    error: 'Wait a moment, review the report, and try again.'
  })
})

test('requires GitHub when a GBot is selected', () => {
  const result = validateSubmission(validSubmission({ includeGbot: true }), now)
  assert.equal(result.ok, false)
  assert.equal(result.code, 'gbot_requires_github')
})

test('rejects the honeypot field', () => {
  const result = validateSubmission(validSubmission({ website: 'https://spam.invalid' }), now)
  assert.equal(result.ok, false)
  assert.equal(result.code, 'invalid_report')
})

test('rejects a malformed honeypot field', () => {
  const result = validateSubmission(validSubmission({ website: null }), now)
  assert.equal(result.ok, false)
  assert.equal(result.code, 'invalid_report')
})

test('sanitizes a multiline issue title', () => {
  const result = validateSubmission(validSubmission({ subject: 'Broken\n`title`' }), now)
  assert.equal(result.ok, true)
  assert.equal(result.value.subject, 'Broken title')
})

test('builds an anonymous issue body without a GBot payload', () => {
  const result = validateSubmission(validSubmission(), now)
  const body = buildIssueBody(result.value)
  assert.match(body, /Submitted anonymously/)
  assert.match(body, /No GBot is included/)
  assert.doesNotMatch(body, /Selected file:/)
})
