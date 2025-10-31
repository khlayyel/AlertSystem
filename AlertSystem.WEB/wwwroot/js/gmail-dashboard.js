// Minimal bootstrap to load inbox/sent using existing endpoints in WEB controllers
let inboxPage = 1, sentPage = 1, pageSize = 50, inboxTotal = 0, sentTotal = 0;

// Enhanced frontend logging helper with comprehensive debug information
const __ALERT_DEBUG = true;
function dbg(){ 
  try { 
    if (__ALERT_DEBUG && window.console){ 
      const timestamp = new Date().toISOString();
      const args = Array.from(arguments);
      args.unshift(`[${timestamp}]`);
      console.debug.apply(console, args); 
    } 
  } catch(e) { 
    console.error('Debug logging error:', e); 
  } 
}

// Enhanced error logging
function logError(context, error, additionalData = {}) {
  dbg('ERROR:', context, {
    error: error.message || error,
    stack: error.stack,
    additionalData,
    timestamp: new Date().toISOString()
  });
  console.error(`[${context}]`, error, additionalData);
}

// Enhanced success logging
function logSuccess(context, data = {}) {
  dbg('SUCCESS:', context, {
    data,
    timestamp: new Date().toISOString()
  });
}
function showLoading(containerId) {
  const c = document.getElementById(containerId);
  if (!c) return;
  c.innerHTML = '<div class="gmail-loading"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Chargement...</span></div><span class="ms-2">Chargement des alertes...</span></div>';
}

document.addEventListener('DOMContentLoaded', function() {
  dbg('DOMContentLoaded: Starting initialization');
  
  // Load sidebar counts on every page
  try {
    dbg('DOMContentLoaded: Loading sidebar counts');
    loadSidebarCounts();
    logSuccess('DOMContentLoaded: Sidebar counts loaded successfully');
  } catch (e) {
    logError('DOMContentLoaded: Failed to load sidebar counts', e);
  }
  
  // Update active navigation state
  updateActiveNavigation();
  
  // Load users for compose modal
  try {
    dbg('DOMContentLoaded: Loading users');
    loadUsers();
    logSuccess('DOMContentLoaded: Users loaded successfully');
  } catch (e) {
    logError('DOMContentLoaded: Failed to load users', e);
  }
  
  // Check if we're on a page that needs inbox/sent loading
  const inboxList = document.getElementById('inboxList');
  const sentList = document.getElementById('sentList');
  
  // Check for Inbox KPIs
  const inboxKpiCards = document.querySelectorAll('#receivedTodayCount, #unreadAlertsCount, #pendingConfirmationCount');
  dbg('DOMContentLoaded: Found Inbox KPI cards', { count: inboxKpiCards.length });
  
  // Check for Outbox KPIs
  const outboxKpiCards = document.querySelectorAll('#sentTodayCount, #confirmedAlertsCount');
  dbg('DOMContentLoaded: Found Outbox KPI cards', { count: outboxKpiCards.length });
  
  // Load Inbox KPIs if we're on the inbox page
  if (inboxKpiCards.length > 0) {
    try {
      dbg('DOMContentLoaded: Loading Inbox KPI data');
      loadInboxKpiData();
      logSuccess('DOMContentLoaded: Inbox KPI data loaded successfully');
    } catch (e) {
      logError('DOMContentLoaded: Failed to load Inbox KPI data', e);
    }
  }
  
  // Load Outbox KPIs if we're on the sent page
  if (outboxKpiCards.length > 0) {
    try {
      dbg('DOMContentLoaded: Loading Outbox KPI data');
      loadOutboxKpiData();
      logSuccess('DOMContentLoaded: Outbox KPI data loaded successfully');
    } catch (e) {
      logError('DOMContentLoaded: Failed to load Outbox KPI data', e);
    }
  }
  
  if (inboxList) {
    try { 
      dbg('DOMContentLoaded: Loading inbox');
      loadInbox(); 
      logSuccess('DOMContentLoaded: Inbox loaded successfully');
    } catch (e) {
      logError('DOMContentLoaded: Failed to load inbox', e);
    }
  } else {
    dbg('DOMContentLoaded: Inbox list not found, skipping inbox load');
  }
  
  if (sentList) {
    try { 
      dbg('DOMContentLoaded: Loading sent items');
      loadSent(); 
      logSuccess('DOMContentLoaded: Sent items loaded successfully');
    } catch (e) {
      logError('DOMContentLoaded: Failed to load sent items', e);
    }
  } else {
    dbg('DOMContentLoaded: Sent list not found, skipping sent load');
  }
  
  const sentTab = document.querySelector('[data-bs-target="#sent"]');
  if (sentTab) {
    dbg('DOMContentLoaded: Setting up sent tab click handler');
    sentTab.addEventListener('click', () => { 
      try { 
        dbg('SentTab: Clicked, loading sent items');
        loadSent(); 
        logSuccess('SentTab: Sent items loaded successfully');
      } catch (e) {
        logError('SentTab: Failed to load sent items', e);
      }
    });
  } else {
    dbg('DOMContentLoaded: Sent tab not found');
  }

  // Robust modal bootstrap: only initialize if the element exists
  document.querySelectorAll('[data-bs-toggle="modal"]').forEach(btn => {
    btn.addEventListener('click', ev => {
      const targetSel = btn.getAttribute('data-bs-target');
      if (!targetSel) return; // no target, let default behavior fail silently
      const el = document.querySelector(targetSel);
      if (!el) { ev.preventDefault(); ev.stopPropagation(); return; }
      // Ensure Bootstrap instance is created once
      if (window.bootstrap && typeof window.bootstrap.Modal?.getOrCreateInstance === 'function') {
        window.bootstrap.Modal.getOrCreateInstance(el);
      }
      dbg('Modal open click', { targetSel });
    });
  });
  // Load KPIs and unread badge
  try { updateTodayKpi(); } catch {}
  try { updateMandatoryConfirmedKpi(); } catch {}
  try { initWebPushSubscriptionFlow(); } catch {}
  try { updateUnreadBadge(); } catch {}
  try { updateMandatoryPendingKpi(); } catch {}

  // Charger la liste des alertes rapides depuis l'API
  try { loadQuickTemplates(); } catch {}
// Enregistrer comme alerte rapide (réutilise CreateFromTemplate pour persister)
const saveQuickBtn = document.getElementById('saveQuickBtn');
if (saveQuickBtn){
  saveQuickBtn.addEventListener('click', async ()=>{
    const title = document.getElementById('composeTitle')?.value || '';
    const message = document.getElementById('composeMessage')?.value || '';
    const type = document.getElementById('composeType')?.value || 'Information';
    try {
      const r = await fetch('/AlertsCrud/SaveQuick', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ title, message, type }) });
      if (!r.ok) throw new Error('HTTP '+r.status);
      await loadQuickTemplates();
      alert('Alerte rapide enregistrée');
    } catch (e){
      console.error('SaveQuick failed', e);
      alert("Échec d'enregistrement de l'alerte rapide");
    }
  });
}

  // periodic KPI refresh when tab is visible
  setInterval(()=>{
    if (document.hidden) return;
    updateTodayKpi().catch(()=>{});
    updateMandatoryConfirmedKpi().catch(()=>{});
    updateMandatoryPendingKpi().catch(()=>{});
    updateUnreadBadge().catch(()=>{});
  }, 10000);

  // Force open compose modal to avoid attribute issues
  const composeBtn = document.querySelector('#composeFab button');
  if (composeBtn){
    composeBtn.addEventListener('click', (e)=>{
      const modalEl = document.getElementById('newAlertModal');
      if (modalEl && window.bootstrap?.Modal){
        e.preventDefault();
        window.bootstrap.Modal.getOrCreateInstance(modalEl).show();
        // initialize defaults to non-obligatory key
        const t = document.getElementById('composeType'); if (t) t.value = 'acquittementNonNecessaire';
        setupDynamicPlatforms();
        setupTagInputs();
      }
    });
  }

  // Pagination controls
  const inboxPrev = document.getElementById('inboxPrevBtn');
  const inboxNext = document.getElementById('inboxNextBtn');
  if (inboxPrev) inboxPrev.addEventListener('click', ()=>{ if (inboxPage>1){ inboxPage--; loadInbox(); } });
  if (inboxNext) inboxNext.addEventListener('click', ()=>{ const maxPage = Math.max(1, Math.ceil(inboxTotal/pageSize)); if (inboxPage<maxPage){ inboxPage++; loadInbox(); } });

  const sentPrev = document.getElementById('sentPrevBtn');
  const sentNext = document.getElementById('sentNextBtn');
  if (sentPrev) sentPrev.addEventListener('click', ()=>{ if (sentPage>1){ sentPage--; loadSent(); } });
  if (sentNext) sentNext.addEventListener('click', ()=>{ const maxPage = Math.max(1, Math.ceil(sentTotal/pageSize)); if (sentPage<maxPage){ sentPage++; loadSent(); } });
  // Live search
  const search = document.getElementById('alertSearch');
  if (search){
    search.addEventListener('input', ()=>{
      const q = search.value.trim().toLowerCase();
      filterList('inboxList', q);
      filterList('sentList', q);
      dbg('search:input', { q });
    });
  }
});
async function loadQuickTemplates(){
  const sel = document.getElementById('quickTemplate');
  if (!sel) return;
  try {
    const items = await fetchJson('/AlertsCrud/QuickList');
    sel.innerHTML = '<option value="">-- Sélectionner --</option>' +
      (items||[]).map(x=>`<option value="${x.id}" data-title="${encodeURIComponent(x.title||'')}" data-message="${encodeURIComponent(x.message||'')}" data-type="${encodeURIComponent(x.type||'Information')}">${x.title||'Sans titre'}</option>`).join('');

    sel.addEventListener('change', ()=>{
      const opt = sel.options[sel.selectedIndex];
      if (!opt || !opt.value) return;
      const title = decodeURIComponent(opt.getAttribute('data-title')||'');
      const message = decodeURIComponent(opt.getAttribute('data-message')||'');
      const type = decodeURIComponent(opt.getAttribute('data-type')||'Information');
      const t = document.getElementById('composeTitle'); if (t) t.value = title;
      const m = document.getElementById('composeMessage'); if (m) m.value = message;
      const ty = document.getElementById('composeType'); if (ty) ty.value = type;
    });
  } catch (e) {
    console.warn('Quick templates load failed', e);
  }
}


async function fetchJson(url){
  dbg('fetchJson: Starting request', { url, timestamp: new Date().toISOString() });
  
  try {
    const r = await fetch(url, { cache:'no-store' });
    dbg('fetchJson: Received response', { 
      url, 
      status: r.status, 
      statusText: r.statusText,
      ok: r.ok,
      headers: Object.fromEntries(r.headers.entries())
    });
    
    if(!r.ok) {
      const errorText = await r.text();
      logError('fetchJson: HTTP error', new Error(`HTTP ${r.status}: ${errorText}`), {
        url,
        status: r.status,
        statusText: r.statusText,
        responseText: errorText
      });
      throw new Error(`HTTP ${r.status}: ${errorText}`);
    }
    
    const j = await r.json();
    dbg('fetchJson: Parsed JSON response', { url, dataType: typeof j, dataKeys: Object.keys(j || {}) });
    logSuccess('fetchJson: Request completed successfully', { url, dataType: typeof j });
    return j;
  } catch (error) {
    logError('fetchJson: Request failed', error, { url });
    throw error;
  }
}

function renderInboxList(containerId, items) {
  const c = document.getElementById(containerId); if (!c) return;

  // INBOX HEADER ROW
  c.innerHTML = `
    <div class="gmail-inbox-header-row d-flex align-items-center fw-bold bg-light border-bottom" style="min-height:44px;">
      <div style="min-width:210px;" class="ps-3 flex-shrink-0">Titre Alerte</div>
      <div class="flex-grow-1 ps-2 pe-2">Description</div>
      <div style="width:130px;" class="text-center flex-shrink-0">Statut</div>
      <div style="min-width:160px;" class="text-end pe-3 flex-shrink-0">Date</div>
    </div>
  `;
  c.innerHTML += (items || []).map(a => {
    const preview = (a.message || '').trim();
    const dRaw = a.createdAt || a.dateCreation || a.date || a.DateCreation;
    const idVal = a.id ?? a.Id ?? a.alertId ?? a.AlertId ?? a.historiqueId ?? a.HistoriqueId;
    const alertTypeId = a.alertTypeId ?? a.AlertTypeId;
    let etatVal = (a.etatAlerteId ?? a.EtatAlerteId ?? a.readStateId);
    if (etatVal == null) etatVal = 1;
    let dateText = '';
    try {
      const d = new Date(dRaw);
      dateText = isNaN(d.getTime()) ? '' : d.toLocaleString('fr-FR', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' });
    } catch { dateText = ''; }
    const requiresConfirmation = (alertTypeId === 2) || (a.alertType === 'acquittementNecessaire' || a.alertType === 'acquittementNécessaire');
    let readBadge = '';
    let readClass = '';
    switch (etatVal) {
      case 1:
        readBadge = requiresConfirmation ? 'Non confirmé' : 'Non Lu';
        readClass = 'bg-danger';
        break;
      case 2:
        readBadge = requiresConfirmation ? 'Confirmé' : 'Lu';
        readClass = 'bg-success';
        break;
      default:
        readBadge = '';
        readClass = '';
        break;
    }
    return `
      <div class="gmail-alert-row d-flex align-items-center" style="min-height:56px; border-bottom:1px solid #f1f1f1;" data-id="${idVal}" data-historique-id="${idVal}">
        <div class="row-left title-col flex-shrink-0 ps-3" style="min-width:210px;">
          <div class="fw-semibold">${(a.title || 'Sans titre')}</div>
          <div class="text-muted small">De: ${a.senderName || a.sender || 'Système'}</div>
        </div>
        <div class="row-main desc-col flex-grow-1 ps-2 pe-2">${preview}</div>
        <div class="row-status flex-shrink-0 text-center align-self-stretch d-flex align-items-center justify-content-center" style="width:130px;">
          ${readBadge ? `<span class="badge ${readClass} px-3 py-2 fs-6">${readBadge}</span>` : ''}
        </div>
        <div class="row-right date-col flex-shrink-0 text-end pe-3" style="min-width:160px;">
          <span class="text-muted small">${dateText}</span>
        </div>
      </div>
    `;
  }).join('');
  
  // Keep all click handlers and selection logic the same
  c.querySelectorAll('.gmail-alert-row').forEach(row => {
    row.addEventListener('click', async () => {
      const id = row.getAttribute('data-id');
      // selection, loading details etc (unchanged)
      dbg('renderInboxList: Alert row clicked', { id, containerId });
      c.querySelectorAll('.gmail-alert-row').forEach(r => r.classList.remove('selected'));
      row.classList.add('selected');
      try {
        showDetailsModal({ title: 'Chargement…', message: 'Veuillez patienter…' });
        const details = await fetchJson(`/Alerts/Details?id=${id}`);
        showDetailsModal(details);
      } catch (err) {
        showDetailsModal({ title: 'Erreur', message: "Impossible de charger les détails de l'alerte." });
        console.error('Details load error', err);
      }
    });
  });
}

function renderOutboxList(containerId, items){
  const c = document.getElementById(containerId); if(!c) return;
  // Client-side de-duplication fallback in case backend returns duplicate groups
  const uniqueMap = new Map();
  (items||[]).forEach(a=>{
    const dRaw = a.createdAt || a.dateCreation || a.date || a.DateCreation || '';
    const dKey = typeof dRaw === 'string' ? dRaw : (new Date(dRaw).toISOString().slice(0,16));
    const key = `${a.title||a.Title||''}|${a.message||a.Message||''}|${dKey}`;
    if (!uniqueMap.has(key)) uniqueMap.set(key, a);
  });
  const list = Array.from(uniqueMap.values());
  c.innerHTML = list.map(a=> {
    const preview = (a.message||'').trim();
    const dRaw = a.createdAt || a.dateCreation || a.date || a.DateCreation;
    let dateText = '';
    try {
      const d = new Date(dRaw);
      dateText = isNaN(d.getTime()) ? '' : d.toLocaleString('fr-FR', { year:'numeric', month:'2-digit', day:'2-digit', hour:'2-digit', minute:'2-digit' });
    } catch { dateText = ''; }
    
    // Status badge based on status string from service
    let statusBadge = '';
    let statusClass = '';
    const status = a.status || a.statutId || a.statusId;
    
    if (typeof status === 'string') {
      switch(status.toLowerCase()) {
        case 'en cours': statusBadge = 'En cours'; statusClass = 'bg-warning'; break;
        case 'envoyé': statusBadge = 'Envoyé'; statusClass = 'bg-success'; break;
        case 'annulé': statusBadge = 'Annulé'; statusClass = 'bg-secondary'; break;
        case 'échoué': statusBadge = 'Échoué'; statusClass = 'bg-danger'; break;
        default: statusBadge = status || 'Inconnu'; statusClass = 'bg-light text-dark'; break;
      }
    } else {
      switch(status) {
        case 1: statusBadge = 'En cours'; statusClass = 'bg-warning'; break;
        case 2: statusBadge = 'Envoyé'; statusClass = 'bg-success'; break;
        case 3: statusBadge = 'Annulé'; statusClass = 'bg-secondary'; break;
        case 4: statusBadge = 'Échoué'; statusClass = 'bg-danger'; break;
        default: statusBadge = 'Inconnu'; statusClass = 'bg-light text-dark'; break;
      }
    }
    
    return `
    <div class="gmail-alert-row" data-id="${a.id}">
      <div class="row-left title-col">
        <div class="d-flex align-items-center">
          <div class="flex-grow-1">
            ${(a.title||'Sans titre')}
          </div>
          <div class="ms-2">
            <span class="badge ${statusClass}">${statusBadge}</span>
          </div>
        </div>
      </div>
      <div class="row-main desc-col">${preview}</div>
      <div class="row-right date-col">${dateText}</div>
    </div>`;
  }).join('');
  
  // Add click handlers for outbox rows
  c.querySelectorAll('.gmail-alert-row').forEach(row=>{
    row.addEventListener('click', async ()=>{
      const id = row.getAttribute('data-id');
      
      // Add visual selection feedback
      dbg('renderOutboxList: Alert row clicked', { id, containerId });
      
      // Remove selection from all rows in this container
      c.querySelectorAll('.gmail-alert-row').forEach(r => r.classList.remove('selected'));
      
      // Add selection to clicked row
      row.classList.add('selected');
      
      try {
        // show loading state in details modal first
        showDetailsModal({ title: 'Chargement…', message: 'Veuillez patienter…' });
        
        // For sent alerts, show recipients
        const recipientsData = await fetchJson(`/Dashboard/AlertRecipients/${id}`);
        const alertDetails = await fetchJson(`/Alerts/Details?id=${id}`);
        
        // Combine alert details with recipients
        const combinedDetails = {
          ...alertDetails,
          recipients: recipientsData.recipients || []
        };
        
        showDetailsModal(combinedDetails);
      } catch (err){
        showDetailsModal({ title: 'Erreur', message: "Impossible de charger les détails de l'alerte." });
        console.error('Details load error', err);
      }
    });
  });
}

// Legacy function for backward compatibility
function renderList(containerId, items){
  // Determine which rendering function to use based on container ID
  if (containerId === 'inboxList') {
    renderInboxList(containerId, items);
  } else if (containerId === 'sentList') {
    renderOutboxList(containerId, items);
  } else {
    // Default to outbox rendering for unknown containers
    renderOutboxList(containerId, items);
  }
}

async function loadInbox(){
  dbg('loadInbox: Starting', { page: inboxPage, pageSize });
  showLoading('inboxList');
  
  try {
    const qs = buildFilterQuery();
    const data = await fetchJson(`/Dashboard/GetInboxAlerts${qs}`);
    dbg('loadInbox: Received data', { 
      itemsCount: data.items?.length || 0, 
      total: data.total,
      page: inboxPage 
    });
    
    renderInboxList('inboxList', data.alerts);
    inboxTotal = data.alerts?.length ?? 0;
    updatePagination('inbox', inboxPage, inboxTotal);
    
    logSuccess('loadInbox: Completed successfully', { 
      page: inboxPage, 
      total: inboxTotal,
      itemsCount: data.alerts?.length || 0
    });
  } catch (error) {
    logError('loadInbox: Failed to load inbox', error, { page: inboxPage, pageSize });
    throw error;
  }
}
async function loadSent(){
  dbg('loadSent: Starting', { page: sentPage, pageSize });
  showLoading('sentList');
  
  try {
    const qs = buildFilterQuery();
    const data = await fetchJson(`/Dashboard/GetOutboxAlerts${qs}`);
    dbg('loadSent: Received data', { 
      itemsCount: data.items?.length || 0, 
      total: data.total,
      page: sentPage 
    });
    
    renderOutboxList('sentList', data.alerts);
    sentTotal = data.alerts?.length ?? 0;
    updatePagination('sent', sentPage, sentTotal);
    
    logSuccess('loadSent: Completed successfully', { 
      page: sentPage, 
      total: sentTotal,
      itemsCount: data.alerts?.length || 0
    });
  } catch (error) {
    logError('loadSent: Failed to load sent items', error, { page: sentPage, pageSize });
    throw error;
  }
}

// Build query string from optional filter controls if present
function buildFilterQuery(){
  const startEl = document.getElementById('filterStart');
  const endEl = document.getElementById('filterEnd');
  const typeEl = document.getElementById('filterType');
  const stateEl = document.getElementById('filterState');
  const p = new URLSearchParams();
  const sv = startEl?.value?.trim();
  const ev = endEl?.value?.trim();
  const tv = typeEl?.value?.trim();
  const stv = stateEl?.value?.trim();
  if (sv) p.set('startDate', sv);
  if (ev) p.set('endDate', ev);
  if (tv) p.set('typeId', tv);
  if (stv) p.set('stateId', stv);
  const s = p.toString();
  return s ? `?${s}` : '';
}

async function loadInboxKpiData(){
  dbg('loadInboxKpiData: Starting');
  
  try {
    const data = await fetchJson('/Dashboard/InboxKpiData');
    dbg('loadInboxKpiData: Received data', data);
    
    // Update Inbox KPI cards
    const receivedTodayCount = document.getElementById('receivedTodayCount');
    const unreadAlertsCount = document.getElementById('unreadAlertsCount');
    const pendingConfirmationCount = document.getElementById('pendingConfirmationCount');
    const unreadCount = document.getElementById('unreadCount');
    
    if (receivedTodayCount) {
      receivedTodayCount.textContent = data.receivedToday || 0;
      dbg('loadInboxKpiData: Updated received today count', data.receivedToday);
    }
    
    if (unreadAlertsCount) {
      unreadAlertsCount.textContent = data.unreadAlerts || 0;
      dbg('loadInboxKpiData: Updated unread alerts count', data.unreadAlerts);
    }
    
    if (pendingConfirmationCount) {
      pendingConfirmationCount.textContent = data.pendingConfirmation || 0;
      dbg('loadInboxKpiData: Updated pending confirmation count', data.pendingConfirmation);
    }
    
    if (unreadCount) {
      unreadCount.textContent = data.unreadAlerts || 0;
      dbg('loadInboxKpiData: Updated sidebar unread count', data.unreadAlerts);
    }
    
    logSuccess('loadInboxKpiData: Completed successfully', data);
  } catch (error) {
    logError('loadInboxKpiData: Failed to load Inbox KPI data', error);
    throw error;
  }
}

async function loadOutboxKpiData(){
  dbg('loadOutboxKpiData: Starting');
  
  try {
    const data = await fetchJson('/Dashboard/OutboxKpiData');
    dbg('loadOutboxKpiData: Received data', data);
    
    // Update Outbox KPI cards
    const sentTodayCount = document.getElementById('sentTodayCount');
    const confirmedAlertsCount = document.getElementById('confirmedAlertsCount');
    const pendingConfirmationCount = document.getElementById('pendingConfirmationCount');
    
    if (sentTodayCount) {
      sentTodayCount.textContent = data.sentToday || 0;
      dbg('loadOutboxKpiData: Updated sent today count', data.sentToday);
    }
    
    if (confirmedAlertsCount) {
      confirmedAlertsCount.textContent = data.confirmedAlerts || 0;
      dbg('loadOutboxKpiData: Updated confirmed alerts count', data.confirmedAlerts);
    }
    
    if (pendingConfirmationCount) {
      pendingConfirmationCount.textContent = data.pendingConfirmation || 0;
      dbg('loadOutboxKpiData: Updated pending confirmation count', data.pendingConfirmation);
    }
    
    logSuccess('loadOutboxKpiData: Completed successfully', data);
  } catch (error) {
    logError('loadOutboxKpiData: Failed to load Outbox KPI data', error);
    throw error;
  }
}

async function loadSidebarCounts(){
  dbg('loadSidebarCounts: Starting');
  
  try {
    const data = await fetchJson('/Dashboard/SidebarCounts');
    dbg('loadSidebarCounts: Received data', data);
    
    // Update sidebar counts
    const unreadCount = document.getElementById('unreadCount');
    const outboxPendingCount = document.getElementById('outboxPendingCount');
    
    if (unreadCount) {
      unreadCount.textContent = data.inboxUnreadCount || 0;
      dbg('loadSidebarCounts: Updated inbox unread count', data.inboxUnreadCount);
    }
    
    if (outboxPendingCount) {
      outboxPendingCount.textContent = data.outboxPendingCount || 0;
      dbg('loadSidebarCounts: Updated outbox pending count', data.outboxPendingCount);
    }
    
    logSuccess('loadSidebarCounts: Completed successfully', data);
  } catch (error) {
    logError('loadSidebarCounts: Failed to load sidebar counts', error);
    throw error;
  }
}

function updateActiveNavigation(){
  dbg('updateActiveNavigation: Starting');
  
  try {
    // Remove active class from all nav links
    const navLinks = document.querySelectorAll('.nav-link');
    navLinks.forEach(link => {
      link.classList.remove('active');
      dbg('updateActiveNavigation: Removed active class from', link.href);
    });
    
    // Get current path
    const currentPath = window.location.pathname;
    dbg('updateActiveNavigation: Current path', currentPath);
    
    // Add active class to matching nav link
    let activeLink = null;
    if (currentPath.includes('/Dashboard/Inbox') || currentPath === '/Dashboard' || currentPath === '/') {
      activeLink = document.querySelector('a[href="/Dashboard/Inbox"]');
    } else if (currentPath.includes('/Dashboard/Sent')) {
      activeLink = document.querySelector('a[href="/Dashboard/Sent"]');
    } else if (currentPath.includes('/AlertsCrud')) {
      activeLink = document.querySelector('a[href="/AlertsCrud"]');
    }
    
    if (activeLink) {
      activeLink.classList.add('active');
      dbg('updateActiveNavigation: Added active class to', activeLink.href);
      logSuccess('updateActiveNavigation: Navigation state updated successfully');
    } else {
      dbg('updateActiveNavigation: No matching nav link found for path', currentPath);
    }
  } catch (error) {
    logError('updateActiveNavigation: Failed to update navigation state', error);
  }
}

function updatePagination(kind, page, total){
  const start = (page-1)*pageSize + 1;
  const end = Math.min(total, page*pageSize);
  const container = document.getElementById(`${kind}Pagination`);
  if (!container) return;
  container.style.display = total>0 ? '' : 'none';
  const rs = document.getElementById(`${kind}RangeStart`);
  const re = document.getElementById(`${kind}RangeEnd`);
  const tt = document.getElementById(`${kind}Total`);
  if (rs) rs.textContent = total===0 ? 0 : start;
  if (re) re.textContent = end;
  if (tt) tt.textContent = total;
  const prev = document.getElementById(`${kind}PrevBtn`);
  const next = document.getElementById(`${kind}NextBtn`);
  const maxPage = Math.max(1, Math.ceil(total/pageSize));
  if (prev) prev.disabled = page<=1;
  if (next) next.disabled = page>=maxPage;
}

function filterList(containerId, query){
  const container = document.getElementById(containerId); if (!container) return;
  const rows = container.querySelectorAll('.gmail-alert-row');
  rows.forEach(row=>{
    if (!query){ row.style.display = ''; return; }
    const text = row.innerText.toLowerCase();
    row.style.display = text.includes(query) ? '' : 'none';
  });
  dbg('filterList', { containerId, query, rows: rows.length });
}

async function updateUnreadBadge(){
  try {
    const count = await fetchJson('/Alerts/UnreadCount');
    const el = document.getElementById('unreadCount');
    if (el){ el.textContent = count; }
  } catch {}
}

// Simple details modal using the compose modal shell for now
async function loadOutboxDetails(alertId) {
  try {
    const data = await fetchJson(`/Dashboard/AlertRecipients/${alertId}`);
    if (data.success && data.recipients) {
      showDetailsModal({
        title: data.title || 'Détails de l\'alerte',
        message: data.message || '',
        type: data.type || '',
        status: data.status || '',
        createdAt: data.createdAt || '',
        recipients: data.recipients
      });
    }
  } catch (error) {
    logError('Failed to load outbox details', error);
  }
}

async function confirmAlert(alertId) {
  try {
    const response = await fetch(`/Dashboard/ConfirmAlert/${alertId}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
      }
    });
    
    const result = await response.json();
    
    if (result.success) {
      logSuccess('Alert confirmed successfully');
      // Refresh the inbox to update the status
      if (currentView === 'inbox') {
        loadInbox();
        loadInboxKpiData();
        loadSidebarCounts(); // Update sidebar counts
      }
    } else {
      logError('Failed to confirm alert', result.message);
    }
  } catch (error) {
    logError('Error confirming alert', error);
  }
}

// Mark-as-read for information alerts shares the same backend endpoint
async function markRead(alertId) {
  return confirmAlert(alertId);
}

async function loadInboxDetails(alertId) {
  try {
    const data = await fetchJson(`/Dashboard/GetInboxAlerts`);
    if (data.success && data.alerts) {
      const alert = data.alerts.find(a => a.id === alertId);
      if (alert) {
        showDetailsModal({
          title: alert.title || 'Détails de l\'alerte',
          message: alert.message || '',
          type: alert.alertType || '',
          status: alert.status || '',
          createdAt: alert.date || '',
          recipients: []
        });
      }
    }
  } catch (error) {
    logError('Failed to load inbox details', error);
  }
}

function showDetailsModal(details){
  const modalEl = document.getElementById('alertDetailsModal');
  if (!modalEl) return;

  const title = details.title || 'Titre non disponible';
  const msg = details.message || 'Message non disponible';
  let createdAt = '';

  // Better date handling
  if (details.createdAt) {
    try {
      const date = new Date(details.createdAt);
      if (!isNaN(date.getTime())) {
        createdAt = date.toLocaleString('fr-FR', {
          year: 'numeric',
          month: '2-digit',
          day: '2-digit',
          hour: '2-digit',
          minute: '2-digit'
        });
      } else {
        createdAt = 'Date invalide';
      }
    } catch (e) {
      createdAt = 'Date invalide';
    }
  } else {
    createdAt = 'Date non disponible';
  }

  // Populate modal fields
  const titleEl = modalEl.querySelector('#detailTitle'); if (titleEl) titleEl.textContent = title;
  const msgEl = modalEl.querySelector('#detailMessage'); if (msgEl) msgEl.textContent = msg;
  
  // Type badge
  const typeEl = modalEl.querySelector('#detailType');
  if (typeEl) {
    const tRaw = (details.type||'').toString();
    const tId = details.alertTypeId;
    const isOblig = (tId === 2) || /acquittementn[ée]cessaire/i.test(tRaw) || /obligatoire/i.test(tRaw);
    typeEl.textContent = isOblig ? 'Obligatoire' : 'Information';
    typeEl.className = 'badge fs-6 ' + (isOblig ? 'bg-danger' : 'bg-info');
  }
  // Status badge
  const statusEl = modalEl.querySelector('#detailStatus'); if (statusEl) {
    const s = (details.status || '').toString().toLowerCase();
    statusEl.textContent = details.status || 'Statut non spécifié';
    statusEl.className = 'badge fs-6 ' + (s.includes('envoy') ? 'bg-success' : s.includes('échou') || s.includes('echec') ? 'bg-danger' : s.includes('cours') ? 'bg-warning' : 'bg-secondary');
  }
  // Date
  const createdEl = modalEl.querySelector('#detailCreatedAt'); if (createdEl) createdEl.textContent = createdAt;

  // --- ACTION BUTTON LOGIC (new: shows only if unread) ---
  const actionBtnContainer = modalEl.querySelector('#detailsActionBtnContainer');
  if (actionBtnContainer) {
    // Determine read state: 1 = unread
    const stateId = details.stateId ?? details.etatAlerteId ?? details.readStateId;
    const alertTypeId = details.alertTypeId ?? details.typeId;
    const theId = details.id ?? details.Id ?? details.alertId ?? details.AlertId;

    if (stateId === 1) {
      if (alertTypeId === 2 || details.type === 'acquittementNecessaire' || details.type === 'acquittementNécessaire') {
        actionBtnContainer.innerHTML = `
          <button class="btn btn-success btn-lg" onclick="confirmAlert(${theId})">
            <i class="bi bi-check2-circle me-1"></i>Confirmer
          </button>`;
      } else {
        actionBtnContainer.innerHTML = `
          <button class="btn btn-secondary btn-lg" onclick="markRead(${theId})">
            <i class="bi bi-eye me-1"></i>Marquer comme lu
          </button>`;
      }
    } else {
      actionBtnContainer.innerHTML = `<span class="text-success fw-semibold">Déjà confirmé ou lu</span>`;
    }
  }

  // Show recipients (for sent alerts - optional, you can remove if not needed)
  const recipientsContainer = modalEl.querySelector('#recipientsContainer');
  if (recipientsContainer && details.recipients && details.recipients.length > 0) {
    recipientsContainer.style.display = 'block';
    const recipientsList = modalEl.querySelector('#recipientsList');
    if (recipientsList) {
      const requiresConfirmation = (details.alertTypeId === 2 || details.type === 'acquittementNecessaire' || details.type === 'acquittementNécessaire');
      recipientsList.innerHTML = details.recipients.map(recipient => {
        // Determine the best identifier to display
        let identifier = '';
        if (recipient.recipientName) {
          identifier = recipient.recipientName;
        } else if (recipient.recipientEmail) {
          identifier = recipient.recipientEmail;
        } else if (recipient.recipientPhone) {
          identifier = recipient.recipientPhone;
        } else if (recipient.recipientDesktop) {
          identifier = recipient.recipientDesktop;
        } else {
          identifier = `ID: ${recipient.recipientId}`;
        }
        const statusClass = recipient.isRead ? 'text-success' : 'text-danger';
        const statusText = recipient.isRead ? (requiresConfirmation ? 'Confirmé' : 'Lu') : 'Non Lu';
        const readDate = recipient.readDate ? new Date(recipient.readDate).toLocaleString('fr-FR') : '';
        return `
          <div class="d-flex justify-content-between align-items-center py-2 border-bottom">
            <div>
              <div class="fw-semibold">${identifier}</div>
              <div class="small text-muted">
                ${readDate ? `Lu le: ${readDate}` : ''}
              </div>
            </div>
            <span class="badge ${statusClass}">${statusText}</span>
          </div>
        `;
      }).join('');
    }
  } else if (recipientsContainer) {
    recipientsContainer.style.display = 'none';
  }

  if (window.bootstrap && window.bootstrap.Modal) {
    window.bootstrap.Modal.getOrCreateInstance(modalEl).show();
  }
}

// Web Push subscription flow
async function initWebPushSubscriptionFlow(){
  try{
    if (!('serviceWorker' in navigator) || !('PushManager' in window)){
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

// Load users for compose modal
let usersList = [];
let usersDefList = [];
let usersGrhList = [];
let usersActiveTab = 'def';

async function loadUsers(){
  dbg('loadUsers: Starting');
  
  try {
    const data = await fetchJson('/Dashboard/GetUsers');
    usersDefList = Array.isArray(data.defUsers) ? data.defUsers : [];
    usersGrhList = Array.isArray(data.grhUsers) ? data.grhUsers : [];
    usersList = usersDefList; // default
    dbg('loadUsers: Received users', { def: usersDefList.length, grh: usersGrhList.length });
    
    // Scrollable table (list déroulante) below the search bar
    const tableBody = document.getElementById('usersTableBody');
    const searchBox = document.getElementById('userSearch');
    
    if (!tableBody || !searchBox) {
      dbg('loadUsers: tableBody or searchBox not found');
      return;
    }

    // Setup scrollable container
    const container = tableBody.closest('.table-responsive');
    if (container) { container.style.maxHeight = '320px'; container.style.overflowY = 'auto'; }

    // Define renderRows function BEFORE tabs setup so tabs can call it
    const renderRows = ()=>{
      const q = (searchBox.value||'').toLowerCase();
      const emailOn = document.getElementById('platformEmail')?.checked || false;
      const waOn = document.getElementById('platformWhatsApp')?.checked || false;
      
      // Show/hide columns based on platform selection
      const colEmail = document.getElementById('colEmail');
      const colWa = document.getElementById('colWa');
      if (colEmail) colEmail.style.display = emailOn ? '' : 'none';
      if (colWa) colWa.style.display = waOn ? '' : 'none';

      // Get the active list based on selected tab
      const source = usersActiveTab === 'def' ? usersDefList : usersGrhList;
      const filtered = source.filter(u => {
        const name = (u.name||'').toLowerCase();
        const email = (u.email||'').toLowerCase();
        const phoneRaw = (u.phoneNumber||'');
        const phoneTxt = phoneRaw.toLowerCase();
        // also match normalized phone digits
        const nums = splitPhones(phoneRaw);
        const phoneMatch = phoneTxt.includes(q) || nums.some(p=> p.replace('+','').includes(q.replace('+','')));
        return name.includes(q) || email.includes(q) || phoneMatch;
      });
      
      dbg('renderRows', { tab: usersActiveTab, total: source.length, filtered: filtered.length, emailOn, waOn });

      // Always render rows, even if no platforms selected (show name only)
      if (filtered.length === 0) {
        tableBody.innerHTML = '<tr><td colspan="4" class="text-muted text-center">Aucun résultat</td></tr>';
        return;
      }

      tableBody.innerHTML = filtered.map(u=>{
        const emailCell = emailOn ? (u.email || '<span class="text-muted">—</span>') : '';
        const phones = splitPhones(u.phoneNumber);
        const phoneFirst = phones.length ? phones[0] : null;
        const phoneCell = waOn ? (phoneFirst ? phoneFirst : '<span class="text-muted">—</span>') : '';
        return `<tr>
          <td class="fw-semibold">${u.name || ('ID '+u.id)}</td>
          ${emailOn ? `<td>${emailCell}</td>` : ''}
          ${waOn ? `<td>${phoneCell}</td>` : ''}
          <td class="text-end"><button type="button" class="btn btn-sm btn-outline-primary user-add" data-id="${u.id}">+</button></td>
        </tr>`;
      }).join('');

      // Wire up + buttons
      tableBody.querySelectorAll('.user-add').forEach(btn=>{
        btn.addEventListener('click', ()=>{
          const id = btn.getAttribute('data-id');
          const list = usersActiveTab === 'def' ? usersDefList : usersGrhList;
          const user = list.find(x => (x.id ?? x.userId).toString() === id);
          if (!user) return;
          
          const emailOnNow = document.getElementById('platformEmail')?.checked || false;
          const waOnNow = document.getElementById('platformWhatsApp')?.checked || false;
          
          if (emailOnNow && user.email) {
            addTagTo('destEmails', user.email);
          }
          if (waOnNow) {
            if (user.phoneNumber) {
              const phones = splitPhones(user.phoneNumber);
              if (phones.length){
                phones.forEach(p=> addTagTo('destPhones', p));
              } else {
                try { showFinalStatusToast(`${user.name || 'Cet utilisateur'} n'a pas de numéro WhatsApp.`, 'warning'); } catch {}
              }
            }
          }
        });
      });
    };

    // Setup tabs just under the search bar
    let tabs = document.getElementById('userTabs');
    if (!tabs && searchBox) {
      tabs = document.createElement('div');
      tabs.id = 'userTabs';
      tabs.className = 'btn-group mb-2';
      tabs.innerHTML = `
        <button type="button" id="userTabDef" class="btn btn-outline-secondary active">Utilisateurs</button>
        <button type="button" id="userTabGrh" class="btn btn-outline-secondary">Employés GRH</button>
      `;
      searchBox.parentElement?.insertAdjacentElement('afterend', tabs);
      
      const tabDef = tabs.querySelector('#userTabDef');
      const tabGrh = tabs.querySelector('#userTabGrh');
      const setActive = (tab)=>{
        usersActiveTab = tab;
        tabs.querySelectorAll('button').forEach(b=>b.classList.remove('active'));
        (tab === 'def' ? tabDef : tabGrh)?.classList.add('active');
        renderRows();
      };
      tabDef?.addEventListener('click', ()=> setActive('def'));
      tabGrh?.addEventListener('click', ()=> setActive('grh'));
    }

    // Wire up event listeners
    searchBox.addEventListener('input', renderRows);
    const emailCheck = document.getElementById('platformEmail');
    const waCheck = document.getElementById('platformWhatsApp');
    if (emailCheck) emailCheck.addEventListener('change', renderRows);
    if (waCheck) waCheck.addEventListener('change', renderRows);
    
    // Initial render
    renderRows();

    // Fallback: render as checkboxes into #usersCheckboxes
    const boxContainer = document.getElementById('usersCheckboxes');
    if (boxContainer) {
      const merged = [...usersDefList, ...usersGrhList];
      boxContainer.innerHTML = merged.map(u=>{
        const label = (u.name || u.login || ("ID "+u.id));
        return `<div class="form-check"><input class="form-check-input user-check" type="checkbox" value="${u.id}" id="u_${u.id}"><label class="form-check-label" for="u_${u.id}">${label}</label></div>`;
      }).join('');

      // Hook checkbox change to auto-fill emails/phones when platforms selected
      boxContainer.querySelectorAll('.user-check').forEach(cb=>{
        cb.addEventListener('change', ()=>{
          const userId = cb.value;
          const list = [...usersDefList, ...usersGrhList];
          const user = list.find(x => (x.id ?? x.userId).toString() === userId);
          const emailOn = document.getElementById('platformEmail')?.checked;
          const waOn = document.getElementById('platformWhatsApp')?.checked;
          if (cb.checked && user){
            if (emailOn && user.email) addTagTo('destEmails', user.email);
            if (waOn){
              splitPhones(user.phoneNumber).forEach(p=> addTagTo('destPhones', p));
            }
          }
        });
      });
    } else {
      // Fallback: populate the legacy select element if present
      const selectElement = document.getElementById('selectedUsers');
      if (selectElement) {
        const merged = [...usersDefList, ...usersGrhList];
        selectElement.innerHTML = merged.length > 0
          ? merged.map(user => `<option value="${user.id}">${user.name || user.login || ('ID '+user.id)} (${user.email || 'Pas d\'email'})</option>`).join('')
          : '<option value="">Aucun utilisateur actif</option>';
      }
    }
  } catch (error) {
    logError('loadUsers: Failed to load users', error);
    const selectElement = document.getElementById('selectedUsers');
    if (selectElement) {
      selectElement.innerHTML = '<option value="">Erreur lors du chargement des utilisateurs</option>';
    }
  }
}

// Get selected users data
function getSelectedUsersData(){
  // Prefer checkboxes when present
  const boxContainer = document.getElementById('usersCheckboxes');
  if (boxContainer){
    const ids = Array.from(boxContainer.querySelectorAll('.user-check:checked')).map(x=>x.value);
    return usersList.filter(u => ids.includes((u.id ?? u.userId).toString()));
  }
  // Fallback legacy select
  const selectElement = document.getElementById('selectedUsers');
  if (!selectElement) return [];
  const selectedValues = Array.from(selectElement.selectedOptions).map(option => option.value);
  return usersList.filter(user => selectedValues.includes((user.id ?? user.userId).toString()));
}

// Compose send (with undo functionality)
const sendBtn = document.getElementById('composeSendBtn');
let isSending = false;
if (sendBtn){
  dbg('SendButton: Setting up click handler');
  // Defensive re-enabler in case a native dialog interrupted the flow
  document.addEventListener('visibilitychange', ()=>{
    if (document.visibilityState === 'visible') { sendBtn.disabled = false; isSending = false; sendBtn.classList.remove('disabled'); }
  });
  sendBtn.addEventListener('click', async ()=>{
    if (isSending) { dbg('SendButton: Already sending - ignored'); return; }
    // Ensure button is always enabled unless we actually start the request
    sendBtn.disabled = false; sendBtn.classList.remove('disabled');
    isSending = true;
    dbg('SendButton: Clicked, starting send process');
    
    const title = document.getElementById('composeTitle')?.value || '';
    const message = document.getElementById('composeMessage')?.value || '';
    const emails = getTagValues('destEmails');
    const phones = getTagValues('destPhones');
    const platforms = {
      Email: document.getElementById('platformEmail')?.checked || false,
      WhatsApp: document.getElementById('platformWhatsApp')?.checked || false,
      Desktop: document.getElementById('platformDesktop')?.checked || false
    };
    
    // Get selected users and combine with manual inputs
    const selectedUsers = getSelectedUsersData();
    
    // Combine emails: manual + selected users (if Email platform is selected)
    const combinedEmails = [...emails];
    if (platforms.Email) {
      selectedUsers.forEach(user => {
        if (user.email && !combinedEmails.includes(user.email)) {
          combinedEmails.push(user.email);
        }
      });
    }
    
    // Combine phones: manual + selected users (if WhatsApp platform is selected)
    const combinedPhones = [...phones];
    if (platforms.WhatsApp) {
      selectedUsers.forEach(user => {
        if (user.phoneNumber && !combinedPhones.includes(user.phoneNumber)) {
          combinedPhones.push(user.phoneNumber);
        }
      });
      // If user checked WhatsApp but provided no phone numbers explicitly,
      // try to extract E.164-like numbers from the message body as a convenience.
      if (combinedPhones.length === 0 && typeof message === 'string' && message.length > 0) {
        try {
          const rawMatches = message.match(/\+?\d[\d\s\-()]{7,}/g) || [];
          const cleanedPhones = Array.from(new Set(rawMatches
            .map(s => s.replace(/[^\d+]/g, ''))
            .map(p => p.startsWith('+') ? p : ('+' + p))
          ));
          cleanedPhones.forEach(p => {
            if (!combinedPhones.includes(p)) combinedPhones.push(p);
          });
        } catch (_) { /* ignore extraction errors */ }
      }
    }
    
    // Get desktop user IDs (if Desktop platform is selected)
    const desktopUserIds = platforms.Desktop ? selectedUsers.map(user => user.userId) : [];

    // Build explicit maps: email -> util_id, phone -> util_id to help backend set DestinataireUserId
    const emailUserMap = {};
    const phoneUserMap = {};
    try {
      const defList = usersDefList || [];
      const normEmail = e => (e||'').toString().trim().toLowerCase();
      const normPhoneDigits = p => {
        let s = (p||'').toString();
        s = s.replace(/[^+\d]/g,'');
        if (s.startsWith('00')) s = '+'+s.substring(2);
        if (!s.startsWith('+') && /^\d{8,15}$/.test(s)) { if (s.length===8) s = '+216'+s; else s = '+'+s; }
        return s.replace(/\D/g,'');
      };
      // Map emails to util_id
      combinedEmails.forEach(e=>{
        const m = defList.find(d => normEmail(d.email) === normEmail(e));
        if (m && m.userId) emailUserMap[e] = m.userId;
      });
      // For phones, try direct match on numbers present in defList via usersGrhList linkage when possible
      const phoneIndex = new Map();
      [...usersDefList, ...usersGrhList].forEach(u=>{
        const nums = (u.phoneNumber ? (u.phoneNumber.match(/\d{8,15}/g) || []) : []);
        nums.forEach(n=>{
          const key = normPhoneDigits(n);
          if (key && !phoneIndex.has(key)) phoneIndex.set(key, u.userId || u.id);
        });
      });
      combinedPhones.forEach(p=>{
        const key = normPhoneDigits(p);
        const uid = phoneIndex.get(key);
        if (uid) phoneUserMap[p] = uid;
      });
    } catch {}
    
    dbg('SendButton: Collected form data', { 
      title, 
      message, 
      emails: combinedEmails, 
      phones: combinedPhones,
      desktopUserIds,
      platforms,
      selectedUsersCount: selectedUsers.length,
      titleLength: title.length,
      messageLength: message.length,
      emailCount: combinedEmails.length,
      phoneCount: combinedPhones.length
    });
    
    // Validation (never disable on validation errors)
    if (!title.trim()) {
      logError('SendButton: Validation failed', new Error('Title is required'));
      alert('Le titre de l\'alerte est obligatoire');
      isSending = false; sendBtn.disabled = false; sendBtn.classList.remove('disabled');
      return;
    }
    
    if (!platforms.Email && !platforms.WhatsApp && !platforms.Desktop) {
      logError('SendButton: Validation failed', new Error('No platform selected'));
      alert('Veuillez sélectionner au moins une plateforme d\'envoi');
      isSending = false; sendBtn.disabled = false; sendBtn.classList.remove('disabled');
      return;
    }
    
    // Check if we have at least one recipient
    if (combinedEmails.length === 0 && combinedPhones.length === 0 && desktopUserIds.length === 0) {
      logError('SendButton: Validation failed', new Error('No recipients'));
      alert('Veuillez ajouter au moins un destinataire (email, téléphone ou utilisateur)');
      isSending = false; sendBtn.disabled = false; sendBtn.classList.remove('disabled');
      return;
    }

    // If WhatsApp is selected, ensure there is at least one phone number after extraction
    if (platforms.WhatsApp && combinedPhones.length === 0) {
      logError('SendButton: Validation failed', new Error('No WhatsApp phone numbers'));
      alert('Veuillez ajouter au moins un numéro de téléphone pour WhatsApp (ex: +216XXXXXXXX)');
      isSending = false; sendBtn.disabled = false; sendBtn.classList.remove('disabled');
      return;
    }
    
    try {
      console.groupCollapsed('Send:compose');
      dbg('SendButton: Starting API request', { 
        url: '/AlertsCrud/Send',
        payload: { title, message, emails: combinedEmails, phones: combinedPhones, desktopUserIds, platforms }
      });
      
      // Disable only while the request is in-flight
      sendBtn.disabled = true; sendBtn.classList.add('disabled');
      
      // No long-running spinner anymore; we'll show a short toast on success
      
      const typeVal = document.getElementById('composeType')?.value || 'acquittementNonNecessaire';
      const payload = {
          title, 
          message, 
          emails: combinedEmails,
          phones: combinedPhones,
          userIds: desktopUserIds,
          emailUserMap,
          phoneUserMap,
          platforms: {
            Email: !!platforms.Email,
            WhatsApp: !!platforms.WhatsApp,
            Desktop: !!platforms.Desktop
        },
        alertTypeId: (typeVal === 'acquittementNecessaire' ? 2 : 1)
      };
      console.debug('Send:compose', payload);
      const r = await fetch('/AlertsCrud/Send', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });
      
      dbg('SendButton: Received response', { 
        status: r.status, 
        statusText: r.statusText,
        ok: r.ok 
      });
      
      if (!r.ok) {
        const errorText = await r.text();
        throw new Error(`HTTP ${r.status}: ${errorText}`);
      }
      
      const j = await r.json();
      dbg('SendButton: Response data', j);
      
      // Show queued toast immediately
      if (j && (j.success === true || r.status === 200 || r.status === 207)) {
        showFinalStatusToast("Alerte mise en file d'attente pour envoi.", 'info');
      } else {
        logError('SendButton: Invalid response', new Error('Missing success flag'), j);
      }
      
      // Fermer le modal et recharger Inbox pour voir la nouvelle alerte
      const modalEl = document.getElementById('newAlertModal');
      if (modalEl && window.bootstrap?.Modal){ 
        dbg('SendButton: Closing modal');
        window.bootstrap.Modal.getOrCreateInstance(modalEl).hide(); 
      }
      
      dbg('SendButton: Reloading inbox');
      inboxPage = 1; 
      await loadInbox();
      try { loadOutboxKpiData(); } catch {}
      try { loadInboxKpiData(); } catch {}
      
      logSuccess('SendButton: Alert sent successfully', { alerteId: j.alerteId });
      console.groupEnd();
    } catch(err){
      logError('SendButton: Send failed', err, { title, message, emails, phones, platforms });
      console.groupEnd();
      alert("Échec de création d'alerte: " + err.message);
    }
    finally { isSending = false; sendBtn.disabled = false; sendBtn.classList.remove('disabled'); }
  });
} else {
  dbg('SendButton: Send button not found');
}

function setupDynamicPlatforms(){
  const emailChk = document.getElementById('platformEmail');
  const waChk = document.getElementById('platformWhatsApp');
  const emailInput = document.getElementById('destEmails')?.closest('.col-12');
  const phoneInput = document.getElementById('destPhones')?.closest('.col-12');
  const updateVis = () => {
    if (emailInput) emailInput.style.display = emailChk?.checked ? '' : 'none';
    if (phoneInput) phoneInput.style.display = waChk?.checked ? '' : 'none';
    dbg('platforms:toggle', { email: !!emailChk?.checked, whatsapp: !!waChk?.checked });
  };
  emailChk?.addEventListener('change', updateVis);
  waChk?.addEventListener('change', updateVis);
  updateVis();
}

function setupTagInputs(){
  makeTagInput('destEmails', /[,\s]/);
  makeTagInput('destPhones', /[,\s]/, normalizePhone);
}

function makeTagInput(inputId, separatorRegex, normalizer){
  const input = document.getElementById(inputId);
  if (!input) return;
  if (input.dataset.tagInit === '1') { dbg('tag:init:already', { inputId }); return; }
  const wrap = document.createElement('div');
  wrap.className = 'tag-input form-control';
  input.parentNode.insertBefore(wrap, input);
  input.style.display = 'none';
  const list = document.createElement('div'); list.className = 'tags'; wrap.appendChild(list);
  const editor = document.createElement('input'); editor.type = 'text'; editor.className = 'tag-editor'; wrap.appendChild(editor);
  input.dataset.tagInit = '1';
  const addTag = (raw)=>{
    let v = (raw||'').trim();
    if (!v) return;
    if (normalizer) v = normalizer(v);
    // Prevent duplicates
    const existing = Array.from(list.querySelectorAll('.tag')).map(t=>t.firstChild.nodeValue);
    if (existing.includes(v)) return;
    const tag = document.createElement('span'); tag.className='tag'; tag.textContent=v;
    const x = document.createElement('button'); x.type='button'; x.className='tag-x'; x.textContent='×';
    x.onclick = ()=>{ list.removeChild(tag); syncHidden(); };
    tag.appendChild(x); list.appendChild(tag); syncHidden();
    dbg('tag:add', { inputId, value:v });
  };
  const syncHidden = ()=>{
    const vals = Array.from(list.querySelectorAll('.tag')).map(t=>t.firstChild.nodeValue);
    input.value = vals.join(',');
    dbg('tag:sync', { inputId, values: vals });
  };
  editor.addEventListener('keydown', (e)=>{
    if (e.key==='Enter' || separatorRegex.test(editor.value)){
      e.preventDefault();
      const parts = editor.value.split(separatorRegex).filter(x=>x.trim());
      parts.forEach(addTag);
      editor.value='';
    }
    if (e.key==='Backspace' && !editor.value && list.lastChild){ list.removeChild(list.lastChild); syncHidden(); }
  });
  // seed from existing hidden value
  (input.value||'').split(',').filter(x=>x).forEach(addTag);
}

function getTagValues(inputId){
  const hidden = document.getElementById(inputId);
  if (!hidden) return [];
  return (hidden.value||'').split(',').map(s=>s.trim()).filter(Boolean);
}

// Helper to programmatically add a tag to an input
function addTagTo(inputId, value){
  const hidden = document.getElementById(inputId);
  if (!hidden) return;
  const wrap = hidden.previousSibling;
  if (!wrap || !wrap.classList || !wrap.classList.contains('tag-input')) return;
  const list = wrap.querySelector('.tags');
  const vals = Array.from(list.querySelectorAll('.tag')).map(t=>t.firstChild.nodeValue);
  let v = value;
  if (inputId === 'destPhones') v = normalizePhone(value);
  if (vals.includes(v)) return;
  const tag = document.createElement('span'); tag.className='tag'; tag.textContent=v;
  const x = document.createElement('button'); x.type='button'; x.className='tag-x'; x.textContent='×';
  x.onclick = ()=>{ list.removeChild(tag); const arr = Array.from(list.querySelectorAll('.tag')).map(t=>t.firstChild.nodeValue); hidden.value = arr.join(','); };
  tag.appendChild(x); list.appendChild(tag);
  const arr = Array.from(list.querySelectorAll('.tag')).map(t=>t.firstChild.nodeValue);
  hidden.value = arr.join(',');
}

function normalizePhone(raw){
  try {
    let p = (raw||'').toString().trim();
    // existing normalization rules...
    if (!p) return '';
    // Keep only digits and plus
    p = p.replace(/[^+\d]/g,'');
    if (p.startsWith('00')) p = '+' + p.substring(2);
    if (!p.startsWith('+') && /^\d{8,15}$/.test(p)) {
      // assume TN if 8 digits
      if (p.length === 8) p = '+216' + p;
      else p = '+' + p;
    }
    return p;
  } catch { return (raw||''); }
}

// Split a raw phone field containing one or multiple numbers separated by non-digits or slashes
function splitPhones(raw){
  const text = (raw||'').toString();
  // Extract digit groups of length >= 8
  const groups = text.match(/\d{8,15}/g) || [];
  const uniques = new Set();
  const result = [];
  for (const g of groups){
    const norm = normalizePhone(g);
    if (norm && !uniques.has(norm)) { uniques.add(norm); result.push(norm); }
  }
  return result;
}

async function updateTodayKpi(){
  try {
    const count = await fetchJson('/Alerts/TodayCount');
    const el = document.getElementById('todayCount');
    if (el){ el.textContent = count; }
  } catch {}
}

async function updateMandatoryConfirmedKpi(){
  try {
    const count = await fetchJson('/Alerts/ConfirmedMandatoryCount');
    const el = document.getElementById('mandatoryConfirmedCount');
    if (el){ el.textContent = count; }
  } catch {}
}

async function updateMandatoryPendingKpi(){
  try {
    const count = await fetchJson('/Alerts/MandatoryPendingCount');
    const el = document.getElementById('mandatoryPendingCount');
    if (el){ el.textContent = count; }
  } catch {}
}

// Undo Send functionality
let undoTimeout = null;
let currentAlertId = null;

// Sending notification removed (legacy spinner/undo deleted)
function showSendingNotification() {}
function hideSendingNotification() {}
function startUndoTimer(){ }
async function cancelSend(){ }

function showFinalStatusToast(message, type) {
  // Create toast notification
  const toast = document.createElement('div');
  toast.className = `toast align-items-center text-white bg-${type}`;
  toast.setAttribute('role', 'alert');
  toast.setAttribute('aria-live', 'assertive');
  toast.setAttribute('aria-atomic', 'true');
  toast.style.cssText = 'position: fixed; top: 20px; right: 20px; z-index: 9999;';
  
  toast.innerHTML = `
    <div class="d-flex">
      <div class="toast-body">${message}</div>
      <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
    </div>
  `;
  
  document.body.appendChild(toast);
  
  // Show toast
  const bsToast = new bootstrap.Toast(toast, { delay: 3000 });
  bsToast.show();
  
  // Remove toast after it's hidden
  toast.addEventListener('hidden.bs.toast', () => toast.remove());
}

// Function to confirm an alert (for alerts requiring acknowledgment)
async function confirmAlert(alertId) {
  try {
    dbg('confirmAlert: Starting confirmation for alert', { alertId });
    
    // Show loading state
    const button = event.target;
    const originalText = button.textContent;
    button.disabled = true;
    button.textContent = 'Confirmation...';
    
    // Get the historique ID from the row
    const row = button.closest('.gmail-alert-row');
    const historiqueId = row ? row.getAttribute('data-historique-id') : alertId;
    
    dbg('confirmAlert: Using historiqueId', { historiqueId, alertId });
    
    // Make API call to confirm the alert
    const response = await fetch('/Alerts/MarkRead', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/x-www-form-urlencoded',
      },
      body: `alertRecipientId=${historiqueId}`
    });
    
    dbg('confirmAlert: API response', {
      status: response.status,
      statusText: response.statusText,
      ok: response.ok
    });
    
    if (response.ok) {
      // Success - update UI in real-time
      showFinalStatusToast('Alerte confirmée avec succès', 'success');
      
      // Find and update the specific alert row
      const row = button.closest('.gmail-alert-row');
      if (row) {
        // Update the badge to show "Lu" with green color
        const badge = row.querySelector('.badge');
        if (badge) {
          badge.textContent = 'Lu';
          badge.className = 'badge bg-success me-1';
        }
        
        // Remove the confirm button and show confirmation time
        const actionsCol = row.querySelector('.actions-col');
        if (actionsCol) {
          actionsCol.innerHTML = `<span class="text-muted small">Confirmé ${new Date().toLocaleTimeString('fr-FR', { hour:'2-digit', minute:'2-digit' })}</span>`;
        }
        
        // Add visual feedback - briefly highlight the row
        row.style.transition = 'background-color 0.3s ease';
        row.style.backgroundColor = '#d4edda';
        setTimeout(() => {
          row.style.backgroundColor = '';
        }, 1000);
      }
      
      // Update the sidebar counts in real-time
      updateSidebarCounts();
      
      logSuccess('confirmAlert: Alert confirmed successfully', { alertId });
    } else {
      const errorText = await response.text();
      logError('confirmAlert: Server error response', new Error(`HTTP ${response.status}: ${errorText}`), {
        status: response.status,
        statusText: response.statusText,
        responseText: errorText
      });
      showFinalStatusToast('Erreur lors de la confirmation', 'danger');
    }
  } catch (error) {
    logError('confirmAlert: Network or other error', error, { alertId });
    showFinalStatusToast('Erreur lors de la confirmation', 'danger');
  } finally {
    // Restore button state
    if (event && event.target) {
      const button = event.target;
      button.disabled = false;
      button.textContent = originalText;
    }
  }
}

// Function to update sidebar counts in real-time
async function updateSidebarCounts() {
  try {
    // Sidebar badges removed per UI polish; refresh KPI cards instead
    await Promise.allSettled([
      loadInboxKpiData().catch(()=>{}),
      loadOutboxKpiData().catch(()=>{})
    ]);
    
    dbg('updateSidebarCounts: Updated sidebar counts successfully');
  } catch (error) {
    logError('updateSidebarCounts: Failed to update sidebar counts', error);
  }
}

// Real-time KPI updates via SignalR
let hubConnection = null;

function getCurrentUserId() {
  // Try to get user ID from a data attribute or global variable
  // This is a simple implementation - you may need to adjust based on your auth system
  var userIdElement = document.querySelector('[data-user-id]');
  if (userIdElement) {
    return userIdElement.getAttribute('data-user-id');
  }
  
  // Fallback: try to get from window object if set by server
  if (window.currentUserId) {
    return window.currentUserId;
  }
  
  // For testing, return a default user ID
  return '1';
}

function initializeSignalR() {
  dbg('SignalR: Starting initialization...');
  console.log('SignalR: typeof signalR =', typeof signalR);
  console.log('SignalR: signalR object =', signalR);
  
  if (typeof signalR !== 'undefined') {
    dbg('SignalR: signalR library found, creating connection...');
    try {
      hubConnection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/notifications')
        .withAutomaticReconnect()
        .build();
      dbg('SignalR: Connection builder created successfully');
      console.log('SignalR: Hub connection created =', hubConnection);

      // Handle general notifications
      hubConnection.on('ReceiveNotification', function (type, data) {
        console.log('SignalR: Received notification', { type, data });
        dbg('SignalR: Received notification', { type, data });
        
        switch (type) {
          case 'AlertCreated':
            // Refresh outbox when a new alert is created
            if (currentView === 'outbox') {
              loadSent();
            }
            loadOutboxKpiData();
            loadSidebarCounts(); // Update sidebar counts
            // If the sender is the current user, optionally add to outbox list immediately
            break;
          case 'AlertProcessed':
            // Refresh both inbox and outbox when an alert is processed
            if (currentView === 'inbox') {
              loadInbox();
            } else if (currentView === 'outbox') {
              loadSent();
            }
            loadInboxKpiData();
            loadOutboxKpiData();
            loadSidebarCounts(); // Update sidebar counts
            break;
          case 'AlertReceived':
            // Refresh inbox when a new alert is received
            if (currentView === 'inbox') {
              loadInbox();
            }
            loadInboxKpiData();
            loadSidebarCounts(); // Update sidebar counts
            break;
          case 'NewAlertReceived':
            if (currentView === 'inbox') { loadInbox(); }
            loadInboxKpiData();
            loadSidebarCounts();
            break;
          case 'AlertStatusUpdated':
            try {
              loadInboxKpiData();
              loadOutboxKpiData();
              if (currentView === 'inbox') { loadInbox(); }
              if (currentView === 'outbox') { loadSent(); }
              const modal = document.getElementById('alertDetailsModal');
              if (modal && modal.classList.contains('show')) {
                const selectedRow = document.querySelector('#sentList .gmail-alert-row.selected');
                const id = selectedRow?.getAttribute('data-id');
                if (id) { loadOutboxDetails(parseInt(id,10)); }
              }
            } catch {}
            break;
        }
      });

      // Dedicated KPI refresh channel
      hubConnection.on('UpdateKpis', function(){
        try { updateSidebarCounts(); } catch {}
      });

      // Handle outbox modal updates
      hubConnection.on('UpdateOutboxModal', function (data) {
        console.log('SignalR: Received UpdateOutboxModal event', data);
        dbg('SignalR: Received outbox modal update', data);
        updateOutboxModal(data);
      });

      // Handle new alert added to outbox
      hubConnection.on('AddNewAlertToOutbox', function (alertData) {
        console.log('SignalR: Received AddNewAlertToOutbox event', alertData);
        dbg('SignalR: Received new alert for outbox', alertData);
        addNewAlertToOutbox(alertData);
      });

      // Handle new alert added to inbox
      hubConnection.on('AddNewAlertToInbox', function (alertData) {
        console.log('SignalR: Received AddNewAlertToInbox event', alertData);
        dbg('SignalR: Received new alert for inbox', alertData);
        addNewAlertToInbox(alertData);
      });
      
      console.log('SignalR: Event handlers registered');

      // Start the connection
      console.log('SignalR: Starting connection...');
      hubConnection.start()
        .then(function () {
          console.log('SignalR: Connected successfully, state =', hubConnection.state);
          dbg('SignalR: Connected successfully');
          logSuccess('SignalR: Connection established');
          
          // Join user group for real-time updates
          var userId = getCurrentUserId();
          console.log('SignalR: Current user ID =', userId);
          if (userId) {
            console.log('SignalR: Joining user group...');
            hubConnection.invoke('JoinUserGroup', userId.toString())
              .then(function () {
                console.log('SignalR: Successfully joined user group for user', userId);
                dbg('SignalR: Joined user group for user', userId);
              })
              .catch(function (err) {
                console.error('SignalR: Failed to join user group', err);
                logError('SignalR: Failed to join user group', err);
              });
          }
        })
        .catch(function (err) {
          console.error('SignalR: Connection failed', err);
          logError('SignalR: Connection failed', err);
        });
    } catch (error) {
      logError('SignalR: Initialization failed', error);
    }
  } else {
    dbg('SignalR: signalR library not available');
  }
}

function updateInboxKpis(kpiData) {
  console.log('updateInboxKpis called with:', kpiData);
  try {
    // Update unread count
    const unreadElement = document.getElementById('unreadAlertsCount');
    console.log('Found unread element:', unreadElement);
    if (unreadElement && kpiData.unreadCount !== undefined) {
      unreadElement.textContent = kpiData.unreadCount;
      console.log('Updated unread count to:', kpiData.unreadCount);
    }

    // Update pending count
    const pendingElement = document.getElementById('pendingConfirmationCount');
    console.log('Found pending element:', pendingElement);
    if (pendingElement && kpiData.mandatoryPendingCount !== undefined) {
      pendingElement.textContent = kpiData.mandatoryPendingCount;
      console.log('Updated pending count to:', kpiData.mandatoryPendingCount);
    }

    // Update received today count (if available)
    const receivedElement = document.getElementById('receivedTodayCount');
    console.log('Found received element:', receivedElement);
    if (receivedElement && kpiData.receivedTodayCount !== undefined) {
      receivedElement.textContent = kpiData.receivedTodayCount;
      console.log('Updated received count to:', kpiData.receivedTodayCount);
    }

    dbg('SignalR: Updated inbox KPIs', kpiData);
    logSuccess('SignalR: Inbox KPIs updated successfully');
  } catch (error) {
    console.error('Error in updateInboxKpis:', error);
    logError('SignalR: Failed to update inbox KPIs', error, { kpiData });
  }
}

function updateOutboxKpis(kpiData) {
  console.log('updateOutboxKpis called with:', kpiData);
  try {
    // Update sent today count
    const sentElement = document.getElementById('sentTodayCount');
    console.log('Found sent element:', sentElement);
    if (sentElement && kpiData.sentTodayCount !== undefined) {
      sentElement.textContent = kpiData.sentTodayCount;
      console.log('Updated sent count to:', kpiData.sentTodayCount);
    }

    // Update confirmed count
    const confirmedElement = document.getElementById('confirmedAlertsCount');
    console.log('Found confirmed element:', confirmedElement);
    if (confirmedElement && kpiData.confirmedCount !== undefined) {
      confirmedElement.textContent = kpiData.confirmedCount;
      console.log('Updated confirmed count to:', kpiData.confirmedCount);
    }

    // Update pending count
    const pendingElement = document.getElementById('pendingConfirmationCount');
    console.log('Found pending element:', pendingElement);
    if (pendingElement && kpiData.pendingCount !== undefined) {
      pendingElement.textContent = kpiData.pendingCount;
      console.log('Updated pending count to:', kpiData.pendingCount);
    }

    dbg('SignalR: Updated outbox KPIs', kpiData);
    logSuccess('SignalR: Outbox KPIs updated successfully');
  } catch (error) {
    console.error('Error in updateOutboxKpis:', error);
    logError('SignalR: Failed to update outbox KPIs', error, { kpiData });
  }
}

function updateOutboxModal(data) {
  try {
    dbg('SignalR: Updating outbox modal', data);
    
    // Check if the modal is open for this specific alert
    const modal = document.getElementById('outboxDetailsModal');
    if (modal && modal.style.display !== 'none') {
      const currentAlertId = modal.getAttribute('data-alert-id');
      if (currentAlertId && parseInt(currentAlertId) === data.alerteId) {
        // Update the recipient list in the modal
        if (data.recipientData) {
          // Refresh the recipient list for this alert
          loadOutboxDetails(data.alerteId);
        }
      }
    }
    
    logSuccess('SignalR: Outbox modal updated successfully');
  } catch (error) {
    logError('SignalR: Failed to update outbox modal', error, { data });
  }
}

function addNewAlertToOutbox(alertData) {
  dbg('Adding new alert to outbox:', alertData);
  
  // Create the alert HTML element
  const alertElement = createAlertElement(alertData, 'outbox');
  
  // Add to the alerts list (prepend to show newest first)
  const alertsList = document.querySelector('#alertsList');
  if (alertsList) {
    alertsList.insertBefore(alertElement, alertsList.firstChild);
    
    // Update the count in the sidebar
    updateOutboxCount();
  }
}

function addNewAlertToInbox(alertData) {
  dbg('Adding new alert to inbox:', alertData);
  
  // Create the alert HTML element
  const alertElement = createAlertElement(alertData, 'inbox');
  
  // Add to the alerts list (prepend to show newest first)
  const alertsList = document.querySelector('#alertsList');
  if (alertsList) {
    alertsList.insertBefore(alertElement, alertsList.firstChild);
    
    // Update the count in the sidebar
    updateInboxCount();
  }
}

function createAlertElement(alertData, type) {
  const statusClass = alertData.status === 'Envoyé' ? 'success' : 'danger';
  const statusText = alertData.status === 'Envoyé' ? 'Envoyé' : 'Échoué';
  
  return `
    <div class="alert-item" data-alert-id="${alertData.id}">
      <div class="d-flex justify-content-between align-items-start">
        <div class="flex-grow-1">
          <h6 class="mb-1 fw-semibold">${alertData.title}</h6>
          <p class="mb-1 text-muted small">${alertData.message}</p>
          <div class="d-flex align-items-center gap-2">
            <span class="badge bg-${statusClass}">${statusText}</span>
            <small class="text-muted">${alertData.date}</small>
          </div>
        </div>
        <div class="dropdown">
          <button class="btn btn-sm btn-outline-secondary" type="button" data-bs-toggle="dropdown">
            <i class="bi bi-three-dots-vertical"></i>
          </button>
          <ul class="dropdown-menu">
            <li><a class="dropdown-item" href="#" onclick="viewAlertDetails(${alertData.id})">
              <i class="bi bi-eye me-2"></i>Voir détails
            </a></li>
            <li><a class="dropdown-item" href="#" onclick="editAlert(${alertData.id})">
              <i class="bi bi-pencil me-2"></i>Modifier
            </a></li>
            <li><hr class="dropdown-divider"></li>
            <li><a class="dropdown-item text-danger" href="#" onclick="deleteAlert(${alertData.id})">
              <i class="bi bi-trash me-2"></i>Supprimer
            </a></li>
          </ul>
        </div>
      </div>
    </div>
  `;
}

function updateOutboxCount() {
  const alertsList = document.querySelector('#alertsList');
  if (alertsList) {
    const count = alertsList.children.length;
    const badge = document.querySelector('#outbox-badge');
    if (badge) {
      badge.textContent = count;
    }
  }
}

function updateInboxCount() {
  const alertsList = document.querySelector('#alertsList');
  if (alertsList) {
    const count = alertsList.children.length;
    const badge = document.querySelector('#inbox-badge');
    if (badge) {
      badge.textContent = count;
    }
  }
}


// Initialize SignalR when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
  // Initialize SignalR after a short delay to ensure all other initialization is complete
  setTimeout(initializeSignalR, 1000);
});


