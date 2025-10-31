import { dbg, logError, logSuccess, showLoading, fetchJson, normalizePhone, splitPhones } from './helpers.js';
import { loadQuickTemplates, setupComposeHandlers, loadUsers, getSelectedUsersData, setupDynamicPlatforms } from './components/compose.js';
import { setupTagInputs, addTagTo, getTagValues } from './components/tags.js';
import { initWebPushSubscriptionFlow, initializeSignalR } from './realtime.js';
import { updatePagination, filterList, showFinalStatusToast, updateActiveNavigation, loadInboxKpiData, loadOutboxKpiData, updateUnreadBadge, confirmAlert, markRead, updateSidebarCounts, updateTodayKpi, updateMandatoryConfirmedKpi, updateMandatoryPendingKpi, loadInboxDetails, loadOutboxDetails, renderList, updateInboxCount, updateOutboxCount } from './components/ui.js';
import { renderInboxList } from './components/inbox.js';
import { renderOutboxList } from './components/outbox.js';
import { showDetailsModal, setupModalA11y } from './modals.js';
import { usersState, ensureUsersLoaded } from './usersState.js';

// --- Export helpers legacy/devtools sur window pour fallback ---
if(typeof window!=="undefined"){
  window.dbg = dbg;
  window.logError = logError;
  window.logSuccess = logSuccess;
  window.showLoading = showLoading;
  window.fetchJson = fetchJson;
  window.normalizePhone = normalizePhone;
  window.splitPhones = splitPhones;
  window.setupTagInputs = setupTagInputs;
  window.addTagTo = addTagTo;
  window.getTagValues = getTagValues;
}

// --- Gestion d'état et variables globales (comme legacy) ---
let inboxPage = 1, sentPage = 1, pageSize = 50, inboxTotal = 0, sentTotal = 0, inboxSearch = '';

function buildFilterQuery(){
  const startEl = document.getElementById('filterStart');
  const endEl = document.getElementById('filterEnd');
  const typeEl = document.getElementById('filterType');
  const stateEl = document.getElementById('filterState');
  const searchEl = document.getElementById('alertSearch');
  const p = new URLSearchParams();
  const sv = startEl?.value?.trim();
  const ev = endEl?.value?.trim();
  const tv = typeEl?.value?.trim();
  const stv = stateEl?.value?.trim();
  const qv = searchEl?.value?.trim();
  if (sv) p.set('startDate', sv);
  if (ev) p.set('endDate', ev);
  if (tv) p.set('typeId', tv);
  if (stv) p.set('stateId', stv);
  if (qv) p.set('q', qv);
  const s = p.toString();
  return s ? `?${s}` : '';
}

async function loadInbox() {
  dbg('loadInbox: Loading page', inboxPage);
  showLoading('inboxList');
  try {
    const qs = buildFilterQuery() + (buildFilterQuery().length ? '&' : '?') + `page=${inboxPage}&pageSize=${pageSize}`;
    const data = await fetchJson(`/Dashboard/GetInboxAlerts${qs}`);
    dbg('loadInbox: received', data);
    renderInboxList('inboxList', data.alerts || []);
    inboxTotal = data.total ?? (data.alerts?.length ?? 0);
    updatePagination('inbox', inboxPage, inboxTotal);
  } catch(e) {
    logError('loadInbox', e);
  }
}
async function loadSent() {
  dbg('loadSent: Loading page', sentPage);
  showLoading('sentList');
  try {
    const qs = buildFilterQuery() + (buildFilterQuery().length ? '&' : '?') + `page=${sentPage}&pageSize=${pageSize}`;
    const data = await fetchJson(`/Dashboard/GetOutboxAlerts${qs}`);
    dbg('loadSent: received', data);
    renderOutboxList('sentList', data.alerts || []);
    sentTotal = data.total ?? (data.alerts?.length ?? 0);
    updatePagination('sent', sentPage, sentTotal);
  } catch(e) {
    logError('loadSent', e);
  }
}

// Expose all window handlers as real versions
window.confirmAlert = confirmAlert;
window.markRead = markRead;
window.showDetailsModal = showDetailsModal;
window.addTagTo = addTagTo;
window.splitPhones = splitPhones;
window.normalizePhone = normalizePhone;
window.showFinalStatusToast = showFinalStatusToast;
window.updateInboxCount = updateInboxCount;
window.updateOutboxCount = updateOutboxCount;
window.startUndoTimer = startUndoTimer;
window.showSendingNotification = showSendingNotification;
window.cancelSend = cancelSend;
window.loadInboxKpiData = loadInboxKpiData;
window.loadOutboxKpiData = loadOutboxKpiData;
window.updateUnreadBadge = updateUnreadBadge;
window.renderList = renderList;
window.loadInbox = loadInbox;
window.loadSent = loadSent;

// Fallbacks helpers counts sur window si utilisés en HTML/devtools
// window.updateInboxCount = function(count) { const badge = document.querySelector('#inbox-badge'); if (badge) badge.textContent = count; };
// window.updateOutboxCount = function(count) { const badge = document.querySelector('#outbox-badge'); if (badge) badge.textContent = count; };

// Exposition globale pour DevTools et HTML (exactement comme legacy)
if (typeof window !== "undefined") {
  window.confirmAlert = confirmAlert;
  window.markRead = markRead;
  window.showDetailsModal = showDetailsModal;
  window.addTagTo = addTagTo;
  window.splitPhones = splitPhones;
  window.normalizePhone = normalizePhone;
  window.showFinalStatusToast = showFinalStatusToast;
  window.loadInboxKpiData = loadInboxKpiData;
  window.loadOutboxKpiData = loadOutboxKpiData;
  window.updateSidebarCounts = updateSidebarCounts;
  window.updateActiveNavigation = updateActiveNavigation;
  window.loadUsers = loadUsers;
  window.getSelectedUsersData = getSelectedUsersData;
  window.setupDynamicPlatforms = setupDynamicPlatforms;
  window.setupTagInputs = setupTagInputs;
  window.updateTodayKpi = updateTodayKpi;
  window.updateMandatoryConfirmedKpi = updateMandatoryConfirmedKpi;
  window.updateMandatoryPendingKpi = updateMandatoryPendingKpi;
  window.updateUnreadBadge = updateUnreadBadge;
  window.loadInboxDetails = loadInboxDetails;
  window.loadOutboxDetails = loadOutboxDetails;
  window.renderList = renderList;
  window.updateInboxCount = updateInboxCount;
  window.updateOutboxCount = updateOutboxCount;
  window.filterList = filterList;
}

// Initialisation complète glue

function reloadInboxAndKpis() { loadInbox(); refreshAllKpis(); }
function reloadSentAndKpis() { loadSent(); refreshAllKpis(); }

// Pagination navigations déjà existantes, on les renforce
const hookPagingControls = () => {
  const inboxPrev = document.getElementById('inboxPrevBtn');
  const inboxNext = document.getElementById('inboxNextBtn');
  if (inboxPrev) inboxPrev.onclick = () => { if (inboxPage > 1) { inboxPage--; dbg('Paging: inboxPrev clicked', {inboxPage}); reloadInboxAndKpis(); } else { dbg('Paging: inboxPrev click - blocked', {inboxPage}); }};
  if (inboxNext) inboxNext.onclick = () => { const max = Math.max(1, Math.ceil(inboxTotal / pageSize)); if (inboxPage < max) { inboxPage++; dbg('Paging: inboxNext clicked', {inboxPage}); reloadInboxAndKpis(); } else { dbg('Paging: inboxNext click - blocked', {inboxPage, max}); }};
  const sentPrev = document.getElementById('sentPrevBtn');
  const sentNext = document.getElementById('sentNextBtn');
  if (sentPrev) sentPrev.onclick = () => { if (sentPage > 1) { sentPage--; dbg('Paging: sentPrev clicked', {sentPage}); reloadSentAndKpis(); } else { dbg('Paging: sentPrev click - blocked', {sentPage}); }};
  if (sentNext) sentNext.onclick = () => { const max = Math.max(1, Math.ceil(sentTotal / pageSize)); if (sentPage < max) { sentPage++; dbg('Paging: sentNext clicked', {sentPage}); reloadSentAndKpis(); } else { dbg('Paging: sentNext click - blocked', {sentPage, max}); }};
};
window.addEventListener('DOMContentLoaded', hookPagingControls);
// Sur chaque reload/données SignalR -> refresh + pagination cohérente/rappel du hook
// Sur chaque search/filtrage, on peut forcer reloadInbox (en plus du DOM filter)
let __searchDebounce;
const triggerReload = () => {
  const hasInbox = !!document.getElementById('inboxList');
  const hasSent = !!document.getElementById('sentList');
  if (hasInbox) reloadInboxAndKpis();
  if (hasSent) reloadSentAndKpis();
};
document.getElementById('alertSearch')?.addEventListener('input', () => {
  if (__searchDebounce) clearTimeout(__searchDebounce);
  __searchDebounce = setTimeout(()=>{
    dbg('Search: debounce reload', { val: document.getElementById('alertSearch').value });
    triggerReload();
  }, 220);
});

['filterStart','filterEnd','filterType','filterState'].forEach(id => {
  const el = document.getElementById(id);
  if (!el) return;
  el.addEventListener('change', ()=> { dbg('Filter change', {id, val: el.value}); triggerReload(); });
});
// (Nb : On laisse le DOM filterList mais : reload real data ici pour cohérence back)

if (!window.__ALERT_GLUE_INIT_DONE) window.__ALERT_GLUE_INIT_DONE = false;

document.addEventListener('DOMContentLoaded', async function() {
  if (window.__ALERT_GLUE_INIT_DONE) { dbg('DOMContentLoaded: glue already initialized, skipping'); return; }
  window.__ALERT_GLUE_INIT_DONE = true;
  dbg('DOMContentLoaded: GLUE + defensive init!');
  try { setupModalA11y(); } catch(e) { logError('setupModalA11y error', e); }
  await ensureUsersLoaded();
  // KPIs INBOX/OUTBOX: load only those present on this view
  try { if (document.getElementById('receivedTodayCount')) loadInboxKpiData(); } catch (e) { logError('loadInboxKpiData error', e); }
  try { if (document.getElementById('sentTodayCount')) loadOutboxKpiData(); } catch (e) { logError('loadOutboxKpiData error', e); }
  try { updateSidebarCounts(); logSuccess('DOMContentLoaded: Sidebar counts loaded successfully'); } catch (e) { logError('DOMContentLoaded: Failed to load sidebar counts', e); }
  try { setupTagInputs(); } catch (e) { logError('setupTagInputs error', e); }
  try { loadQuickTemplates(); } catch (e) { logError('loadQuickTemplates error', e); }
  try { setupComposeHandlers(); } catch (e) { logError('setupComposeHandlers error', e); }
  try { initWebPushSubscriptionFlow(); } catch (e) { logError('initWebPushSubscriptionFlow error', e); }
  try { initializeSignalR(); } catch (e) { logError('initializeSignalR error', e); }
  try { updateActiveNavigation(); } catch (e) { logError('updateActiveNavigation error', e); }

  // Rendu Inbox/Sent
  const inboxList = document.getElementById('inboxList');
  const sentList  = document.getElementById('sentList');
  if (inboxList)  { dbg('DOMContentLoaded: Loading inbox'); loadInbox(); }
  if (sentList)   { dbg('DOMContentLoaded: Loading sent items'); loadSent(); }

  // Listeners robustes Bootstrap modal
  document.querySelectorAll('[data-bs-toggle="modal"]').forEach(btn => {
    btn.addEventListener('click', ev => {
      const targetSel = btn.getAttribute('data-bs-target');
      if (!targetSel) return;
      const el = document.querySelector(targetSel);
      if (!el) { ev.preventDefault(); ev.stopPropagation(); return; }
      if (window.bootstrap && typeof window.bootstrap.Modal?.getOrCreateInstance === 'function') {
        window.bootstrap.Modal.getOrCreateInstance(el);
      }
      dbg('Modal open click', { targetSel });
    });
  });

  // Correction: Forcer loadUsers() lors de l'ouverture de la modal compose
  const composeBtn = document.querySelector('#composeFab button');
  if (composeBtn){
    composeBtn.addEventListener('click', (e)=>{
      loadUsers();
      // ... reste du glue modal (modal show etc.)
      const modalEl = document.getElementById('newAlertModal');
      if (modalEl && window.bootstrap?.Modal){ e.preventDefault(); window.bootstrap.Modal.getOrCreateInstance(modalEl).show(); }
    });
  }

  // Silent if some KPI/DOM elements are not present on this page (expected per view)
  // Rebind tab click glue always
  const sentTab = document.querySelector('[data-bs-target="#sent"]');
  if (sentTab) {
    sentTab.addEventListener('click', ()=>{
      dbg('TAB: Sent clicked, forcing reload sent/outbox + KPIs');
      if(window.loadSent) window.loadSent();
      if(window.loadOutboxKpiData) window.loadOutboxKpiData();
      if(window.updateSidebarCounts) window.updateSidebarCounts();
    });
  }
  const inboxTab = document.querySelector('[data-bs-target="#inbox"]');
  if(inboxTab){
    inboxTab.addEventListener('click', ()=>{
      dbg('TAB: Inbox clicked, forcing reload inbox + KPIs');
      if(window.loadInbox) window.loadInbox();
      if(window.loadInboxKpiData) window.loadInboxKpiData();
      if(window.updateSidebarCounts) window.updateSidebarCounts();
    });
  }

  // 1. Charger KPIs Outbox aussi sur tab click sent
  // This is now handled by the rebindTabClickGlue above
  // const sentTab = document.querySelector('[data-bs-target="#sent"]');
  // if (sentTab) {
  //   sentTab.addEventListener('click', ()=>{
  //     if (typeof window.loadSent === 'function') window.loadSent();
  //     if (typeof window.loadOutboxKpiData === 'function') window.loadOutboxKpiData();
  //     if (typeof window.updateSidebarCounts === 'function') window.updateSidebarCounts();
  //   });
  // }
  // 2. Listeners glue confirmAlert/markRead pour KPIs auto (existe déjà dans ui.js mais rebranche ici pour robustesse)
  window.confirmAlert = async function(alertId){
    if (typeof window._confirmAlertImpl === 'function') return window._confirmAlertImpl(alertId);  // fallback impl
    try {
      // ... fallback impl direct ...
      await import('./components/ui.js').then(ui=>ui.confirmAlert(alertId));
      setTimeout(()=>{ try{loadInboxKpiData();}catch{} try{loadOutboxKpiData();}catch{} try{updateSidebarCounts();}catch{} }, 100);
    } catch (e) { dbg('confirmAlert: error', e); }
  };
  window.markRead = async function(alertId){ window.confirmAlert(alertId); };
  // 3. Listener robustes SignalR hook updateKPI
  if (window.initializeSignalR && typeof window.initializeSignalR === 'function') {
    window.initializeSignalR();
  }
  // 4. Ajoute fallback kpi reload sur tab switch (Boite de reception/expo KPIs: déjà fait par setInterval, renforce listeners...)
  // This is now handled by the rebindTabClickGlue above
  // const inboxTab = document.querySelector('[data-bs-target="#inbox"]');
  // if (inboxTab){
  //   inboxTab.addEventListener('click', ()=>{
  //     if (typeof window.loadInboxKpiData === 'function') window.loadInboxKpiData();
  //     if (typeof window.loadInbox === 'function') window.loadInbox();
  //   });
  // }

  // Refresh périodique toutes les 10s (KSPI, badge, sidebar ok, toujours visible)
  if (!window.__ALERT_KPI_INTERVAL_ID) {
    window.__ALERT_KPI_INTERVAL_ID = setInterval(()=>{
      if(document.hidden) return;
      try { if (document.getElementById('receivedTodayCount')) loadInboxKpiData(); } catch{}
      try { if (document.getElementById('sentTodayCount')) loadOutboxKpiData(); } catch{}
      try { updateSidebarCounts(); } catch{}
      try { updateUnreadBadge(); } catch{}
    }, 10000);
  }
  dbg('DOMContentLoaded: glue complete');
});

// Ajout renforcé : chaque action refresh les KPIs tout de suite après les modifications de données
let __kpiDebounce;
function refreshAllKpis() {
  if (__kpiDebounce) clearTimeout(__kpiDebounce);
  __kpiDebounce = setTimeout(()=>{
    dbg('refreshAllKpis: called');
    try { if (document.getElementById('receivedTodayCount')) loadInboxKpiData(); } catch(e){ logError('refreshAllKpis: loadInboxKpiData', e); }
    try { if (document.getElementById('sentTodayCount')) loadOutboxKpiData(); } catch(e){ logError('refreshAllKpis: loadOutboxKpiData', e); }
    try { updateSidebarCounts(); } catch(e){ logError('refreshAllKpis: updateSidebarCounts', e); }
  }, 150);
}
window.refreshAllKpis = refreshAllKpis;

// Exemples d'ajouts (après send, confirm, markRead, tab switch etc.) :
// 1. Après sendBtn/:
// ...
// await sendAlertLogic();
refreshAllKpis();
// ...

// 2. Après confirm/markRead (dans window.confirmAlert ET handler ui.js) :
// ...
refreshAllKpis();
// ...

// 3. Après tab switch sent/inbox :
// ...
refreshAllKpis();
// ...

// 4. Après pagination (suiv/préc) :
// ...
refreshAllKpis();
// ...
// Veille à ne pas créer de boucle infinie si déjà appelé par un reload cyclique.

// Le setInterval existant reste, mais cela garantit qu'aucune action utilisateur n'affiche un KPI "en retard"

// Ajoute systématiquement refreshAllKpis après chaque action sur les listes d’alertes
window.confirmAlert = async function(alertId){
  try {
    dbg('alert-dashboard: confirmAlert called', {alertId});
    await import('./components/ui.js').then(ui=>ui.confirmAlert(alertId));
    setTimeout(()=>{ refreshAllKpis(); if (typeof window.loadInbox === 'function') window.loadInbox(); }, 200);
  } catch(e){ logError('alert-dashboard: confirmAlert error', e); showFinalStatusToast('Erreur lors de la confirmation', 'danger'); }
}
window.markRead = async function(alertId){
  try {
    dbg('alert-dashboard: markRead called', {alertId});
    await import('./components/ui.js').then(ui=>ui.markRead(alertId));
    setTimeout(()=>{ refreshAllKpis(); if (typeof window.loadInbox === 'function') window.loadInbox(); }, 200);
  } catch(e){ logError('alert-dashboard: markRead error', e); showFinalStatusToast('Erreur lors du marquage lu', 'danger'); }
}
// Chaque clic sur une alerte refresh automatiquement les KPIs (pour repair bug visuel post-action)
document.addEventListener('DOMContentLoaded', function() {
  // ... déjà présent ...
  // Attach click for all alert-rows on DOM updates
  function hookAlertRowsActions() {
    document.querySelectorAll('.gmail-alert-row').forEach(row => {
      row.addEventListener('click', ()=> { setTimeout(refreshAllKpis, 300); });
    });
  }
  // Hook after every reload
  const observer = new MutationObserver(hookAlertRowsActions);
  observer.observe(document.body, {childList: true, subtree: true});
  // ...
}); // ...code restant...

// --- Setup all legacy helpers on window ---
if(typeof window!=="undefined"){
// Expose all main glue, UI, and utility for devtools/fallback as in monolith
  window.dbg = dbg;
  window.logError = logError;
  window.logSuccess = logSuccess;
  window.showLoading = showLoading;
  window.fetchJson = fetchJson;
  window.normalizePhone = normalizePhone;
  window.splitPhones = splitPhones;
  window.setupTagInputs = setupTagInputs;
  window.addTagTo = addTagTo;
  window.getTagValues = getTagValues;
  window.renderList = renderList;
  window.updateInboxCount = updateInboxCount;
  window.updateOutboxCount = updateOutboxCount;
  window.filterList = filterList;
  window.confirmAlert = confirmAlert;
  window.markRead = markRead;
  window.showDetailsModal = showDetailsModal;
  window.showFinalStatusToast = showFinalStatusToast;
  window.getSelectedUsersData = getSelectedUsersData;
  window.refreshAllKpis = refreshAllKpis;
  window.ensureUsersLoaded = ensureUsersLoaded;
}
// Ensure usersState is initialized at startup
ensureUsersLoaded();

// Legacy window helper glue (final backup):
if(typeof window!=="undefined"){
  // Re-expose all glue helpers after load for late HTML/cshtml
  window.dbg = dbg;
  window.logError = logError;
  window.logSuccess = logSuccess;
  window.showLoading = showLoading;
  window.fetchJson = fetchJson;
  window.normalizePhone = normalizePhone;
  window.splitPhones = splitPhones;
  window.setupTagInputs = setupTagInputs;
  window.addTagTo = addTagTo;
  window.getTagValues = getTagValues;
  window.renderList = renderList;
  window.updateInboxCount = updateInboxCount;
  window.updateOutboxCount = updateOutboxCount;
  window.filterList = filterList;
  window.confirmAlert = confirmAlert;
  window.markRead = markRead;
  window.showDetailsModal = showDetailsModal;
  window.showFinalStatusToast = showFinalStatusToast;
  window.getSelectedUsersData = getSelectedUsersData;
  window.refreshAllKpis = refreshAllKpis;
  window.ensureUsersLoaded = ensureUsersLoaded;
}
