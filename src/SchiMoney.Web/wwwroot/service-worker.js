const CACHE_NAME = "schimoney-static-v1";
const STATIC_ASSETS = [
    "/offline.html",
    "/manifest.json",
    "/icons/icon-192.png",
    "/icons/icon-512.png"
];

self.addEventListener("install", (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then((cache) => cache.addAll(STATIC_ASSETS))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener("activate", (event) => {
    event.waitUntil(
        caches.keys()
            .then((keys) =>
                Promise.all(
                    keys
                        .filter((key) => key.startsWith("schimoney-static-") && key !== CACHE_NAME)
                        .map((key) => caches.delete(key))
                )
            )
            .then(() => self.clients.claim())
    );
});

self.addEventListener("fetch", (event) => {
    const request = event.request;

    if (request.method !== "GET") return;

    const url = new URL(request.url);

    if (url.origin !== self.location.origin) return;

    if (request.mode === "navigate") {
        event.respondWith(
            fetch(request, { cache: "no-store" })
                .catch(() => caches.match("/offline.html"))
        );
        return;
    }

    const isStaticAsset =
        url.pathname.startsWith("/css/") ||
        url.pathname.startsWith("/js/") ||
        url.pathname.startsWith("/icons/") ||
        url.pathname === "/manifest.json";

    if (!isStaticAsset) return;

    event.respondWith(
        fetch(request, { cache: "no-store" })
            .then(async (response) => {
                if (response.ok) {
                    const cache = await caches.open(CACHE_NAME);
                    await cache.put(request, response.clone());
                }

                return response;
            })
            .catch(async () => {
                const cached = await caches.match(request);
                return cached || Response.error();
            })
    );
});
