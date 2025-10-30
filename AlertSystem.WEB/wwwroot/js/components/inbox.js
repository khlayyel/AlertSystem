import { dbg, fetchJson } from '../helpers.js';
import { showDetailsModal } from '../modals.js';

export function renderInboxList(containerId, items) {
  const c = document.getElementById(containerId); if (!c) return;
  c.innerHTML = `
    <div class="gmail-inbox-header-row d-flex align-items-center fw-bold bg-light border-bottom" style="min-height:44px;">
      <div style="min-width:210px;" class="ps-3 flex-shrink-0">Titre Alerte</div>
      <div class="flex-grow-1 ps-2 pe-2">Description</div>
      <div style="width:130px;" class="text-center flex-shrink-0">Statut</div>
      <div style="min-width:160px;" class="text-end pe-3 flex-shrink-0">Date</div>
    </div>`;
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
  c.querySelectorAll('.gmail-alert-row').forEach(row => {
    row.addEventListener('click', async () => {
      const id = row.getAttribute('data-id');
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
      setTimeout(()=>{ if(window.refreshAllKpis) window.refreshAllKpis(); },300);
    });
  });
}

// Export for external refresh
export function reloadInbox(containerId = 'inboxList') {
  if (window.loadInbox) window.loadInbox();
}
