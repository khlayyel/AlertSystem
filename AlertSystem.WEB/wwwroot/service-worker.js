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


