/* Basic service worker for Web Push notifications */
self.addEventListener('install', (event) => {
  // Activate immediately
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  // Claim clients so SW controls open pages without reload
  event.waitUntil(self.clients.claim());
});

self.addEventListener('push', (event) => {
  // Debug: log that push arrived
  try { console.debug('[SW] push event received'); } catch {}
  let payload = {};
  try {
    payload = event.data ? event.data.json() : {};
  } catch {
    try { payload = { body: event.data ? event.data.text() : '' }; } catch {}
  }
  const title = payload.title || 'Nouvelle alerte';
  const options = {
    body: (payload.body || payload.message) || 'Vous avez reçu une nouvelle alerte.',
    data: { url: payload.url || (payload.data && payload.data.url) || '/Dashboard' },
    icon: '/favicon.ico',
    badge: '/favicon.ico',
    vibrate: [100, 50, 100],
    requireInteraction: false,
  };
  event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener('notificationclick', (event) => {
  event.notification.close();
  const targetUrl = (event.notification && event.notification.data && event.notification.data.url) || '/Dashboard';
  event.waitUntil((async () => {
    const allClients = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
    for (const client of allClients) {
      if ('focus' in client) { client.focus(); }
      if ('navigate' in client) { client.navigate(targetUrl); }
      return;
    }
    if (self.clients.openWindow) await self.clients.openWindow(targetUrl);
  })());
});

/* Service Worker for Web Push and offline basics */
self.addEventListener('install', event => {
  self.skipWaiting();
});

self.addEventListener('activate', event => {
  event.waitUntil(self.clients.claim());
});

self.addEventListener('push', event => {
  try {
    const data = event.data ? event.data.json() : {};
    const title = data.title || 'Alerte';
    const options = {
      body: data.body || '',
      icon: data.icon || '/icon-192x192.png',
      badge: data.badge || '/badge-72x72.png',
      data: data.data || { url: '/Dashboard' },
      tag: data.tag || 'alert-notification',
      requireInteraction: !!data.requireInteraction,
      actions: data.actions || []
    };
    event.waitUntil(self.registration.showNotification(title, options));
  } catch (e) {
    // fallback
    event.waitUntil(self.registration.showNotification('Alerte', { body: 'Nouvelle alerte' }));
  }
});

self.addEventListener('notificationclick', event => {
  event.notification.close();
  const targetUrl = (event.notification && event.notification.data && event.notification.data.url) || '/';
  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then(windowClients => {
      for (let i = 0; i < windowClients.length; i++) {
        const client = windowClients[i];
        if (client.url && client.url.includes(targetUrl) && 'focus' in client) {
          return client.focus();
        }
      }
      if (clients.openWindow) {
        return clients.openWindow(targetUrl);
      }
    })
  );
});


