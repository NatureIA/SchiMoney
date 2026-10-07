(() => {
  if (!('serviceWorker' in navigator)) return;

  const RESET_KEY = 'schimoney-pwa-reset-v5';
  const CONTROL_KEY = 'schimoney-pwa-controlled-v5';

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

    if (installButton) {
      installButton.hidden = false;
    }
  });

  window.addEventListener('appinstalled', () => {
    deferredPrompt = null;

    if (installButton) {
      installButton.hidden = true;
    }
  });

  async function resetOldPwaStateOnce() {
    if (isStandalone()) return false;

    try {
      if (localStorage.getItem(RESET_KEY) === '1') return false;

      const registrations = await navigator.serviceWorker.getRegistrations();
      await Promise.all(registrations.map(registration => registration.unregister()));

      if ('caches' in window) {
        const keys = await caches.keys();
        await Promise.all(
          keys
            .filter(key => key.startsWith('schimoney-static-'))
            .map(key => caches.delete(key))
        );
      }

      localStorage.setItem(RESET_KEY, '1');
      location.replace(location.href);
      return true;
    } catch (err) {
      console.error('PWA reset:', err);
      return false;
    }
  }

  window.addEventListener('load', async () => {
    ensureInstallButton();

    try {
      const reloading = await resetOldPwaStateOnce();
      if (reloading) return;

      const registration = await navigator.serviceWorker.register(
        '/service-worker.js?v=5',
        {
          scope: '/',
          updateViaCache: 'none'
        }
      );

      await registration.update();
      await navigator.serviceWorker.ready;

      if (!navigator.serviceWorker.controller && sessionStorage.getItem(CONTROL_KEY) !== '1') {
        sessionStorage.setItem(CONTROL_KEY, '1');
        location.replace(location.href);
      }
    } catch (err) {
      console.error('PWA:', err);
    }
  });
})();
