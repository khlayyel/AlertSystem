import { dbg } from './helpers.js';

export function showDetailsModal(details){
  const modalEl = document.getElementById('alertDetailsModal');
  if (!modalEl) return;
  const title = details.title || 'Titre non disponible';
  const msg = details.message || 'Message non disponible';
  let createdAt = '';
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
  const titleEl = modalEl.querySelector('#detailTitle'); if (titleEl) titleEl.textContent = title;
  const msgEl = modalEl.querySelector('#detailMessage'); if (msgEl) msgEl.textContent = msg;
  const typeEl = modalEl.querySelector('#detailType');
  if (typeEl) {
    const tRaw = (details.type||'').toString();
    const tId = details.alertTypeId;
    const isOblig = (tId === 2) || /acquittementn[ée]cessaire/i.test(tRaw) || /obligatoire/i.test(tRaw);
    typeEl.textContent = isOblig ? 'Obligatoire' : 'Information';
    typeEl.className = 'badge fs-6 ' + (isOblig ? 'bg-danger' : 'bg-info');
  }
  const statusEl = modalEl.querySelector('#detailStatus');
  if (statusEl) {
    // Use badge & class from modalStatus if passed (from outbox)
    if (details.modalStatus && details.modalStatus.badge) {
      statusEl.textContent = details.modalStatus.badge;
      statusEl.className = 'badge fs-6 ' + (details.modalStatus.class || 'bg-light text-dark');
    } else {
      const s = (details.status || '').toString().toLowerCase();
      statusEl.textContent = details.status || 'Statut non spécifié';
      statusEl.className = 'badge fs-6 ' + (s.includes('envoy') ? 'bg-success' : s.includes('échou') || s.includes('echec') ? 'bg-danger' : s.includes('cours') ? 'bg-warning' : 'bg-secondary');
    }
  }
  const createdEl = modalEl.querySelector('#detailCreatedAt'); if (createdEl) createdEl.textContent = createdAt;
  const actionBtnContainer = modalEl.querySelector('#detailsActionBtnContainer');
  if (actionBtnContainer) {
    const stateId = details.stateId ?? details.etatAlerteId ?? details.readStateId;
    const alertTypeId = details.alertTypeId ?? details.typeId;
    const theId = details.id ?? details.Id ?? details.alertId ?? details.AlertId;
    if (stateId === 1) {
      if (alertTypeId === 2 || details.type === 'acquittementNecessaire' || details.type === 'acquittementNécessaire') {
        actionBtnContainer.innerHTML = `
          <button class="btn btn-success btn-lg" id="modalConfirmBtn" type="button">
            <i class="bi bi-check2-circle me-1"></i>Confirmer
          </button>`;
      } else {
        actionBtnContainer.innerHTML = `
          <button class="btn btn-secondary btn-lg" id="modalReadBtn" type="button">
            <i class="bi bi-eye me-1"></i>Marquer comme lu
          </button>`;
      }
      // --- DYNAMIQUE: patch pour que le bouton soit remplacé dynamiquement après succès ---
      const confirmBtn = modalEl.querySelector('#modalConfirmBtn');
      if (confirmBtn) confirmBtn.addEventListener('click', async e => {
        confirmBtn.disabled = true;
        try {
          await window.confirmAlert(theId); // Promise
          setTimeout(() => { actionBtnContainer.innerHTML = `<span class="text-success fw-semibold">Déjà confirmé ou lu</span>`; }, 350);
        } catch { confirmBtn.disabled = false; }
      });
      const readBtn = modalEl.querySelector('#modalReadBtn');
      if (readBtn) readBtn.addEventListener('click', async e => {
        readBtn.disabled = true;
        try {
          await window.markRead(theId);
          setTimeout(() => { actionBtnContainer.innerHTML = `<span class="text-success fw-semibold">Déjà confirmé ou lu</span>`; }, 350);
        } catch { readBtn.disabled = false; }
      });
    } else {
      actionBtnContainer.innerHTML = `<span class="text-success fw-semibold">Déjà confirmé ou lu</span>`;
    }
  }
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

// Accessibility: keep hidden modals inert to avoid focusable elements inside aria-hidden containers
export function setupModalA11y(){
  const ids = ['newAlertModal','alertDetailsModal'];
  ids.forEach(id => {
    const el = document.getElementById(id);
    if (!el) return;
    const onShow = ()=>{ try { el.removeAttribute('inert'); el.setAttribute('aria-hidden','false'); } catch(_){} };
    const onHide = ()=>{ try { el.setAttribute('inert',''); el.setAttribute('aria-hidden','true'); } catch(_){} };
    el.addEventListener('show.bs.modal', onShow);
    el.addEventListener('shown.bs.modal', onShow);
    el.addEventListener('hide.bs.modal', onHide);
    el.addEventListener('hidden.bs.modal', onHide);
    // Initialize state (closed by default)
    if (el.classList.contains('show')) { onShow(); } else { onHide(); }
  });
}

if (typeof window !== "undefined") {
  window.showDetailsModal = showDetailsModal;
  window.setupModalA11y = setupModalA11y;
  window.reloadInbox = (window.reloadInbox || (window.loadInbox ? window.loadInbox.bind(window) : null));
}
