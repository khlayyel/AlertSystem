// Gmail-Style Dashboard Implementation
console.log('🚀 Gmail-Style Dashboard Loading...');

// Global state
let currentPage = { inbox: 1, sent: 1 };
let totalPages = { inbox: 1, sent: 1 };
let alertCancellationTimer = null;
let pendingAlertId = null;

// Utility functions
const DLOG = (scope, msg, obj) => { try { console.debug(`[${scope}]`, msg, obj ?? ''); } catch {} };

async function fetchJson(url) {
  console.log('[DEBUG] fetchJson called with URL:', url);
  try {
    const r = await fetch(url, { cache: 'no-store' });
    console.log('[DEBUG] fetchJson response status:', r.status, r.statusText);
    if (!r.ok) throw new Error(`HTTP ${r.status}`);
    const data = await r.json();
    console.log('[DEBUG] fetchJson success, data:', data);
    return data;
  } catch (error) {
    console.error('[DEBUG] fetchJson error for URL:', url, 'Error:', error);
    throw error;
  }
}

function formatDate(dateIso) {
  const date = new Date(dateIso);
  const now = new Date();
  const diffDays = Math.floor((now - date) / (1000 * 60 * 60 * 24));
  
  if (diffDays === 0) {
    return date.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
  } else if (diffDays < 7) {
    return date.toLocaleDateString('fr-FR', { weekday: 'short' });
  } else {
    return date.toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit' });
  }
}

function truncateText(text, maxLength = 100) {
  if (!text) return '';
  return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
}

// UI Helper Functions
function showLoading(containerId) {
  const container = document.getElementById(containerId);
  if (container) {
    container.innerHTML = `
      <div class="gmail-loading">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Chargement...</span>
        </div>
        <span class="ms-2">Chargement des alertes...</span>
      </div>
    `;
  }
}

function showError(containerId, message) {
  const container = document.getElementById(containerId);
  if (container) {
    container.innerHTML = `
      <div class="text-center p-4">
        <i class="bi bi-exclamation-triangle text-warning" style="font-size: 2rem;"></i>
        <p class="mt-2 text-muted">${message}</p>
        <button class="btn btn-outline-primary btn-sm" onclick="location.reload()">
          <i class="bi bi-arrow-clockwise me-1"></i>Réessayer
        </button>
      </div>
    `;
  }
}

function showEmptyState(emptyStateId) {
  const emptyState = document.getElementById(emptyStateId);
  if (emptyState) {
    emptyState.style.display = 'block';
  }
}

function hideEmptyState(emptyStateId) {
  const emptyState = document.getElementById(emptyStateId);
  if (emptyState) {
    emptyState.style.display = 'none';
  }
}

function hidePagination(paginationId) {
  const pagination = document.getElementById(paginationId);
  if (pagination) {
    pagination.style.display = 'none';
  }
}

// Gmail-style alert rendering
function renderGmailList(containerId, alerts, type) {
  const container = document.getElementById(containerId);
  if (!container || !alerts || alerts.length === 0) return;

  const alertRows = alerts.map(alert => {
    const isUnread = type === 'inbox' && alert.alertType === 'Obligatoire' && !alert.isConfirmed;
    const title = alert.title || 'Sans titre';
    const preview = truncateText(alert.description || alert.message || '');
    const date = formatDate(alert.createdAt || alert.dateCreation);
    const sender = alert.senderEmail || alert.senderName || 'Système';
    
    let typeIcon = '';
    let typeBadge = '';
    if (alert.alertType === 'Obligatoire') {
      typeIcon = '<i class="bi bi-exclamation-triangle text-warning"></i>';
      typeBadge = '<span class="badge bg-warning text-dark alert-type-badge">Obligatoire</span>';
    } else {
      typeIcon = '<i class="bi bi-info-circle text-info"></i>';
      typeBadge = '<span class="badge bg-info alert-type-badge">Information</span>';
    }

    return `
      <div class="gmail-alert-row ${isUnread ? 'unread' : ''}" data-alert-id="${alert.alertId || alert.id}" onclick="openAlertDetail(${alert.alertId || alert.id})">
        <div class="alert-checkbox">
          <input type="checkbox" class="form-check-input" onclick="event.stopPropagation()">
        </div>
        <div class="alert-status">
          ${typeIcon}
        </div>
        <div class="alert-title">
          ${title}
        </div>
        <div class="alert-preview">
          ${type === 'sent' ? `À: ${alert.recipientEmails || 'Destinataires'} - ` : `De: ${sender} - `}${preview}
          ${typeBadge}
        </div>
        <div class="alert-date">
          ${date}
        </div>
      </div>
    `;
  }).join('');

  container.innerHTML = alertRows;
}

// Pagination functions
function updatePagination(type, currentPage, total) {
  const pageSize = 50;
  const totalPages = Math.ceil(total / pageSize);
  const start = (currentPage - 1) * pageSize + 1;
  const end = Math.min(currentPage * pageSize, total);

  // Update pagination info
  document.getElementById(`${type}RangeStart`).textContent = start;
  document.getElementById(`${type}RangeEnd`).textContent = end;
  document.getElementById(`${type}Total`).textContent = total;

  // Update pagination controls
  const prevBtn = document.getElementById(`${type}PrevBtn`);
  const nextBtn = document.getElementById(`${type}NextBtn`);
  const pagination = document.getElementById(`${type}Pagination`);

  if (prevBtn) prevBtn.disabled = currentPage <= 1;
  if (nextBtn) nextBtn.disabled = currentPage >= totalPages;
  if (pagination) pagination.style.display = total > 0 ? 'block' : 'none';

  // Store total pages
  totalPages[type] = totalPages;
}

// Gmail-style alert loading with pagination
async function loadInbox(page = 1) {
  console.log('[DEBUG] loadInbox start, page:', page);
  try {
    showLoading('inboxList');
    const data = await fetchJson(`/Alerts/HistoryData?status=all&page=${page}&size=50`);
    console.log('[DEBUG] loadInbox result:', { total: data?.total, items: data?.items?.length });
    
    if (data?.items?.length > 0) {
      renderGmailList('inboxList', data.items, 'inbox');
      updatePagination('inbox', page, data.total);
      hideEmptyState('inboxEmpty');
    } else {
      showEmptyState('inboxEmpty');
      hidePagination('inboxPagination');
    }
    
    updateKpisFromData(data);
    updateUnreadKpi();
    currentPage.inbox = page;
  } catch (error) {
    console.error('[DEBUG] loadInbox error:', error);
    showError('inboxList', 'Erreur lors du chargement des alertes');
  }
}

async function loadSent(page = 1) {
  console.log('[DEBUG] loadSent start, page:', page);
  try {
    showLoading('sentList');
    const data = await fetchJson(`/Alerts/SentData?page=${page}&size=50`);
    console.log('[DEBUG] loadSent result:', { total: data?.total, items: data?.items?.length });
    
    if (data?.items?.length > 0) {
      renderGmailList('sentList', data.items, 'sent');
      updatePagination('sent', page, data.total);
      hideEmptyState('sentEmpty');
    } else {
      showEmptyState('sentEmpty');
      hidePagination('sentPagination');
    }
    
    currentPage.sent = page;
  } catch (error) {
    console.error('[DEBUG] loadSent error:', error);
    showError('sentList', 'Erreur lors du chargement des alertes envoyées');
  }
}

// Alert detail modal
async function openAlertDetail(alertId) {
  try {
    console.log('[DEBUG] Opening alert detail for ID:', alertId);
    
    // Show loading in modal
    document.getElementById('detailTitle').textContent = 'Chargement...';
    document.getElementById('detailContent').textContent = 'Chargement des détails...';
    
    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('alertDetailModal'));
    modal.show();
    
    // Fetch alert details (you'll need to implement this endpoint)
    const alert = await fetchJson(`/Alerts/Details/${alertId}`);
    
    // Populate modal
    document.getElementById('detailTitle').textContent = alert.title || 'Sans titre';
    document.getElementById('detailSender').innerHTML = `<i class="bi bi-person me-1"></i>${alert.senderEmail || alert.senderName || 'Système'}`;
    document.getElementById('detailDate').innerHTML = `<i class="bi bi-calendar me-1"></i>${new Date(alert.createdAt || alert.dateCreation).toLocaleString('fr-FR')}`;
    document.getElementById('detailType').innerHTML = `<i class="bi bi-tag me-1"></i>${alert.alertType || 'Information'}`;
    document.getElementById('detailStatus').innerHTML = `<i class="bi bi-check-circle me-1"></i>${alert.isConfirmed ? 'Lu' : 'Non lu'}`;
    document.getElementById('detailContent').textContent = alert.description || alert.message || 'Aucun contenu';
    
    // Show/hide mark as read button
    const markAsReadBtn = document.getElementById('markAsReadBtn');
    if (alert.alertType === 'Obligatoire' && !alert.isConfirmed) {
      markAsReadBtn.style.display = 'inline-block';
      markAsReadBtn.onclick = () => markAsRead(alert.alertRecipientId);
    } else {
      markAsReadBtn.style.display = 'none';
    }
    
  } catch (error) {
    console.error('[DEBUG] Error opening alert detail:', error);
    document.getElementById('detailTitle').textContent = 'Erreur';
    document.getElementById('detailContent').textContent = 'Impossible de charger les détails de l\'alerte.';
  }
}

// Alert cancellation system (Gmail-style)
function showCancellationToast(alertId) {
  pendingAlertId = alertId;
  const toast = document.getElementById('alertCancellationToast');
  const message = document.querySelector('.toast-message');
  
  message.textContent = 'Envoi en cours...';
  toast.style.display = 'block';
  
  // Start 10-second countdown
  let countdown = 10;
  const countdownInterval = setInterval(() => {
    countdown--;
    message.textContent = `Envoi en cours... (${countdown}s pour annuler)`;
    
    if (countdown <= 0) {
      clearInterval(countdownInterval);
      hideCancellationToast(true); // Success
    }
  }, 1000);
  
  // Store the interval for potential cancellation
  alertCancellationTimer = countdownInterval;
}

function hideCancellationToast(success = false) {
  const toast = document.getElementById('alertCancellationToast');
  const message = document.querySelector('.toast-message');
  
  if (success) {
    message.textContent = 'Alerte envoyée avec succès!';
    setTimeout(() => {
      toast.style.display = 'none';
      loadSent(); // Refresh sent list
      loadInbox(); // Refresh inbox if needed
    }, 2000);
  } else {
    toast.style.display = 'none';
  }
  
  if (alertCancellationTimer) {
    clearInterval(alertCancellationTimer);
    alertCancellationTimer = null;
  }
  pendingAlertId = null;
}

async function cancelAlertSend() {
  if (!pendingAlertId) return;
  
  try {
    // Call cancellation API (you'll need to implement this)
    await fetchJson(`/Alerts/Cancel/${pendingAlertId}`, { method: 'POST' });
    
    const message = document.querySelector('.toast-message');
    message.textContent = 'Envoi d\'alerte annulé avec succès';
    
    setTimeout(() => {
      hideCancellationToast(false);
    }, 2000);
    
  } catch (error) {
    console.error('[DEBUG] Error cancelling alert:', error);
    const message = document.querySelector('.toast-message');
    message.textContent = 'Erreur lors de l\'annulation';
  }
}

// KPI and utility functions (keeping existing ones)
async function updateUnreadKpi() {
  try {
    const response = await fetch('/Alerts/UnreadCount');
    console.debug('[DEBUG] updateUnreadKpi response:', response.status, response.statusText);
    const count = await response.json();
    console.debug('[DEBUG] updateUnreadKpi data:', count);
    
    const kpiElement = document.getElementById('kpiPending');
    if (kpiElement) {
      kpiElement.textContent = count;
      console.debug('[DEBUG] updateUnreadKpi set kpiPending to:', count);
    }
    
    // Update unread badge in tab
    const unreadBadge = document.getElementById('unreadCount');
    if (unreadBadge && count > 0) {
      unreadBadge.textContent = count;
      unreadBadge.style.display = 'inline-block';
    } else if (unreadBadge) {
      unreadBadge.style.display = 'none';
    }
  } catch (error) {
    console.error('[DEBUG] updateUnreadKpi error:', error);
  }
}

async function updateConfirmedMandatoryKpi() {
  try {
    const response = await fetch('/Alerts/ConfirmedMandatoryCount');
    console.debug('[DEBUG] updateConfirmedMandatoryKpi response:', response.status, response.statusText);
    const count = await response.json();
    console.debug('[DEBUG] updateConfirmedMandatoryKpi data:', count);
    
    const kpiElement = document.getElementById('kpiConfirmedMandatory');
    if (kpiElement) {
      kpiElement.textContent = count;
      console.debug('[DEBUG] updateConfirmedMandatoryKpi set to:', count);
    }
  } catch (error) {
    console.error('[DEBUG] updateConfirmedMandatoryKpi error:', error);
  }
}

async function updateTodayKpi() {
  try {
    const response = await fetch('/Alerts/TodayCount');
    console.debug('[DEBUG] updateTodayKpi response:', response.status, response.statusText);
    const count = await response.json();
    console.debug('[DEBUG] updateTodayKpi data:', count);
    
    const kpiElement = document.getElementById('kpiToday');
    if (kpiElement) {
      kpiElement.textContent = count;
      console.debug('[DEBUG] updateTodayKpi set to:', count);
    }
  } catch (error) {
    console.error('[DEBUG] updateTodayKpi error:', error);
  }
}

function updateKpisFromData(data) {
  // This function can be enhanced based on your data structure
  console.debug('[DEBUG] updateKpisFromData called with:', data);
}

async function markAsRead(alertRecipientId) {
  try {
    const response = await fetch(`/Alerts/MarkAsRead/${alertRecipientId}`, { method: 'POST' });
    if (response.ok) {
      console.log('[DEBUG] Alert marked as read successfully');
      
      // Close modal
      const modal = bootstrap.Modal.getInstance(document.getElementById('alertDetailModal'));
      if (modal) modal.hide();
      
      // Refresh lists
      loadInbox(currentPage.inbox);
      updateUnreadKpi();
      updateConfirmedMandatoryKpi();
      
      // Show success message
      showToast('Alerte marquée comme lue', 'success');
    }
  } catch (error) {
    console.error('[DEBUG] Error marking alert as read:', error);
    showToast('Erreur lors de la mise à jour', 'error');
  }
}

function showToast(message, type = 'info') {
  // Simple toast implementation
  const toast = document.createElement('div');
  toast.className = `alert alert-${type === 'success' ? 'success' : type === 'error' ? 'danger' : 'info'} position-fixed`;
  toast.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
  toast.textContent = message;
  
  document.body.appendChild(toast);
  
  setTimeout(() => {
    toast.remove();
  }, 3000);
}

// Event listeners
document.addEventListener('DOMContentLoaded', function() {
  console.log('🎯 Gmail Dashboard Initialized');
  
  // Load initial data
  loadInbox();
  updateUnreadKpi();
  updateConfirmedMandatoryKpi();
  updateTodayKpi();
  
  // Tab switching
  document.querySelector('[data-bs-target="#sent"]').addEventListener('click', function() {
    if (currentPage.sent === 1) {
      loadSent();
    }
  });
  
  // Pagination event listeners
  document.getElementById('inboxPrevBtn')?.addEventListener('click', () => {
    if (currentPage.inbox > 1) {
      loadInbox(currentPage.inbox - 1);
    }
  });
  
  document.getElementById('inboxNextBtn')?.addEventListener('click', () => {
    loadInbox(currentPage.inbox + 1);
  });
  
  document.getElementById('sentPrevBtn')?.addEventListener('click', () => {
    if (currentPage.sent > 1) {
      loadSent(currentPage.sent - 1);
    }
  });
  
  document.getElementById('sentNextBtn')?.addEventListener('click', () => {
    loadSent(currentPage.sent + 1);
  });
  
  // Cancel send button
  document.getElementById('cancelSendBtn')?.addEventListener('click', cancelAlertSend);
  
  // Enhanced modal send button with cancellation
  const modalSendBtn = document.getElementById('modalSendBtn');
  if (modalSendBtn) {
    modalSendBtn.addEventListener('click', async function() {
      // Get form data (keeping existing logic)
      const title = document.getElementById('modalTitle')?.value?.trim();
      const message = document.getElementById('modalMessage')?.value?.trim();
      const alertType = document.getElementById('modalAlertType')?.value;
      
      if (!title || !message) {
        showToast('Titre et message requis', 'error');
        return;
      }
      
      try {
        // Send alert (keeping existing API call logic)
        const formData = new FormData();
        formData.append('title', title);
        formData.append('message', message);
        formData.append('alertType', alertType);
        
        const response = await fetch('/AlertsCrud/Create', { method: 'POST', body: formData });
        
        if (response.ok) {
          const result = await response.json();
          
          // Close modal
          const modal = bootstrap.Modal.getInstance(document.getElementById('newAlertModal'));
          if (modal) modal.hide();
          
          // Show cancellation toast
          showCancellationToast(result.alertId);
          
          // Clear form
          document.getElementById('modalTitle').value = '';
          document.getElementById('modalMessage').value = '';
        } else {
          showToast('Erreur lors de l\'envoi', 'error');
        }
      } catch (error) {
        console.error('[DEBUG] Error sending alert:', error);
        showToast('Erreur réseau', 'error');
      }
    });
  }
});

// SignalR connection (keeping existing)
const connection = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/notifications')
  .build();

connection.start().then(function () {
  console.log('[SignalR] connected');
}).catch(function (err) {
  console.error('[SignalR] error:', err.toString());
});

connection.on('AlertCreated', function (alertData) {
  console.log('[SignalR] AlertCreated received:', alertData);
  loadInbox(currentPage.inbox);
  updateUnreadKpi();
  updateTodayKpi();
});

connection.on('AlertConfirmed', function (alertData) {
  console.log('[SignalR] AlertConfirmed received:', alertData);
  loadInbox(currentPage.inbox);
  updateUnreadKpi();
  updateConfirmedMandatoryKpi();
});
