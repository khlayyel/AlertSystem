import { dbg } from '../helpers.js';

dbg('ui.js: module loaded');

export function updateActiveNavigation() {
  dbg('updateActiveNavigation: Starting');
  try {
    // Remove active class from all nav links
    const navLinks = document.querySelectorAll('.nav-link, #sidebar a');
    navLinks.forEach(link => {
      link.classList.remove('active');
      dbg('updateActiveNavigation: Removed active class from', link.href);
    });
    // Get current path
    const currentPath = (window.location.pathname || '').toLowerCase();
    dbg('updateActiveNavigation: Current path', currentPath);
    // Add active class to matching nav link
    const routes = [
      { sel: 'a[href="/Dashboard/Inbox"]',  match: ['/dashboard/inbox','/','/home/index','/dashboard'] },
      { sel: 'a[href="/Dashboard/Sent"]',   match: ['/dashboard/sent'] },
      { sel: 'a[href="/DefUtilisateur"]',   match: ['/defutilisateur'] },
      { sel: 'a[href="/DefAlerte"]',        match: ['/defalerte'] },
      { sel: 'a[href="/DefApp"]',           match: ['/defapp'] },
      { sel: 'a[href="/DefTypeAlerte"]',    match: ['/deftypealerte'] },
      { sel: 'a[href="/AlertsCrud"]',       match: ['/alertscrud'] }
    ];

    let matched = false;
    for (const r of routes){
      if (r.match.some(m => currentPath === m || currentPath.startsWith(m + '/'))) {
        const el = document.querySelector(r.sel);
        if (el) {
          el.classList.add('active');
          matched = true;
          dbg('updateActiveNavigation: Added active class to', el.href || r.sel);
        }
        break;
      }
    }
    if (!matched && (currentPath === '/' || currentPath === '')) {
      const home = document.querySelector('a[href="/Dashboard/Inbox"]');
      if (home) home.classList.add('active');
    }
  } catch (error) {
    dbg('updateActiveNavigation: Failed', error);
  }
}

function rebindTabsAndKpis() {
  const sentTab = document.querySelector('[data-bs-target="#sent"]');
  if (sentTab) {
    sentTab.onclick = ()=>{
      dbg('UI TAB: Sent active (forced)');
      if(window.loadSent) window.loadSent();
      if(window.loadOutboxKpiData) window.loadOutboxKpiData();
      if(window.updateSidebarCounts) window.updateSidebarCounts();
    };
  }
  const inboxTab = document.querySelector('[data-bs-target="#inbox"]');
  if(inboxTab){
    inboxTab.onclick = ()=>{
      dbg('UI TAB: Inbox active (forced)');
      if(window.loadInbox) window.loadInbox();
      if(window.loadInboxKpiData) window.loadInboxKpiData();
      if(window.updateSidebarCounts) window.updateSidebarCounts();
    };
  }
}

export async function loadInboxKpiData() {
  dbg('loadInboxKpiData: Starting');
  try {
    const response = await fetch('/Dashboard/InboxKpiData');
    const data = await response.json();
    dbg('loadInboxKpiData: Received', data);
    const receivedTodayCount = document.getElementById('receivedTodayCount');
    const unreadAlertsCount = document.getElementById('unreadAlertsCount');
    const pendingConfirmationCount = document.getElementById('pendingConfirmationCount');
    if (receivedTodayCount) receivedTodayCount.textContent = data.receivedToday || 0;
    if (unreadAlertsCount) unreadAlertsCount.textContent = data.unreadAlerts || 0;
    if (pendingConfirmationCount) pendingConfirmationCount.textContent = data.pendingConfirmation || 0;
    rebindTabsAndKpis();
  } catch (err) { dbg('loadInboxKpiData error', err); }
}

export async function loadOutboxKpiData() {
  dbg('loadOutboxKpiData: Starting');
  try {
    const response = await fetch('/Dashboard/OutboxKpiData');
    const data = await response.json();
    dbg('loadOutboxKpiData: Received', data);
    const sentTodayCount = document.getElementById('sentTodayCount');
    const confirmedAlertsCount = document.getElementById('confirmedAlertsCount');
    const pendingConfirmationCount = document.getElementById('pendingConfirmationCount');
    if (sentTodayCount) sentTodayCount.textContent = data.sentToday || 0;
    if (confirmedAlertsCount) confirmedAlertsCount.textContent = data.confirmedAlerts || 0;
    if (pendingConfirmationCount) pendingConfirmationCount.textContent = data.pendingConfirmation || 0;
    rebindTabsAndKpis();
  } catch (err) { dbg('loadOutboxKpiData error', err); }
}

export async function updateUnreadBadge() {
  dbg('updateUnreadBadge: Starting');
  try {
    const response = await fetch('/Alerts/UnreadCount');
    const count = await response.json();
    const el = document.getElementById('unreadCount');
    if (el) el.textContent = count;
  } catch (e) { dbg('updateUnreadBadge error', e); }
}

export async function updateMandatoryConfirmedKpi(){
  try {
    const count = await fetchJson('/Alerts/ConfirmedMandatoryCount');
    const el = document.getElementById('mandatoryConfirmedCount');
    if (el){ el.textContent = count; }
  } catch {}
}

export async function updateMandatoryPendingKpi(){
  try {
    const count = await fetchJson('/Alerts/MandatoryPendingCount');
    const el = document.getElementById('mandatoryPendingCount');
    if (el){ el.textContent = count; }
  } catch {}
}

export async function updateTodayKpi(){
  try {
    const count = await fetchJson('/Alerts/TodayCount');
    const el = document.getElementById('todayCount');
    if (el){ el.textContent = count; }
  } catch {}
}

export async function updateSidebarCounts() {
  try {
    // Refresh only KPIs present on this view to avoid cross-over writes
    const ops = [];
    if (document.getElementById('receivedTodayCount')) ops.push(loadInboxKpiData().catch(()=>{}));
    if (document.getElementById('sentTodayCount')) ops.push(loadOutboxKpiData().catch(()=>{}));
    await Promise.allSettled(ops);
    dbg('updateSidebarCounts: Updated sidebar counts successfully');
  } catch (error) {
    logError('updateSidebarCounts: Failed to update sidebar counts', error);
  }
}

export function updatePagination(kind, page, total) {
  dbg('updatePagination called', { kind, page, total });
  const pageSize = 50;
  const start = (page-1)*pageSize + 1;
  const end = Math.min(total, page*pageSize);
  const container = document.getElementById(`${kind}Pagination`);
  if (!container) { dbg('updatePagination: container not found', { kind }); return; }
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
  dbg('updatePagination finished', { containerVisible: container.style.display, prevDisabled: prev?.disabled, nextDisabled: next?.disabled });
}

export function filterList(containerId, query){
  dbg('filterList called', { containerId, query });
  const container = document.getElementById(containerId); if (!container) { dbg('filterList: container not found', { containerId }); return; }
  const rows = container.querySelectorAll('.gmail-alert-row');
  rows.forEach(row=>{
    if (!query){ row.style.display = ''; return; }
    const text = row.innerText.toLowerCase();
    row.style.display = text.includes(query) ? '' : 'none';
  });
  dbg('filterList: completed for', { rows: rows.length });
}

export function showFinalStatusToast(message, type) {
  dbg('showFinalStatusToast called', { message, type });
  const toast = document.createElement('div');
  toast.className = `toast align-items-center text-white bg-${type}`;
  toast.setAttribute('role', 'alert');
  toast.setAttribute('aria-live', 'assertive');
  toast.setAttribute('aria-atomic', 'true');
  toast.style.cssText = 'position: fixed; top: 20px; right: 20px; z-index: 9999;';
  toast.innerHTML = `
    <div class=\"d-flex\">
      <div class=\"toast-body\">${message}</div>
      <button type=\"button\" class=\"btn-close btn-close-white me-2 m-auto\" data-bs-dismiss=\"toast\" aria-label=\"Close\"></button>
    </div>
  `;
  document.body.appendChild(toast);
  const bsToast = new bootstrap.Toast(toast, { delay: 3000 });
  bsToast.show();
  toast.addEventListener('hidden.bs.toast', () => {
    dbg('showFinalStatusToast: toast removed');
    toast.remove();
  });
}

// utilise renderInboxList / renderOutboxList importés où il faut (ici fallback)
export function renderList(containerId, items) {
  // Doit router selon id de conteneur comme dans legacy
  try {
    if (containerId === 'inboxList') {
      // lazy import pour éviter import croisé
      import('./inbox.js').then(mod => mod.renderInboxList(containerId, items));
    } else if (containerId === 'sentList') {
      import('./outbox.js').then(mod => mod.renderOutboxList(containerId, items));
    } else {
      import('./outbox.js').then(mod => mod.renderOutboxList(containerId, items));
    }
    dbg('renderList', {containerId, itemsCount: items?.length});
  } catch (e) { dbg('renderList error', e); }
}

export async function confirmAlert(alertId) {
  dbg('confirmAlert: called', { alertId });
  try {
    const response = await fetch(`/Dashboard/ConfirmAlert/${alertId}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
      },
    });
    const result = await response.json();
    dbg('confirmAlert: result', result);
    if (result.success) {
      window.showFinalStatusToast('Alerte confirmée avec succès', 'success');
      if (typeof window.loadInbox === 'function') window.loadInbox();
      if (typeof window.loadInboxKpiData === 'function') window.loadInboxKpiData();
      if (typeof window.updateSidebarCounts === 'function') window.updateSidebarCounts();
    } else {
      window.showFinalStatusToast('Echec lors de la confirmation', 'danger');
    }
    if(typeof window.refreshAllKpis==='function') window.refreshAllKpis(); // legacy parity
  } catch (e) {
    dbg('confirmAlert error', e);
    window.showFinalStatusToast('Erreur lors de la confirmation', 'danger');
    if(typeof window.refreshAllKpis==='function') window.refreshAllKpis(); // always fallback
  }
}

export async function markRead(alertId) {
  dbg('markRead: called', { alertId });
  const res = await confirmAlert(alertId);
  if(typeof window.refreshAllKpis==='function') window.refreshAllKpis();
  return res; // même endpoint et logique mutualisée
}

export function updateInboxCount(count) {
  const badge = document.querySelector('#inbox-badge');
  if (badge) badge.textContent = count;
  dbg('updateInboxCount', count);
}
export function updateOutboxCount(count) {
  const badge = document.querySelector('#outbox-badge');
  if (badge) badge.textContent = count;
  dbg('updateOutboxCount', count);
}

// Unique exports ONLY, no duplicate below
export function startUndoTimer() {
  dbg('startUndoTimer: (noop in modern UI)');
}
export function showSendingNotification() {
  dbg('showSendingNotification: (noop)');
}
export function hideSendingNotification() {
  dbg('hideSendingNotification: (noop)');
}
export function cancelSend() {
  dbg('cancelSend: (noop)');
}

export function createAlertElement(alertData, type) {
  const statusClass = alertData.status === 'Envoyé' ? 'success' : 'danger';
  const statusText = alertData.status === 'Envoyé' ? 'Envoyé' : 'Échoué';
  return `<div class="alert-item" data-alert-id="${alertData.id}"><div class="d-flex justify-content-between align-items-start"><div class="flex-grow-1"><h6 class="mb-1 fw-semibold">${alertData.title}</h6><p class="mb-1 text-muted small">${alertData.message}</p><div class="d-flex align-items-center gap-2"><span class="badge bg-${statusClass}">${statusText}</span><small class="text-muted">${alertData.date}</small></div></div><div class="dropdown"><button class="btn btn-sm btn-outline-secondary" type="button" data-bs-toggle="dropdown"><i class="bi bi-three-dots-vertical"></i></button><ul class="dropdown-menu"><li><a class="dropdown-item" href="#" onclick="viewAlertDetails(${alertData.id})"><i class="bi bi-eye me-2"></i>Voir détails</a></li><li><a class="dropdown-item" href="#" onclick="editAlert(${alertData.id})"><i class="bi bi-pencil me-2"></i>Modifier</a></li><li><hr class="dropdown-divider"></li><li><a class="dropdown-item text-danger" href="#" onclick="deleteAlert(${alertData.id})"><i class="bi bi-trash me-2"></i>Supprimer</a></li></ul></div></div></div>`;
}
export function updateInboxKpis(kpiData){}
export function updateOutboxKpis(kpiData){}
export function updateOutboxModal(data){}
export function addNewAlertToOutbox(alertData){}
export function addNewAlertToInbox(alertData){}

if(typeof window!=="undefined"){
  window.createAlertElement = createAlertElement;
  window.showSendingNotification = showSendingNotification;
  window.hideSendingNotification = hideSendingNotification;
  window.startUndoTimer = startUndoTimer;
  window.cancelSend = cancelSend;
  window.updateInboxKpis=updateInboxKpis;
  window.updateOutboxKpis=updateOutboxKpis;
  window.updateOutboxModal=updateOutboxModal;
  window.addNewAlertToOutbox=addNewAlertToOutbox;
  window.addNewAlertToInbox=addNewAlertToInbox;
  window.filterList = filterList;
  window.updatePagination = updatePagination;
  window.renderList = renderList;
  window.confirmAlert = confirmAlert;
  window.markRead = markRead;
  window.updateInboxCount = updateInboxCount;
  window.updateOutboxCount = updateOutboxCount;
  window.showDetailsModal = showDetailsModal;
  window.showFinalStatusToast = showFinalStatusToast;
}

// Add loadInboxDetails and loadOutboxDetails so dashboard import works
export async function loadInboxDetails(alertId) {
  try {
    const mod = await import('./inbox.js');
    if(typeof mod.loadInboxDetails === 'function') return mod.loadInboxDetails(alertId);
    dbg('ui.js: loadInboxDetails fallback (noop)');
  } catch(e){ dbg('ui.js: loadInboxDetails error', e); }
}
export async function loadOutboxDetails(alertId) {
  try {
    const mod = await import('./outbox.js');
    if(typeof mod.loadOutboxDetails === 'function') return mod.loadOutboxDetails(alertId);
    dbg('ui.js: loadOutboxDetails fallback (noop)');
  } catch(e){ dbg('ui.js: loadOutboxDetails error', e); }
}

export function showDetailsModal(details){
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
  // --- ACTION BUTTON LOGIC (shows only if unread)
  const actionBtnContainer = modalEl.querySelector('#detailsActionBtnContainer');
  if (actionBtnContainer) {
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
  // Show recipients (for sent alerts)
  const recipientsContainer = modalEl.querySelector('#recipientsContainer');
  if (recipientsContainer && details.recipients && details.recipients.length > 0) {
    recipientsContainer.style.display = 'block';
    const recipientsList = modalEl.querySelector('#recipientsList');
    if (recipientsList) {
      const requiresConfirmation = (details.alertTypeId === 2 || details.type === 'acquittementNecessaire' || details.type === 'acquittementNécessaire');
      recipientsList.innerHTML = details.recipients.map(recipient => {
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
