// Minimal bootstrap to load inbox/sent using existing endpoints in WEB controllers
let inboxPage = 1, sentPage = 1, pageSize = 50, inboxTotal = 0, sentTotal = 0;

// Simple frontend logging helper
const __ALERT_DEBUG = true;
function dbg(){ try { if (__ALERT_DEBUG && window.console){ console.debug.apply(console, arguments); } } catch{} }

document.addEventListener('DOMContentLoaded', function() {
  try { loadInbox(); } catch {}
  const sentTab = document.querySelector('[data-bs-target="#sent"]');
  if (sentTab) sentTab.addEventListener('click', () => { try { loadSent(); } catch {} });

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
  dbg('fetchJson:start', url);
  const r = await fetch(url, { cache:'no-store' });
  dbg('fetchJson:resp', { url, status:r.status });
  if(!r.ok) throw new Error('HTTP '+r.status);
  const j = await r.json();
  dbg('fetchJson:data', { url, data:j });
  return j;
}

function showLoading(containerId){ const c = document.getElementById(containerId); if(!c) return; c.innerHTML = '<div class="gmail-loading"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Chargement...</span></div><span class="ms-2">Chargement des alertes...</span></div>'; }

function renderList(containerId, items){
  const c = document.getElementById(containerId); if(!c) return;
  c.innerHTML = (items||[]).map(a=> {
    const preview = (a.message||'').trim();
    const dateText = new Date(a.createdAt||a.dateCreation).toLocaleDateString('fr-FR', { year:'numeric', month:'2-digit', day:'2-digit' });
    return `
    <div class="gmail-alert-row" data-id="${a.id}">
      <div class="row-left title-col">${(a.title||'Sans titre')}</div>
      <div class="row-main desc-col">${preview}</div>
      <div class="row-right date-col">${dateText}</div>
    </div>`;
  }).join('');
  // click -> details
  c.querySelectorAll('.gmail-alert-row').forEach(row=>{
    row.addEventListener('click', async ()=>{
      const id = row.getAttribute('data-id');
      try {
        // show loading state in details modal first
        showDetailsModal({ title: 'Chargement…', message: 'Veuillez patienter…' });
        const details = await fetchJson(`/Alerts/Details?id=${id}`);
        showDetailsModal(details);
      } catch (err){
        showDetailsModal({ title: 'Erreur', message: "Impossible de charger les détails de l'alerte." });
        console.error('Details load error', err);
      }
    });
  });
}

async function loadInbox(){
  showLoading('inboxList');
  const data = await fetchJson(`/Alerts/HistoryData?page=${inboxPage}&size=${pageSize}`);
  renderList('inboxList', data.items);
  inboxTotal = data.total ?? 0;
  updatePagination('inbox', inboxPage, inboxTotal);
  dbg('inbox:loaded', { page: inboxPage, total: inboxTotal });
}
async function loadSent(){
  showLoading('sentList');
  const data = await fetchJson(`/Alerts/SentData?page=${sentPage}&size=${pageSize}`);
  renderList('sentList', data.items);
  sentTotal = data.total ?? 0;
  updatePagination('sent', sentPage, sentTotal);
  dbg('sent:loaded', { page: sentPage, total: sentTotal });
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
  if (window.bootstrap && window.bootstrap.Modal){ window.bootstrap.Modal.getOrCreateInstance(modalEl).show(); }
}

// Compose send (stub)
const sendBtn = document.getElementById('composeSendBtn');
if (sendBtn){
  sendBtn.addEventListener('click', async ()=>{
    const title = document.getElementById('composeTitle')?.value || '';
    const message = document.getElementById('composeMessage')?.value || '';
    const emails = getTagValues('destEmails');
    const phones = getTagValues('destPhones');
    const platforms = {
      Email: document.getElementById('platformEmail')?.checked || false,
      WhatsApp: document.getElementById('platformWhatsApp')?.checked || false,
      Desktop: document.getElementById('platformDesktop')?.checked || false
    };
    try {
      console.groupCollapsed('Send:compose');
      dbg('send:payload', { title, message, emails, phones, platforms });
      const r = await fetch('/AlertsCrud/Send', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ title, message, emails, phones, platforms }) });
      dbg('send:responseStatus', r.status);
      if (!r.ok) throw new Error('HTTP '+r.status);
      const j = await r.json();
      dbg('send:responseBody', j);
      // Fermer le modal et recharger Inbox pour voir la nouvelle alerte
      const modalEl = document.getElementById('newAlertModal');
      if (modalEl && window.bootstrap?.Modal){ window.bootstrap.Modal.getOrCreateInstance(modalEl).hide(); }
      inboxPage = 1; await loadInbox();
      console.groupEnd();
    } catch(err){
      console.error('CreateFromTemplate failed', err);
      console.groupEnd();
      alert("Échec de création d'alerte");
    }
  });
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


