// Keeps the app itself on the device so it opens instantly, with or without a connection.
//
// Only the app's files are cached here. Anything that talks to the server (/api, /auth, the
// Google sign-in callback) always goes to the network; the app keeps its own copy of your last
// data and queues your changes until the server can be reached.
//
// A new deploy is picked up in the background and used from the next time Amiki opens.

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall()));
self.addEventListener('activate', event => event.waitUntil(onActivate()));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'amiki-app-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const include = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff2?$/, /\.png$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.webmanifest$/];
const exclude = [/^service-worker\.js$/];
const networkOnly = [/^\/api\//, /^\/auth\//, /^\/signin-google/];

async function onInstall() {
    const requests = self.assetsManifest.assets
        .filter(asset => include.some(pattern => pattern.test(asset.url)))
        .filter(asset => !exclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    const cache = await caches.open(cacheName);
    await cache.addAll(requests);
    // index.html loads the framework script by its plain name, which the server maps to the
    // current hashed file; cache it under that name too.
    await cache.add(new Request('_framework/blazor.webassembly.js', { cache: 'no-cache' }));
    await self.skipWaiting();
}

async function onActivate() {
    const names = await caches.keys();
    await Promise.all(names
        .filter(name => name.startsWith(cacheNamePrefix) && name !== cacheName)
        .map(name => caches.delete(name)));
    await self.clients.claim();
}

async function onFetch(event) {
    const url = new URL(event.request.url);
    if (event.request.method !== 'GET' || url.origin !== self.location.origin || networkOnly.some(p => p.test(url.pathname))) {
        return fetch(event.request);
    }

    const cache = await caches.open(cacheName);
    // Every page of the app is the same index.html; serve it from the device, instantly.
    const request = event.request.mode === 'navigate' ? 'index.html' : event.request;
    return (await cache.match(request)) || fetch(event.request);
}
