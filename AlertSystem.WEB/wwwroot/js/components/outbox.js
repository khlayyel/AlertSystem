import { dbg, fetchJson } from '../helpers.js';
import { showDetailsModal } from '../modals.js';

export function renderOutboxList(containerId, items) {
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
  // Table-like header same as inbox
  c.innerHTML = `<div class="gmail-inbox-header-row d-flex align-items-center fw-bold bg-light border-bottom" style="min-height:44px;">
    <div style="min-width:210px;" class="ps-3 flex-shrink-0">Titre Alerte</div>
    <div class="flex-grow-1 ps-2 pe-2">Description</div>
    <div style="width:130px;" class="text-center flex-shrink-0">Statut</div>
    <div style="min-width:160px;" class="text-end pe-3 flex-shrink-0">Date</div>
  </div>`;
  c.innerHTML += list.map(a=> {
    const preview = (a.message||'').trim();
    const dRaw = a.createdAt || a.dateCreation || a.date || a.DateCreation;
    let dateText = '';
    try {
      const d = new Date(dRaw);
      dateText = isNaN(d.getTime()) ? '' : d.toLocaleString('fr-FR', { year:'numeric', month:'2-digit', day:'2-digit', hour:'2-digit', minute:'2-digit' });
    } catch { dateText = ''; }
    let statusBadge = '';
    let statusClass = '';
    const status = a.status || a.statutId || a.statusId;
    // Harmonize logic with modal/line: match inbox logic for status
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
    return `<div class="gmail-alert-row d-flex align-items-center" data-id="${a.id}" style="min-height:56px; border-bottom:1px solid #f1f1f1;">
      <div class="row-left title-col flex-shrink-0 ps-3" style="min-width:210px;">
        <div class="fw-semibold">${(a.title||'Sans titre')}</div>
      </div>
      <div class="row-main desc-col flex-grow-1 ps-2 pe-2">${preview}</div>
      <div class="row-status flex-shrink-0 text-center align-self-stretch d-flex align-items-center justify-content-center" style="width:130px;">
        ${statusBadge ? `<span class="badge ${statusClass} px-3 py-2 fs-6">${statusBadge}</span>` : ''}
      </div>
      <div class="row-right date-col flex-shrink-0 text-end pe-3" style="min-width:160px;">
        <span class="text-muted small">${dateText}</span>
      </div>
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
        showDetailsModal({ title: 'Chargement…', message: 'Veuillez patienter…' });
        // For sent alerts, show recipients/plus propagate correct status details
        const recipientsData = await fetchJson(`/Dashboard/AlertRecipients/${id}`);
        const alertDetails = await fetchJson(`/Alerts/Details?id=${id}`);
        // Patch: Propagate status badge/class as in line to detail modal
        let statusBadge = '';
        let statusClass = '';
        const status = alertDetails.status || alertDetails.statutId || alertDetails.statusId;
        if (typeof status === 'string') {
          switch((status||'').toLowerCase()) {
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
        const combinedDetails = {
          ...alertDetails,
          recipients: recipientsData.recipients || [],
          modalStatus: {
            badge: statusBadge,
            class: statusClass
          }
        };
        showDetailsModal(combinedDetails);
      } catch (err){
        showDetailsModal({ title: 'Erreur', message: "Impossible de charger les détails de l'alerte." });
        console.error('Details load error', err);
      }
      setTimeout(()=>{ if(window.refreshAllKpis) window.refreshAllKpis(); },300);
    });
  });
}
