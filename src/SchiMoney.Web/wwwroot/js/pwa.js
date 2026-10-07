(() => {
  if (!('serviceWorker' in navigator)) return;

  const RESET_KEY = 'schimoney-pwa-reset-20261007-2';
  const CONTROL_KEY = 'schimoney-pwa-control-20261007-2';

  let deferredPrompt = null;
  let installButton = null;

  function isStandalone() {
    return window.matchMedia('(display-mode: standalone)').matches ||
      window.navigator.standalone === true;
  }

  function ensureInstallButton() {
    if (installButton || isStandalone()) return;

    installButton = document.createElement('button');
    installButton.type = 'button';
    installButton.className = 'pwa-install';
    installButton.textContent = 'Instalar SchiMoney';
    installButton.hidden = true;
    document.body.appendChild(installButton);

    installButton.addEventListener('click', async () => {
      if (!deferredPrompt) return;
      deferredPrompt.prompt();
      await deferredPrompt.userChoice;
      deferredPrompt = null;
      installButton.hidden = true;
    });
  }

  window.addEventListener('beforeinstallprompt', event => {
    event.preventDefault();
    deferredPrompt = event;
    ensureInstallButton();
    if (installButton) installButton.hidden = false;
  });

  window.addEventListener('appinstalled', () => {
    deferredPrompt = null;
    if (installButton) installButton.hidden = true;
  });

  async function resetOldStateOnce() {
    if (isStandalone() || localStorage.getItem(RESET_KEY) === '1') return false;

    const registrations = await navigator.serviceWorker.getRegistrations();
    await Promise.all(registrations.map(reg => reg.unregister()));

    if ('caches' in window) {
      const keys = await caches.keys();
      await Promise.all(
        keys
          .filter(key => key.startsWith('schimoney-static-'))
          .map(key => caches.delete(key))
      );
    }

    localStorage.setItem(RESET_KEY, '1');
    location.reload();
    return true;
  }

  window.addEventListener('load', async () => {
    ensureInstallButton();

    try {
      if (await resetOldStateOnce()) return;

      const registration = await navigator.serviceWorker.register(
        '/service-worker.js?v=12',
        { scope: '/', updateViaCache: 'none' }
      );

      await registration.update();
      await navigator.serviceWorker.ready;

      if (!navigator.serviceWorker.controller &&
          sessionStorage.getItem(CONTROL_KEY) !== '1') {
        sessionStorage.setItem(CONTROL_KEY, '1');
        location.reload();
      }
    } catch (err) {
      console.error('PWA:', err);
    }
  });
})();
