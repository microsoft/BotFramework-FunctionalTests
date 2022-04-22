// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// @ts-check

import path from 'path';
import { existsSync } from 'fs';
import { readFile, writeFile, mkdir, rm, readdir } from 'fs/promises';
import { fileURLToPath } from 'url';
import { spawn } from 'child_process';

/**
 * Path variables
 */
const paths = {
  // @ts-ignore
  dirname: () => path.dirname(fileURLToPath(import.meta.url)),
  env: () => path.resolve(paths.dirname(), '../.env'),
  package: () => path.resolve(process.cwd(), 'package.json'),
  packageTemp: (name) => path.resolve(paths.temp(), `package.${name}.json`),
  temp: () => path.resolve(paths.dirname(), 'temp')
};

main();

/**
 * The root functionality of the script, it reads and process based on the cmd arguments.
 * @returns {Promise<void>}
 */
async function main () {
  const [, script, action] = process.argv;

  switch (action) {
    case 'replace': {
      const env = await readEnv();
      await replacePackage(env);
      break;
    }

    case 'restore':
      spawn('node', [script, 'detached'], {
        stdio: 'ignore',
        detached: true
      }).unref();
      // Wait a bit before finishing the process, giving time to the 'spawn' to start.
      await wait(250);
      break;

    case 'detached':
      do {
        await wait(250);
      } while (isRunning(process.ppid));
      await restorePackage();
      break;

    default:
      throw new Error(
        `Unable to recognize the action '${action}'. Supported actions [replace, restore].`
      );
  }
}

/**
 * Reads the environment variables from the root, project and OS.
 * @returns {Promise<Record<string, string>>}
 */
async function readEnv () {
  const pkgEnv = await readFile('.env', 'utf8');
  const rootEnv = await readFile(paths.env(), 'utf8');

  const env = [pkgEnv, rootEnv].join('\n');
  const lines = env.replace(/\r\n?/gm, '\n').split('\n');

  const result = lines
    .filter((e) => e?.trim())
    .reduce((acc, val) => {
      const [key, value] = val.split('=');
      acc[key] = value;
      return acc;
    }, {});

  return Object.entries({ ...result, ...process.env }).reduce(
    (acc, [key, val]) => {
      acc[key] = escape(val);
      return acc;
    },
    {}
  );
}

/**
 * Process and replace the project package.json, assigning the environment variables defined as '$VARIABLE'.
 * @param {Record<string, string>} env
 * @returns {Promise<void>}
 */
async function replacePackage (env) {
  const raw = await readFile(paths.package(), 'utf8');
  const processed = Object.entries(env).reduce(
    (acc, [key, val]) => acc.replace(new RegExp(`\\$${key}`, 'gmi'), val),
    raw
  );

  if (raw === processed) {
    return;
  }

  if (!existsSync(paths.temp())) {
    await mkdir(paths.temp());
  }

  const json = JSON.parse(raw);
  await writeFile(paths.packageTemp(json.name), raw);
  await writeFile(paths.package(), processed);
}

/**
 * Restores the original package.json.
 * @returns {Promise<void>}
 */
async function restorePackage () {
  const raw = await readFile(paths.package(), 'utf8');
  const json = JSON.parse(raw);
  const temp = paths.packageTemp(json.name);
  const original = await readFile(temp, 'utf8');

  await writeFile(paths.package(), original);
  const files = await readdir(paths.temp());

  if (files.length - 1 <= 0) {
    await rm(paths.temp(), { recursive: true, force: true });
  } else {
    await rm(temp, { force: true });
  }
}

/**
 * Checks if a process is still running.
 * @param {number} pid
 * @returns {boolean}
 */
function isRunning (pid) {
  try {
    return process.kill(pid, 0);
  } catch (e) {
    return e.code === 'EPERM';
  }
}

/**
 * Waits on async/await cases based on a specific time.
 * @param {number} time
 * @returns {Promise<void>}
 */
function wait (time) {
  return new Promise((resolve) => setTimeout(resolve, time));
}

/**
 * Escapes unwanted characters from a string.
 * @param {string} value
 * @returns {string}
 */
function escape (value) {
  // Escape string [\ ' "]
  return value.replace(/[\\$'"]/g, '\\$&');
}
