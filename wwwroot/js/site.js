(async function initNotifications(){
  try{
    if ('serviceWorker' in navigator){
      try{ await navigator.serviceWorker.register('/sw.js'); }catch{}
    }
    if ("Notification" in window && Notification.permission === 'default'){
      try{ await Notification.requestPermission(); }catch{}
    }
    // Auto subscribe to push if permitted
    if ("Notification" in window && Notification.permission === 'granted' && 'serviceWorker' in navigator){
      try{
        const reg = await navigator.serviceWorker.ready;
        // Get VAPID public key
        const r = await fetch('/Push/PublicKey');
        const { publicKey } = await r.json();
        if (!publicKey) return;
        const sub = await reg.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: urlBase64ToUint8Array(publicKey) });
        const body = { Endpoint: sub.endpoint, P256dh: arrayBufferToBase64(sub.getKey('p256dh')), Auth: arrayBufferToBase64(sub.getKey('auth')) };
        await fetch('/Push/Subscribe', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify(body) });
      }catch{}
    }
  }catch{}
})();

function urlBase64ToUint8Array(base64String){
  const padding = '='.repeat((4 - base64String.length % 4) % 4);
  const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
  const rawData = window.atob(base64);
  const outputArray = new Uint8Array(rawData.length);
  for (let i = 0; i < rawData.length; ++i){ outputArray[i] = rawData.charCodeAt(i); }
  return outputArray;
}
function arrayBufferToBase64(buffer){
  if (!buffer) return '';
  const bytes = new Uint8Array(buffer);
  let binary = '';
  for (let i = 0; i < bytes.byteLength; i++){ binary += String.fromCharCode(bytes[i]); }
  return btoa(binary);
}
// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
