self.addEventListener('install', (e)=>{ self.skipWaiting(); });
self.addEventListener('activate', (e)=>{ self.clients.claim(); });

self.addEventListener('message', (event)=>{
  const data = event.data || {};
  if (data.type === 'SHOW_ALERT_NOTIFICATION'){
    const title = data.title || 'Nouvelle alerte';
    const body = data.message || '';
    const url = data.url || (`/AlertsCrud/Details/${data.alertId||''}`);
    const badge = '/favicon.ico';
    const icon = '/favicon.ico';
    event.waitUntil(self.registration.showNotification(title, {
      body,
      icon,
      badge,
      data: { url }
    }));
  }
});

self.addEventListener('notificationclick', (event)=>{
  event.notification.close();
  const url = (event.notification.data && event.notification.data.url) || '/';
  event.waitUntil((async()=>{
    const allClients = await self.clients.matchAll({ type:'window', includeUncontrolled:true });
    for (const client of allClients){
      const cl = client; if ('focus' in cl){ cl.focus(); }
      if ('navigate' in cl){ cl.navigate(url); return; }
    }
    await self.clients.openWindow(url);
  })());
});


