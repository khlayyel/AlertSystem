// Service Worker pour AlertSystem - Web Push Notifications
const CACHE_NAME = 'alertsystem-v1';
const urlsToCache = [
  '/',
  '/Dashboard',
  '/css/site.css',
  '/js/site.js',
  '/gmail-dashboard.js',
  '/favicon.ico'
];

// Installation du Service Worker
self.addEventListener('install', (event) => {
  console.log('[SW] Installing...');
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then((cache) => {
        console.log('[SW] Caching app shell');
        return cache.addAll(urlsToCache);
      })
      .then(() => self.skipWaiting())
  );
});

// Activation du Service Worker
self.addEventListener('activate', (event) => {
  console.log('[SW] Activating...');
  event.waitUntil(
    caches.keys().then((cacheNames) => {
      return Promise.all(
        cacheNames.map((cacheName) => {
          if (cacheName !== CACHE_NAME) {
            console.log('[SW] Deleting old cache:', cacheName);
            return caches.delete(cacheName);
          }
        })
      );
    }).then(() => self.clients.claim())
  );
});

// Gestion des notifications push
self.addEventListener('push', (event) => {
  console.log('[SW] Push received:', event);
  
  let notificationData = {
    title: 'Nouvelle Alerte',
    body: 'Vous avez reçu une nouvelle alerte',
    icon: '/favicon.ico',
    badge: '/favicon.ico',
    tag: 'alert-notification',
    requireInteraction: true,
    actions: [
      { action: 'view', title: 'Voir', icon: '/favicon.ico' },
      { action: 'dismiss', title: 'Ignorer', icon: '/favicon.ico' }
    ],
    data: { url: '/Dashboard' }
  };

  if (event.data) {
    try {
      const payload = event.data.json();
      notificationData = { ...notificationData, ...payload };
    } catch (e) {
      console.log('[SW] Error parsing push data:', e);
      notificationData.body = event.data.text();
    }
  }

  event.waitUntil(
    self.registration.showNotification(notificationData.title, {
      body: notificationData.body,
      icon: notificationData.icon,
      badge: notificationData.badge,
      tag: notificationData.tag,
      requireInteraction: notificationData.requireInteraction,
      actions: notificationData.actions,
      data: notificationData.data,
      vibrate: [200, 100, 200],
      timestamp: Date.now()
    })
  );
});

// Gestion des clics sur les notifications
self.addEventListener('notificationclick', (event) => {
  console.log('[SW] Notification clicked:', event);
  
  event.notification.close();
  
  const action = event.action;
  const notificationData = event.notification.data || {};
  const url = notificationData.url || '/Dashboard';
  
  if (action === 'dismiss') {
    // Ne rien faire, juste fermer la notification
    return;
  }
  
  // Action 'view' ou clic sur la notification
  event.waitUntil(
    (async () => {
      const allClients = await self.clients.matchAll({ 
        type: 'window', 
        includeUncontrolled: true 
      });
      
      // Chercher une fenêtre existante avec l'URL
      for (const client of allClients) {
        if (client.url.includes('/Dashboard') && 'focus' in client) {
          await client.focus();
          if ('navigate' in client) {
            await client.navigate(url);
          }
          return;
        }
      }
      
      // Ouvrir une nouvelle fenêtre
      await self.clients.openWindow(url);
    })()
  );
});

// Gestion des messages depuis l'application
self.addEventListener('message', (event) => {
  const data = event.data || {};
  console.log('[SW] Message received:', data);
  
  if (data.type === 'SHOW_ALERT_NOTIFICATION') {
    const title = data.title || 'Nouvelle alerte';
    const body = data.message || '';
    const url = data.url || `/AlertsCrud/Details/${data.alertId || ''}`;
    
    event.waitUntil(
      self.registration.showNotification(title, {
        body,
        icon: '/favicon.ico',
        badge: '/favicon.ico',
        tag: 'alert-notification',
        data: { url },
        actions: [
          { action: 'view', title: 'Voir', icon: '/favicon.ico' },
          { action: 'dismiss', title: 'Ignorer', icon: '/favicon.ico' }
        ]
      })
    );
  }
});

// Gestion du cache (stratégie Cache First pour les ressources statiques)
self.addEventListener('fetch', (event) => {
  // Ignorer les requêtes non-GET
  if (event.request.method !== 'GET') {
    return;
  }
  
  // Ignorer les requêtes API
  if (event.request.url.includes('/api/')) {
    return;
  }
  
  event.respondWith(
    caches.match(event.request)
      .then((response) => {
        // Retourner la version en cache si disponible
        if (response) {
          return response;
        }
        
        // Sinon, faire la requête réseau
        return fetch(event.request);
      })
  );
});


