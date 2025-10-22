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
        // initialize defaults
        const t = document.getElementById('composeType'); if (t) t.value = 'Information';
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

function showLoading(containerId){ const c = document.getElementById(containerId); if(!c) return; c.innerHTML = '<div class="gmail-loading"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Chargement...</span></div><span class="ms-2">Chargement des alertes...</span></div>'; }

function renderInboxList(containerId, items){
  const c = document.getElementById(containerId); if(!c) return;
  c.innerHTML = (items||[]).map(a=> {
    const preview = (a.message||'').trim();
    const dateText = new Date(a.createdAt||a.dateCreation).toLocaleDateString('fr-FR', { year:'numeric', month:'2-digit', day:'2-digit' });
    
    // Read state badge based on EtatAlerteId
    let readBadge = '';
    let readClass = '';
    switch(a.etatAlerteId || a.readStateId) {
      case 1: readBadge = 'Non Lu'; readClass = 'bg-danger'; break;
      case 2: readBadge = 'Lu'; readClass = 'bg-success'; break;
      default: readBadge = ''; readClass = ''; break;
    }
    
    return `
    <div class="gmail-alert-row" data-id="${a.id}">
      <div class="row-left title-col">
        <div class="d-flex align-items-center">
          <div class="flex-grow-1">
            <div class="fw-semibold">${(a.title||'Sans titre')}</div>
            <div class="text-muted small">De: ${a.senderName || a.sender || 'Système'}</div>
          </div>
          <div class="ms-2">
            ${readBadge ? `<span class="badge ${readClass} me-1">${readBadge}</span>` : ''}
          </div>
        </div>
      </div>
      <div class="row-main desc-col">${preview}</div>
      <div class="row-right date-col">${dateText}</div>
    </div>`;
  }).join('');
  
  // Add click handlers for inbox rows
  c.querySelectorAll('.gmail-alert-row').forEach(row=>{
    row.addEventListener('click', async ()=>{
      const id = row.getAttribute('data-id');
      
      // Add visual selection feedback
      dbg('renderInboxList: Alert row clicked', { id, containerId });
      
      // Remove selection from all rows in this container
      c.querySelectorAll('.gmail-alert-row').forEach(r => r.classList.remove('selected'));
      
      // Add selection to clicked row
      row.classList.add('selected');
      
      try {
        // show loading state in details modal first
        showDetailsModal({ title: 'Chargement…', message: 'Veuillez patienter…' });
        
        // For inbox alerts, show normal details
        const details = await fetchJson(`/Alerts/Details?id=${id}`);
        showDetailsModal(details);
      } catch (err){
        showDetailsModal({ title: 'Erreur', message: "Impossible de charger les détails de l'alerte." });
        console.error('Details load error', err);
      }
    });
  });
}

function renderOutboxList(containerId, items){
  const c = document.getElementById(containerId); if(!c) return;
  c.innerHTML = (items||[]).map(a=> {
    const preview = (a.message||'').trim();
    const dateText = new Date(a.createdAt||a.dateCreation).toLocaleDateString('fr-FR', { year:'numeric', month:'2-digit', day:'2-digit' });
    
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
    const data = await fetchJson(`/Alerts/InboxData?page=${inboxPage}&size=${pageSize}`);
    dbg('loadInbox: Received data', { 
      itemsCount: data.items?.length || 0, 
      total: data.total,
      page: inboxPage 
    });
    
    renderInboxList('inboxList', data.items);
    inboxTotal = data.total ?? 0;
    updatePagination('inbox', inboxPage, inboxTotal);
    
    logSuccess('loadInbox: Completed successfully', { 
      page: inboxPage, 
      total: inboxTotal,
      itemsCount: data.items?.length || 0
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
    const data = await fetchJson(`/Alerts/SentData?page=${sentPage}&size=${pageSize}`);
    dbg('loadSent: Received data', { 
      itemsCount: data.items?.length || 0, 
      total: data.total,
      page: sentPage 
    });
    
    renderOutboxList('sentList', data.items);
    sentTotal = data.total ?? 0;
    updatePagination('sent', sentPage, sentTotal);
    
    logSuccess('loadSent: Completed successfully', { 
      page: sentPage, 
      total: sentTotal,
      itemsCount: data.items?.length || 0
    });
  } catch (error) {
    logError('loadSent: Failed to load sent items', error, { page: sentPage, pageSize });
    throw error;
  }
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
function showDetailsModal(details){
  const modalEl = document.getElementById('alertDetailsModal');
  if (!modalEl) return;
  const title = details.title || '';
  const msg = typeof details.message === 'string' ? details.message : JSON.stringify(details, null, 2);
  const createdAt = details.createdAt ? new Date(details.createdAt).toLocaleString('fr-FR') : '';
  
  modalEl.querySelector('#detailTitle').textContent = title;
  modalEl.querySelector('#detailMessage').textContent = msg;
  modalEl.querySelector('#detailType').textContent = details.type || '';
  modalEl.querySelector('#detailStatus').textContent = details.status || '';
  modalEl.querySelector('#detailCreatedAt').textContent = createdAt;
  
  // Show recipients if available (for sent alerts)
  const recipientsContainer = modalEl.querySelector('#recipientsContainer');
  if (recipientsContainer && details.recipients && details.recipients.length > 0) {
    recipientsContainer.style.display = 'block';
    const recipientsList = modalEl.querySelector('#recipientsList');
    if (recipientsList) {
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
        const statusText = recipient.isRead ? 'Lu' : 'Non Lu';
        const readDate = recipient.readDate ? new Date(recipient.readDate).toLocaleString('fr-FR') : '';
        const platformBadge = recipient.platform ? `<span class="badge bg-info me-1">${recipient.platform}</span>` : '';
        
        return `
          <div class="d-flex justify-content-between align-items-center py-2 border-bottom">
            <div>
              <div class="fw-semibold">${identifier}</div>
              <div class="small text-muted">
                ${platformBadge}
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
  
  if (window.bootstrap && window.bootstrap.Modal){ window.bootstrap.Modal.getOrCreateInstance(modalEl).show(); }
}

// Load users for compose modal
let usersList = [];

async function loadUsers(){
  dbg('loadUsers: Starting');
  
  try {
    const data = await fetchJson('/Users/List');
    usersList = data.users || [];
    dbg('loadUsers: Received users', { count: usersList.length });
    
    // Populate the select element
    const selectElement = document.getElementById('selectedUsers');
    if (selectElement) {
      selectElement.innerHTML = usersList.map(user => 
        `<option value="${user.userId}">${user.fullName} (${user.email || 'Pas d\'email'})</option>`
      ).join('');
      
      logSuccess('loadUsers: Users loaded successfully', { count: usersList.length });
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
  const selectElement = document.getElementById('selectedUsers');
  if (!selectElement) return [];
  
  const selectedValues = Array.from(selectElement.selectedOptions).map(option => option.value);
  return usersList.filter(user => selectedValues.includes(user.userId.toString()));
}

// Compose send (with undo functionality)
const sendBtn = document.getElementById('composeSendBtn');
if (sendBtn){
  dbg('SendButton: Setting up click handler');
  sendBtn.addEventListener('click', async ()=>{
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
    }
    
    // Get desktop user IDs (if Desktop platform is selected)
    const desktopUserIds = platforms.Desktop ? selectedUsers.map(user => user.userId) : [];
    
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
    
    // Validation
    if (!title.trim()) {
      logError('SendButton: Validation failed', new Error('Title is required'));
      alert('Le titre de l\'alerte est obligatoire');
      return;
    }
    
    if (!platforms.Email && !platforms.WhatsApp && !platforms.Desktop) {
      logError('SendButton: Validation failed', new Error('No platform selected'));
      alert('Veuillez sélectionner au moins une plateforme d\'envoi');
      return;
    }
    
    // Check if we have at least one recipient
    if (combinedEmails.length === 0 && combinedPhones.length === 0 && desktopUserIds.length === 0) {
      logError('SendButton: Validation failed', new Error('No recipients'));
      alert('Veuillez ajouter au moins un destinataire (email, téléphone ou utilisateur)');
      return;
    }
    
    try {
      console.groupCollapsed('Send:compose');
      dbg('SendButton: Starting API request', { 
        url: '/AlertsCrud/Send',
        payload: { title, message, emails: combinedEmails, phones: combinedPhones, desktopUserIds, platforms }
      });
      
      // Show sending notification immediately
      showSendingNotification();
      dbg('SendButton: Sending notification displayed');
      
      const r = await fetch('/AlertsCrud/Send', { 
        method:'POST', 
        headers:{'Content-Type':'application/json'}, 
        body: JSON.stringify({ title, message, emails, phones, platforms }) 
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
      
      // Start 5-second timer for undo functionality
      if (j.success && j.alerteId) {
        dbg('SendButton: Starting undo timer', { alerteId: j.alerteId, jobId: j.jobId });
        startUndoTimer(j.alerteId, j.jobId);
      } else {
        logError('SendButton: Invalid response', new Error('Missing success or alerteId'), j);
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
      
      logSuccess('SendButton: Alert sent successfully', { alerteId: j.alerteId });
      console.groupEnd();
    } catch(err){
      logError('SendButton: Send failed', err, { title, message, emails, phones, platforms });
      console.groupEnd();
      hideSendingNotification();
      alert("Échec de création d'alerte: " + err.message);
    }
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
  const wrap = document.createElement('div');
  wrap.className = 'tag-input form-control';
  input.parentNode.insertBefore(wrap, input);
  input.style.display = 'none';
  const list = document.createElement('div'); list.className = 'tags'; wrap.appendChild(list);
  const editor = document.createElement('input'); editor.type = 'text'; editor.className = 'tag-editor'; wrap.appendChild(editor);
  const addTag = (raw)=>{
    let v = (raw||'').trim();
    if (!v) return;
    if (normalizer) v = normalizer(v);
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

function normalizePhone(p){
  let s = (p||'').replace(/[^\d+]/g,'');
  if (!s) return s;
  if (s.startsWith('+')) return s;
  if (s.startsWith('216') && s.length===11) return '+'+s; // Tunisia E.164
  if (s.length===8) return '+216'+s; // local 8-digit
  if (s.startsWith('0') && s.length>=9) return '+216'+s.slice(1);
  return s.startsWith('+')?s:('+'+s);
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

function showSendingNotification() {
  // Remove any existing notification
  hideSendingNotification();
  
  // Create notification bar
  const notification = document.createElement('div');
  notification.id = 'sendingNotification';
  notification.className = 'alert alert-info alert-dismissible fade show position-fixed';
  notification.style.cssText = 'bottom: 20px; left: 20px; z-index: 9999; min-width: 300px;';
  notification.innerHTML = `
    <div class="d-flex align-items-center">
      <div class="spinner-border spinner-border-sm me-2" role="status">
        <span class="visually-hidden">Loading...</span>
      </div>
      <span class="me-3">Envoi en cours...</span>
      <button type="button" class="btn btn-sm btn-outline-danger" id="undoBtn">Annuler</button>
    </div>
  `;
  
  document.body.appendChild(notification);
  
  // Add undo button click handler
  document.getElementById('undoBtn').addEventListener('click', cancelSend);
}

function hideSendingNotification() {
  const notification = document.getElementById('sendingNotification');
  if (notification) {
    notification.remove();
  }
  if (undoTimeout) {
    clearTimeout(undoTimeout);
    undoTimeout = null;
  }
}

function startUndoTimer(alerteId, jobId) {
  currentAlertId = alerteId;
  
  // Set 5-second timeout
  undoTimeout = setTimeout(async () => {
    hideSendingNotification();
    showFinalStatusToast('Alerte envoyée avec succès', 'success');
  }, 5000);
}

async function cancelSend() {
  dbg('cancelSend: Starting cancellation process', { currentAlertId });
  
  if (!currentAlertId) {
    logError('cancelSend: No current alert ID available', new Error('No alert ID'));
    showFinalStatusToast('Aucune alerte en cours d\'envoi', 'warning');
    return;
  }
  
  try {
    dbg('cancelSend: Sending cancellation request', { 
      alertId: currentAlertId, 
      url: `/AlertsCrud/CancelSend/${currentAlertId}` 
    });
    
    const response = await fetch(`/AlertsCrud/CancelSend/${currentAlertId}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' }
    });
    
    dbg('cancelSend: Received response', { 
      status: response.status, 
      statusText: response.statusText,
      ok: response.ok 
    });
    
    if (response.ok) {
      const responseData = await response.json();
      dbg('cancelSend: Response data', responseData);
      
      hideSendingNotification();
      showFinalStatusToast('Envoi annulé avec succès', 'warning');
      logSuccess('cancelSend: Alert cancelled successfully', { alertId: currentAlertId });
    } else {
      const errorText = await response.text();
      logError('cancelSend: Server error response', new Error(`HTTP ${response.status}: ${errorText}`), {
        status: response.status,
        statusText: response.statusText,
        responseText: errorText
      });
      showFinalStatusToast('Erreur lors de l\'annulation', 'danger');
    }
  } catch (error) {
    logError('cancelSend: Network or other error', error, { alertId: currentAlertId });
    showFinalStatusToast('Erreur lors de l\'annulation', 'danger');
  }
}

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


