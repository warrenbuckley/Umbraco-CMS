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

// Candidate paths in preference order (newest location first)
const SCHEMA_PATHS = [
  'umbraco-package-schema.json',
  'dist-cms/umbraco-package-schema.json',
];

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

    if (majorLatestMatch) {
      const major = majorLatestMatch[1].replace('v', '');
      npmVersion = await resolveLatestForMajor(major);
      if (!npmVersion) {
        return new Response(`No stable release found for major version ${majorLatestMatch[1]}`, {
          status: 404,
        });
      }
    } else if (exactMatch) {
      npmVersion = exactMatch[1];
      // A concrete semver (not "latest") is immutable — cache forever
      isImmutable = npmVersion !== 'latest' && /^\d+\.\d+\.\d+/.test(npmVersion);
    } else {
      return new Response(
        [
          'Not Found',
          '',
          'Valid paths:',
          '  /umbraco-package/{version}.json          e.g. /umbraco-package/17.2.2.json',
          '  /umbraco-package/latest.json',
          '  /umbraco-package/v{major}/latest.json    e.g. /umbraco-package/v17/latest.json',
        ].join('\n'),
        { status: 404, headers: { 'Content-Type': 'text/plain' } },
      );
    }

    const upstream = await fetchSchema(npmVersion);

    if (!upstream) {
      return new Response(`Schema not found for version "${npmVersion}"`, { status: 404 });
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
  },
};
