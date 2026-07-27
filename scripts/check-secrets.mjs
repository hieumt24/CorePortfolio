import { execFileSync } from 'node:child_process';
import { readFileSync } from 'node:fs';
import { basename } from 'node:path';

const detectors = [
  ['Telegram bot token', /\b\d{6,12}:[A-Za-z0-9_-]{30,}\b/g],
  ['GitHub token', /\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{40,})\b/g],
  ['CoinGecko API key', /\bCG-[A-Za-z0-9_-]{20,}\b/g],
  ['OpenAI API key', /\bsk-[A-Za-z0-9_-]{20,}\b/g],
  ['AWS access key', /\b(?:AKIA|ASIA)[A-Z0-9]{16}\b/g],
  ['Slack token', /\bxox[baprs]-[A-Za-z0-9-]{20,}\b/g],
  ['Azure storage account key', /\bAccountKey=[A-Za-z0-9+/=]{20,}/g],
  ['Private key', /-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----/g],
];

const placeholderPattern = /^(?:<.*>|\$\{.*\}|__.*__|changeme|example|placeholder)$/i;
const trackedFiles = execFileSync('git', ['ls-files', '-z'])
  .toString('utf8')
  .split('\0')
  .filter(Boolean);
const findings = [];

function lineNumber(text, offset) {
  return text.slice(0, offset).split('\n').length;
}

function inspectConfigValue(file, path, value) {
  if (typeof value === 'string') {
    const normalizedPath = path.join('.');
    const isCredential = /(?:token|api.?key|password|secret)/i.test(normalizedPath)
      || /jwt.*key/i.test(normalizedPath);
    if (isCredential && value.trim() && !placeholderPattern.test(value.trim())) {
      findings.push({ detector: 'Non-empty credential setting', file, location: normalizedPath });
    }
    return;
  }

  if (value && typeof value === 'object' && !Array.isArray(value)) {
    for (const [key, child] of Object.entries(value)) {
      inspectConfigValue(file, [...path, key], child);
    }
  }
}

for (const file of trackedFiles) {
  const buffer = readFileSync(file);
  if (buffer.includes(0)) continue;
  const text = buffer.toString('utf8');

  for (const [detector, pattern] of detectors) {
    pattern.lastIndex = 0;
    for (const match of text.matchAll(pattern)) {
      findings.push({ detector, file, location: `line ${lineNumber(text, match.index ?? 0)}` });
    }
  }

  if (/^appsettings(?:\.[^.]+)*\.json$/i.test(basename(file))) {
    try {
      inspectConfigValue(file, [], JSON.parse(text));
    } catch {
      // Invalid JSON is handled by the application build; do not duplicate that diagnostic here.
    }
  }
}

if (findings.length > 0) {
  console.error('Potential committed credentials detected:');
  for (const finding of findings) {
    console.error(`- ${finding.detector}: ${finding.file} (${finding.location})`);
  }
  console.error('Move credentials to User Secrets or deployment environment variables before committing.');
  process.exit(1);
}

console.log(`Secret check passed: ${trackedFiles.length} tracked files scanned.`);
