/**
 * Cloudflare Worker: Umbraco JSON Schema Proxy
 *
 * Serves versioned umbraco-package-schema.json files from jsDelivr (npm).
 *
 * Supported routes:
 *   GET /umbraco-package/latest.json           → latest stable npm release
 *   GET /umbraco-package/17.2.2.json           → specific version
 *   GET /umbraco-package/v17/latest.json       → latest stable release within major v17
 *
 * Path history on npm (@umbraco-cms/backoffice):
 *   v14.0.0 – v17.1.x  →  dist-cms/umbraco-package-schema.json
 *   v17.2.0+            →  umbraco-package-schema.json  (root)
 *
 * The worker tries the new (root) path first and falls back to dist-cms/ on 404,
 * so it handles all versions transparently without needing to know the exact cutoff.
 */

const NPM_PACKAGE = '@umbraco-cms/backoffice';
const JSDELIVR_BASE = 'https://cdn.jsdelivr.net/npm';

// Candidate paths in preference order (newest location first).
// v17.2.0+ uses the root path; v14.0.0–v17.1.x used dist-cms/.
const SCHEMA_PATHS = [
  'umbraco-package-schema.json',
  'dist-cms/umbraco-package-schema.json',
];

// Matches standard semver (with optional pre-release tag) or the literal "latest"
const VALID_VERSION = /^(\d+\.\d+\.\d+[^\s/]*)|(latest)$/;

function textResponse(body, status) {
  return new Response(body, {
    status,
    headers: { 'Content-Type': 'text/plain' },
  });
}

async function resolveLatestForMajor(major) {
  const registryUrl = `https://registry.npmjs.org/${NPM_PACKAGE}`;
  const meta = await fetch(registryUrl).then((r) => r.json());
  const versions = Object.keys(meta.versions ?? {}).filter(
    (v) => v.startsWith(`${major}.`) && !v.includes('-'),
  );
  if (!versions.length) return null;
  return versions.sort((a, b) => b.localeCompare(a, undefined, { numeric: true }))[0];
}

async function fetchSchema(npmVersion) {
  for (const path of SCHEMA_PATHS) {
    const url = `${JSDELIVR_BASE}/${NPM_PACKAGE}@${npmVersion}/${path}`;
    const response = await fetch(url);
    if (response.ok) return response;
  }
  return null;
}

export default {
  async fetch(request) {
    const { pathname } = new URL(request.url);

    // Route: /umbraco-package/{version}.json  (version = semver or "latest")
    const exactMatch = pathname.match(/^\/umbraco-package\/([^/]+)\.json$/);
    // Route: /umbraco-package/v{major}/latest.json
    const majorLatestMatch = pathname.match(/^\/umbraco-package\/(v\d+)\/latest\.json$/);

    let npmVersion;
    let isImmutable = false;

    try {
      if (majorLatestMatch) {
        const major = majorLatestMatch[1].replace('v', '');
        npmVersion = await resolveLatestForMajor(major);
        if (!npmVersion) {
          return textResponse(
            `No stable release found for major version ${majorLatestMatch[1]}.\n` +
            `Check https://www.npmjs.com/package/${NPM_PACKAGE}?activeTab=versions for available versions.`,
            404,
          );
        }
      } else if (exactMatch) {
        npmVersion = exactMatch[1];

        if (!VALID_VERSION.test(npmVersion)) {
          return textResponse(
            `Invalid version "${npmVersion}".\n\n` +
            `Use a valid semver (e.g. 17.2.2) or "latest".\n` +
            `Available versions: https://www.npmjs.com/package/${NPM_PACKAGE}?activeTab=versions`,
            400,
          );
        }

        // Concrete semver (not "latest" and not a pre-release tag like -rc) — cache forever
        isImmutable = npmVersion !== 'latest' && /^\d+\.\d+\.\d+$/.test(npmVersion);
      } else {
        return textResponse(
          [
            'Not Found',
            '',
            'Valid paths:',
            '  /umbraco-package/{version}.json          e.g. /umbraco-package/17.2.2.json',
            '  /umbraco-package/latest.json',
            '  /umbraco-package/v{major}/latest.json    e.g. /umbraco-package/v17/latest.json',
            '',
            `Schema is available from v14.0.0 onwards.`,
          ].join('\n'),
          404,
        );
      }

      const upstream = await fetchSchema(npmVersion);

      if (!upstream) {
        return textResponse(
          `Schema not found for version "${npmVersion}".\n\n` +
          `The schema is available from v14.0.0 onwards.\n` +
          `Available versions: https://www.npmjs.com/package/${NPM_PACKAGE}?activeTab=versions`,
          404,
        );
      }

      return new Response(upstream.body, {
        status: 200,
        headers: {
          'Content-Type': 'application/schema+json',
          'Access-Control-Allow-Origin': '*',
          'Cache-Control': isImmutable
            ? 'public, max-age=31536000, immutable'
            : 'public, max-age=300, stale-while-revalidate=60',
        },
      });
    } catch (err) {
      return textResponse(
        `Upstream error: unable to reach the npm registry or jsDelivr. Please try again shortly.`,
        502,
      );
    }
  },
};
