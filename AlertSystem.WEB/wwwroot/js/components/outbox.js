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
      setTimeout(()=>{ if(window.refreshAllKpis) window.refreshAllKpis(); },300);
    });
  });
}
