import { dbg, logError, logSuccess, fetchJson } from './helpers.js';
import * as ui from './components/ui.js';

export async function initWebPushSubscriptionFlow() {
  try{
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
      dbg('WebPush: Not supported in this browser');
      return;
    }
    // Ensure service worker is registered
    const reg = await navigator.serviceWorker.register('/service-worker.js');
    dbg('WebPush: SW registered', reg.scope || '');
    // Ask for permission
    const permission = await Notification.requestPermission();
    if (permission !== 'granted'){
      dbg('WebPush: Permission not granted');
      return;
    }
    // Fetch VAPID key
    const pk = await fetchJson('/Push/VapidPublicKey');
    const vapidPublicKey = (pk && pk.publicKey) ? pk.publicKey : '';
    if (!vapidPublicKey){ dbg('WebPush: Missing VAPID key'); return; }
    // Convert base64 URL key to Uint8Array
    const keyUint8 = (function urlBase64ToUint8Array(base64String) {
      const padding = '='.repeat((4 - base64String.length % 4) % 4);
      const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
      const rawData = window.atob(base64);
      const outputArray = new Uint8Array(rawData.length);
      for (let i = 0; i < rawData.length; ++i) { outputArray[i] = rawData.charCodeAt(i); }
      return outputArray;
    })(vapidPublicKey);
    // Subscribe
    const sub = await reg.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: keyUint8 });
    const endpoint = sub.endpoint;
    const p256dh = btoa(String.fromCharCode.apply(null, new Uint8Array(sub.getKey('p256dh'))));
    const auth = btoa(String.fromCharCode.apply(null, new Uint8Array(sub.getKey('auth'))));
    // Send to backend
    await fetch('/Push/Subscribe', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ endpoint: endpoint, p256dh: p256dh, auth: auth })
    });
    dbg('WebPush: Subscription posted');
  } catch(err){
    logError('WebPush: Subscription flow error', err);
  }
}

export function initializeSignalR() {
  if (window.signalR && window.signalR.HubConnectionBuilder) {
    dbg('SignalR: Initializing hub connection...');
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/notifications')
      .withAutomaticReconnect()
      .build();
    connection.on('ReceiveNotification', (type, data) => {
      dbg('SignalR: ReceiveNotification', {type, data});
      switch(type){
        case 'AlertCreated':
        case 'AlertProcessed':
        case 'AlertReceived':
        case 'NewAlertReceived':
        case 'AlertStatusUpdated':
          if(typeof window.refreshAllKpis==='function') window.refreshAllKpis();
          if(typeof window.loadInbox==='function') window.loadInbox();
          if(typeof window.loadSent==='function') window.loadSent();
          break;
        case 'UpdateKpis':
          if(typeof window.refreshAllKpis==='function') window.refreshAllKpis();
          break;
      }
    });
    connection.start()
      .then(() => { dbg('SignalR: Connected') })
      .catch(err=>{ logError('SignalR connect error', err); });
  } else {
    dbg('SignalR: signalR.HubConnectionBuilder not found.');
  }
}

export const updateInboxKpis = ui.updateInboxKpis;
export const updateOutboxKpis = ui.updateOutboxKpis;
export const updateOutboxModal = ui.updateOutboxModal;
export const addNewAlertToOutbox = ui.addNewAlertToOutbox;
export const addNewAlertToInbox = ui.addNewAlertToInbox;
if(typeof window!=="undefined"){window.updateInboxKpis=updateInboxKpis;window.updateOutboxKpis=updateOutboxKpis;window.updateOutboxModal=updateOutboxModal;window.addNewAlertToOutbox=addNewAlertToOutbox;window.addNewAlertToInbox=addNewAlertToInbox;}

export async function updateSidebarCounts() {
  try {
    const [inboxData, outboxData] = await Promise.all([
      fetchJson('/Dashboard/InboxKpiData'),
      fetchJson('/Dashboard/OutboxKpiData')
    ]);
    const unreadCount = document.getElementById('unreadCount');
    if (unreadCount && inboxData.unreadAlerts !== undefined) {
      unreadCount.textContent = inboxData.unreadAlerts;
    }
    const outboxPendingCount = document.getElementById('outboxPendingCount');
    if (outboxPendingCount && outboxData.pendingConfirmation !== undefined) {
      outboxPendingCount.textContent = outboxData.pendingConfirmation;
    }
    dbg('updateSidebarCounts: Updated sidebar counts successfully');
  } catch (error) {
    logError('updateSidebarCounts: Failed to update sidebar counts', error);
  }
}

if (typeof window !== "undefined") {
  window.initializeSignalR = initializeSignalR;
}

function postSignalRRefresh(){ if(typeof window.refreshAllKpis==='function') window.refreshAllKpis(); }
