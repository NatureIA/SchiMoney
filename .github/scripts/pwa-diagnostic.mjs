import { chromium } from 'playwright';

const browser = await chromium.launch({ channel: 'chrome', headless: true });
const context = await browser.newContext();
const page = await context.newPage();

try {
  await page.goto('https://schimoney.runasp.net/', { waitUntil: 'networkidle', timeout: 60000 });
  await page.waitForTimeout(7000);

  const runtime = await page.evaluate(async () => {
    const manifestHref = document.querySelector('link[rel="manifest"]')?.href ?? null;
    let registration = null;
    let ready = false;

    if ('serviceWorker' in navigator) {
      registration = await navigator.serviceWorker.getRegistration('/');
      try {
        await Promise.race([
          navigator.serviceWorker.ready.then(() => { ready = true; }),
          new Promise(resolve => setTimeout(resolve, 5000))
        ]);
      } catch {}
    }

    return {
      href: location.href,
      secureContext: window.isSecureContext,
      manifestHref,
      serviceWorkerSupported: 'serviceWorker' in navigator,
      serviceWorkerRegistered: Boolean(registration),
      serviceWorkerReady: ready,
      serviceWorkerControlled: Boolean(navigator.serviceWorker?.controller)
    };
  });

  const cdp = await context.newCDPSession(page);
  const manifest = await cdp.send('Page.getAppManifest');
  const installability = await cdp.send('Page.getInstallabilityErrors');

  console.log('PWA_RUNTIME=' + JSON.stringify(runtime));
  console.log('PWA_MANIFEST_URL=' + JSON.stringify(manifest.url || null));
  console.log('PWA_MANIFEST_ERRORS=' + JSON.stringify(manifest.errors || []));
  console.log('PWA_INSTALLABILITY_ERRORS=' + JSON.stringify(installability.installabilityErrors || []));

  const hardErrors = [];
  if (!runtime.secureContext) hardErrors.push({ reason: 'not-secure-context' });
  if (!runtime.manifestHref) hardErrors.push({ reason: 'manifest-link-missing' });
  if ((manifest.errors || []).length) hardErrors.push(...manifest.errors);
  if ((installability.installabilityErrors || []).length) hardErrors.push(...installability.installabilityErrors);

  if (hardErrors.length) {
    console.error('PWA_HARD_ERRORS=' + JSON.stringify(hardErrors));
    process.exitCode = 2;
  }
} finally {
  await browser.close();
}
