import { readdir, readFile } from 'node:fs/promises';
import path from 'node:path';
import process from 'node:process';

const repositoryRoot = process.cwd();
const sourceRoots = ['backend', 'frontend', 'docs', '.agents', '.github', 'scripts'];
const ignoredDirectories = new Set([
  '.git',
  'bin',
  'dist',
  'node_modules',
  'obj',
  'TestResults',
  'wwwroot',
]);
const textExtensions = new Set([
  '.cs',
  '.csproj',
  '.css',
  '.html',
  '.js',
  '.json',
  '.md',
  '.mjs',
  '.ps1',
  '.slnx',
  '.ts',
  '.tsx',
  '.xml',
  '.yaml',
  '.yml',
]);
const utf8Decoder = new TextDecoder('utf-8', { fatal: true });
const suspiciousMojibake =
  /\uFFFD|Ã[\u0080-\u00BF]|Â[\u0080-\u00BF]|Ä[\u0080-\u00BF]|â(?:[\u0080-\u00BF]|[\u2000-\u206F])|ð[\u0080-\u00BF]/u;

async function collectTextFiles(directory) {
  const entries = await readdir(directory, { withFileTypes: true });
  const files = [];

  for (const entry of entries) {
    if (ignoredDirectories.has(entry.name))
      continue;

    const entryPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      files.push(...await collectTextFiles(entryPath));
    } else if (textExtensions.has(path.extname(entry.name).toLowerCase())) {
      files.push(entryPath);
    }
  }

  return files;
}

const failures = [];
for (const sourceRoot of sourceRoots) {
  const absoluteRoot = path.join(repositoryRoot, sourceRoot);
  for (const filePath of await collectTextFiles(absoluteRoot)) {
    let text;
    try {
      text = utf8Decoder.decode(await readFile(filePath));
    } catch {
      failures.push(`${path.relative(repositoryRoot, filePath)}: invalid UTF-8`);
      continue;
    }

    const lines = text.split(/\r?\n/u);
    lines.forEach((line, index) => {
      if (suspiciousMojibake.test(line))
        failures.push(`${path.relative(repositoryRoot, filePath)}:${index + 1}: suspicious mojibake`);
    });
  }
}

if (failures.length > 0) {
  console.error('Text encoding check failed:');
  failures.forEach(failure => console.error(`- ${failure}`));
  process.exitCode = 1;
} else {
  console.log('Text encoding check passed: all scanned source files are valid UTF-8 without common mojibake markers.');
}
