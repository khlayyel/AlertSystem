import { dbg, logError, logSuccess, fetchJson } from '../helpers.js';
import { showFinalStatusToast } from './ui.js';
import { addTagTo, getTagValues } from './tags.js';
import { splitPhones } from '../helpers.js';
import { usersState, ensureUsersLoaded } from '../usersState.js';

export function loadQuickTemplates() {
  dbg('loadQuickTemplates: called');
  const sel = document.getElementById('quickTemplate');
  if (!sel) { dbg('loadQuickTemplates: quickTemplate element not found'); return; }
  fetchJson('/AlertsCrud/QuickList').then(items => {
    dbg('loadQuickTemplates: items fetched', items);
    sel.innerHTML = '<option value="">-- Sélectionner --</option>' +
      (items||[]).map(x=>`<option value="${x.id}" data-title="${encodeURIComponent(x.title||'')}" data-message="${encodeURIComponent(x.message||'')}" data-type="${encodeURIComponent(x.type||'Information')}">${x.title||'Sans titre'}</option>`).join('');
  });
  sel.addEventListener('change', () => {
    dbg('loadQuickTemplates: template changed');
    const opt = sel.options[sel.selectedIndex];
    if (!opt || !opt.value) return;
    const title = decodeURIComponent(opt.getAttribute('data-title')||'');
    const message = decodeURIComponent(opt.getAttribute('data-message')||'');
    const type = decodeURIComponent(opt.getAttribute('data-type')||'Information');
    const t = document.getElementById('composeTitle'); if (t) t.value = title;
    const m = document.getElementById('composeMessage'); if (m) m.value = message;
    const ty = document.getElementById('composeType'); if (ty) ty.value = type;
  });
}

// Use global users state for all user list/functions
function getGlobalUsers(tab) {
  return tab === 'def' ? usersState.usersDefList : usersState.usersGrhList;
}
export async function loadUsers() {
  dbg('loadUsers: Starting');
  await ensureUsersLoaded(); // Always fill usersState if needed
  let usersDefList = usersState.usersDefList || [], usersGrhList = usersState.usersGrhList || [];
  // Persist active tab across reloads to avoid snapping back to "Utilisateurs"
  let usersActiveTab = (window.usersState && window.usersState.activeTab) ? window.usersState.activeTab : 'def';
  const tableBody = document.getElementById('usersTableBody');
  const searchBox = document.getElementById('userSearch');
  if (!tableBody || !searchBox) { dbg('loadUsers: tableBody or searchBox not found'); return; }
  const container = tableBody.closest('.table-responsive');
  if (container) { container.style.maxHeight = '320px'; container.style.overflowY = 'auto'; }
  let tabs = document.getElementById('userTabs');
  if (!tabs && searchBox) {
    tabs = document.createElement('div');
    tabs.id = 'userTabs';
    tabs.className = 'btn-group mb-2';
    tabs.innerHTML = `<button type="button" id="userTabDef" class="btn btn-outline-secondary active">Utilisateurs</button><button type="button" id="userTabGrh" class="btn btn-outline-secondary">Employés GRH</button>`;
    searchBox.parentElement?.insertAdjacentElement('afterend', tabs);
    const tabDef = tabs.querySelector('#userTabDef');
    const tabGrh = tabs.querySelector('#userTabGrh');
    const setActive = (tab)=>{
      usersActiveTab = tab;
      if (window.usersState) window.usersState.activeTab = tab;
      tabs.querySelectorAll('button').forEach(b=>b.classList.remove('active'));
      (tab === 'def' ? tabDef : tabGrh)?.classList.add('active');
      renderRows();
    };
    tabDef?.addEventListener('click', ()=> setActive('def'));
    tabGrh?.addEventListener('click', ()=> setActive('grh'));
  }
  const renderRows = ()=>{
    const q = (searchBox.value||'').toLowerCase();
    const emailOn = document.getElementById('platformEmail')?.checked || false;
    const waOn = document.getElementById('platformWhatsApp')?.checked || false;
    const colEmail = document.getElementById('colEmail');
    const colWa = document.getElementById('colWa');
    if (colEmail) colEmail.style.display = emailOn ? '' : 'none';
    if (colWa) colWa.style.display = waOn ? '' : 'none';
    // Always resolve active tab at call-time to avoid stale closures
    const activeTab = (window.usersState && window.usersState.activeTab) ? window.usersState.activeTab : usersActiveTab;
    const source = (activeTab === 'def') ? usersDefList : usersGrhList;
    const filtered = source.filter(u => {
      const name = (u.name||'').toLowerCase();
      const email = (u.email||'').toLowerCase();
      // For GRH we want search to match raw and normalized phones
      const phoneRaw = (u.phoneNumber||'');
      const phone = phoneRaw.toLowerCase();
      const phones = (window.splitPhones ? window.splitPhones(phoneRaw) : []);
      const phoneMatch = phone.includes(q) || phones.some(p=>p.replace('+','').includes(q.replace('+','')));
      return name.includes(q) || email.includes(q) || phoneMatch;
    });
    dbg('renderRows', { tab: activeTab, total: source.length, filtered: filtered.length, emailOn, waOn });
    if (filtered.length === 0) {
      tableBody.innerHTML = '<tr><td colspan="4" class="text-muted text-center">Aucun résultat</td></tr>';
      return;
    }
    tableBody.innerHTML = filtered.map(u => {
      const emailCell = emailOn ? (u.email || '<span class="text-muted">—</span>') : '';
      const phones = splitPhones(u.phoneNumber);
      const phoneFirst = phones.length ? phones[0] : null;
      const phoneCell = waOn ? (phoneFirst ? phoneFirst : '<span class="text-muted">—</span>') : '';
      return `<tr><td class="fw-semibold">${u.name || ('ID ' + u.id)}</td>${emailOn ? `<td>${emailCell}</td>` : ''}${waOn ? `<td>${phoneCell}</td>` : ''}<td class="text-end"><button type="button" class="btn btn-sm btn-outline-primary user-add" data-id="${u.id}">+</button></td></tr>`;
    }).join('');
    tableBody.querySelectorAll('.user-add').forEach(btn => {
      btn.addEventListener('click', () => {
        const id = btn.getAttribute('data-id');
        const currentTab = (window.usersState && window.usersState.activeTab) ? window.usersState.activeTab : usersActiveTab;
        const list = currentTab === 'def' ? usersDefList : usersGrhList;
        const user = list.find(x => (x.id ?? x.userId).toString() === id);
        if (!user) return;
        // Always add to Users field with name; resolve util_id if possible
        const tryResolveId = () => {
          const raw = (user.userId ?? user.id);
          let n = raw !== undefined && raw !== null ? parseInt(raw, 10) : NaN;
          if (!isNaN(n) && n > 0 && n <= 9999) return n;
          const norm = (s)=> (s||'').toString().trim().toLowerCase().replace(/\s+/g,' ');
          const uName = norm(user.name);
          const defList = (Array.isArray(window.usersState?.usersDefList)?window.usersState.usersDefList:[]);
          const match = defList.find(d => norm(d.name) === uName);
          if (match) {
            const mid = parseInt(match.userId ?? match.id, 10);
            if (!isNaN(mid) && mid>0 && mid<=9999) return mid;
          }
          return null;
        };
        const resolvedId = tryResolveId();
        const label = user.name || (resolvedId ? `ID ${resolvedId}` : 'Utilisateur');
        if (window.addUserTag) window.addUserTag(resolvedId, label); else addTagTo('destUsers', resolvedId? String(resolvedId) : label);
        // Desktop platform removed; keep only email/whatsapp enrichment
        const emailOnNow = document.getElementById('platformEmail')?.checked || false;
        const waOnNow = document.getElementById('platformWhatsApp')?.checked || false;
        if (emailOnNow && user.email) addTagTo('destEmails', user.email);
        if (waOnNow) {
          if (user.phoneNumber) {
            const phones = splitPhones(user.phoneNumber);
            if (phones.length){ phones.forEach(p=> addTagTo('destPhones', p)); }
            else { try { showFinalStatusToast(`${user.name || 'Cet utilisateur'} n'a pas de numéro WhatsApp.`, 'warning'); } catch {} }
          }
        }
      });
    });
  };
  searchBox.removeEventListener('input', renderRows); // defensive
  searchBox.addEventListener('input', ()=>{
    renderRows();
  });
  const emailCheck = document.getElementById('platformEmail');
  const waCheck = document.getElementById('platformWhatsApp');
  if (emailCheck) emailCheck.addEventListener('change', renderRows);
  if (waCheck) waCheck.addEventListener('change', renderRows);
  renderRows();
}

export function setupComposeHandlers() {
  dbg('setupComposeHandlers: called');
  const saveQuickBtn = document.getElementById('saveQuickBtn');
  if (saveQuickBtn) {
    dbg('setupComposeHandlers: saveQuickBtn found');
    saveQuickBtn.addEventListener('click', async () => {
      dbg('setupComposeHandlers: saveQuickBtn click');
      const title = document.getElementById('composeTitle')?.value || '';
      const message = document.getElementById('composeMessage')?.value || '';
      const type = document.getElementById('composeType')?.value || 'Information';
      try {
        const r = await fetch('/AlertsCrud/SaveQuick', { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ title, message, type }) });
        dbg('setupComposeHandlers: quick alert save API status', r && r.status);
        if (!r.ok) throw new Error('HTTP '+r.status);
        await loadQuickTemplates();
        alert('Alerte rapide enregistrée');
      } catch (e) {
        logError('SaveQuick failed', e);
        alert("Échec d'enregistrement de l'alerte rapide");
      }
    });
  } else { dbg('setupComposeHandlers: saveQuickBtn not found'); }
  // --- Handler complet pour le bouton d'envoi ---
  const sendBtn = document.getElementById('composeSendBtn');
  let isSending = false;
  if (sendBtn){
    dbg('setupComposeHandlers: sendBtn found and click handler init');
    document.addEventListener('visibilitychange', ()=>{
      if (document.visibilityState === 'visible') { sendBtn.disabled = false; isSending = false; sendBtn.classList.remove('disabled'); dbg('setupComposeHandlers: visibilitychange re-enabled sendBtn'); }
    });
    sendBtn.addEventListener('click', async ()=>{
      dbg('setupComposeHandlers: sendBtn click');
      if (isSending) { dbg('SendButton: Already sending - ignored'); return; }
      sendBtn.disabled = false; sendBtn.classList.remove('disabled');
      isSending = true;
      // Legacy logic →
      const title = document.getElementById('composeTitle')?.value || '';
      const message = document.getElementById('composeMessage')?.value || '';
      const emails = getTagValues('destEmails');
      const phones = getTagValues('destPhones');
      const userTags = getTagValues('destUsers').map(x=>parseInt(x,10)).filter(n=>!isNaN(n));
      const platforms = {
        Email: document.getElementById('platformEmail')?.checked || false,
        WhatsApp: document.getElementById('platformWhatsApp')?.checked || false
      };
      const norm = (s)=> (s||'').toString().trim().toLowerCase().replace(/\s+/g,' ');
      const allDef = (Array.isArray(window.usersState?.usersDefList)?window.usersState.usersDefList:[]);
      let usersList = [];
      try {
        const data = await fetchJson('/Dashboard/GetUsers');
        usersList = [].concat(
          Array.isArray(data.defUsers)?data.defUsers:[],
          Array.isArray(data.grhUsers)?data.grhUsers:[]
        );
      } catch { usersList = []; }
      // Collect from checkboxes selection if exists (or fallback usersList)
      const checkedIds = Array.from(document.querySelectorAll('#usersCheckboxes .user-check:checked')).map(x=>x.value);
      const selectedUsers = usersList.filter(u=>(checkedIds.includes((u.id??u.userId).toString())));
      // Ajoute email/phones des users cochés selon plateforme :
      let combinedEmails = [...emails];
      let combinedPhones = [...phones];
      if (platforms.Email) {
        selectedUsers.forEach(user=>{ if(user.email && !combinedEmails.includes(user.email)) combinedEmails.push(user.email); });
      }
      // Build maps email->userId and phone->userId using combined lists (after merging)
      const emailUserMap = {};
      const phoneUserMap = {};
      combinedEmails.forEach(e=>{
        const m = allDef.find(d=> (d.email||'').toLowerCase() === (e||'').toLowerCase());
        if (m) { const id = parseInt(m.userId ?? m.id, 10); if (!isNaN(id)) emailUserMap[e]=id; }
      });
      const onlyDigits = s => (s||'').toString().replace(/\D/g,'');
      combinedPhones.forEach(p=>{
        const target = onlyDigits(p).replace(/^00/, '');
        const found = allDef.find(d=>{
          const src = onlyDigits(d.phoneNumber||'');
          if (!src) return false;
          return src.endsWith(target) || target.endsWith(src);
        });
        if (found) { const id = parseInt(found.userId ?? found.id, 10); if (!isNaN(id)) phoneUserMap[p]=id; }
      });
      if (platforms.WhatsApp) {
        selectedUsers.forEach(user=>{ if(user.phoneNumber && !combinedPhones.includes(user.phoneNumber)) combinedPhones.push(user.phoneNumber); });
      }
      // Desktop platform removed
      dbg('SendButton: Collected form data', { title, message, emails:combinedEmails, phones:combinedPhones, platforms, selectedUsersCount:selectedUsers.length });
      // Validation complète legacy :
      if (!title.trim()) {
        logError('SendButton: Validation failed', new Error('Title is required'));
        alert('Le titre de l\'alerte est obligatoire');
        isSending = false; sendBtn.disabled = false; sendBtn.classList.remove('disabled'); return;
      }
      if (!platforms.Email && !platforms.WhatsApp) {
        logError('SendButton: Validation failed', new Error('No platform selected'));
        alert('Veuillez sélectionner au moins une plateforme d\'envoi');
        isSending = false; sendBtn.disabled = false; sendBtn.classList.remove('disabled'); return;
      }
      if (combinedEmails.length === 0 && combinedPhones.length === 0) {
        logError('SendButton: Validation failed', new Error('No recipients'));
        alert('Veuillez ajouter au moins un destinataire (email ou téléphone)');
        isSending = false; sendBtn.disabled = false; sendBtn.classList.remove('disabled'); return;
      }
      if (platforms.WhatsApp && combinedPhones.length === 0) {
        logError('SendButton: Validation failed', new Error('No WhatsApp phone numbers'));
        alert('Veuillez ajouter au moins un numéro de téléphone pour WhatsApp (ex: +216XXXXXXXX)');
        isSending = false; sendBtn.disabled = false; sendBtn.classList.remove('disabled'); return;
      }
      try {
        dbg('SendButton: Starting API request', { combinedEmails, combinedPhones, platforms });
        sendBtn.disabled = true; sendBtn.classList.add('disabled');
        const typeVal = document.getElementById('composeType')?.value || 'acquittementNonNecessaire';
        const payload = {
          title,
          message,
          emails: combinedEmails,
          phones: combinedPhones,
          userIds: [],
          emailUserMap,
          phoneUserMap,
          platforms: {
            Email: !!platforms.Email,
            WhatsApp: !!platforms.WhatsApp,
            Desktop: false
          },
          alertTypeId: (typeVal === 'acquittementNecessaire' ? 2 : 1)
        };
        const r = await fetch('/AlertsCrud/Send', { method:'POST', headers: {'Content-Type':'application/json'}, body: JSON.stringify(payload) });
        dbg('SendButton: Received response', { status: r.status, statusText: r.statusText, ok: r.ok });
        if (!r.ok) { const errorText = await r.text(); throw new Error(`HTTP ${r.status}: ${errorText}`); }
        const j = await r.json(); dbg('SendButton: Response data', j);
        if (j && (j.success === true || r.status === 200 || r.status === 207)) {
          showFinalStatusToast("Alerte mise en file d'attente pour envoi.", 'info');
        } else {
          logError('SendButton: Invalid response', new Error('Missing success flag'), j);
        }
        const modalEl = document.getElementById('newAlertModal');
        if (modalEl && window.bootstrap?.Modal){ window.bootstrap.Modal.getOrCreateInstance(modalEl).hide(); }
        dbg('SendButton: Reloading inbox');
        if (window.loadInbox) window.loadInbox();
        if (window.loadSent) window.loadSent();
        try { if (window.loadOutboxKpiData) window.loadOutboxKpiData(); } catch {}
        try { if (window.loadInboxKpiData) window.loadInboxKpiData(); } catch {}
        logSuccess('SendButton: Alert sent successfully', { alerteId: j.alerteId });
      } catch(err){
        logError('SendButton: Send failed', err, { title, message, emails, phones, platforms });
        alert("Échec de création d'alerte: " + (err.message||err));
      } finally {
        isSending = false; sendBtn.disabled = false; sendBtn.classList.remove('disabled');
      }
    });
  } else { dbg('setupComposeHandlers: sendBtn not found'); }
}

export function getSelectedUsersData(){
  const boxContainer = document.getElementById('usersCheckboxes');
  if (boxContainer){
    const ids = Array.from(boxContainer.querySelectorAll('.user-check:checked')).map(x=>x.value);
    return usersState.allUsers().filter(u => ids.includes((u.id ?? u.userId).toString()));
  }
  const selectElement = document.getElementById('selectedUsers');
  if (!selectElement) return [];
  const selectedValues = Array.from(selectElement.selectedOptions).map(option => option.value);
  return usersState.allUsers().filter(user => selectedValues.includes((user.id ?? user.userId).toString()));
}

export function setupDynamicPlatforms(){
  const emailChk = document.getElementById('platformEmail');
  const waChk = document.getElementById('platformWhatsApp');
  const emailInput = document.getElementById('destEmails')?.closest('.col-12');
  const phoneInput = document.getElementById('destPhones')?.closest('.col-12');
  const usersInput = document.getElementById('destUsers')?.closest('.col-12');
  const updateVis = () => {
    if (emailInput) emailInput.style.display = emailChk?.checked ? '' : 'none';
    if (phoneInput) phoneInput.style.display = waChk?.checked ? '' : 'none';
    if (usersInput) usersInput.style.display = 'none'; // desktop removed
    dbg('platforms:toggle', { email: !!emailChk?.checked, whatsapp: !!waChk?.checked });
  };
  emailChk?.addEventListener('change', updateVis);
  waChk?.addEventListener('change', updateVis);
  updateVis();
}
// Expose helpers to window
if (typeof window !== "undefined") {
  window.addTagTo = addTagTo;
  window.getTagValues = getTagValues;
  window.splitPhones = splitPhones;
  window.getSelectedUsersData = getSelectedUsersData;
  window.loadUsers = loadUsers;
}

// 1. Always fetch users & re-init tab/modal users on modal open
if (typeof window !== "undefined") {
  // Listen modal open (newAlertModal) to reload usersList + tabs
  const newAlertModal = document.getElementById('newAlertModal');
  if (newAlertModal) {
    newAlertModal.addEventListener('show.bs.modal', () => {
      dbg('compose.js: Modal opened, reloading users data/tabs');
      loadUsers().catch(e => logError('compose.js: loadUsers on show', e));
    });
  }
  // On platform switch, reload user list + update/tab synchronisation
  ['platformEmail','platformWhatsApp'].forEach(id=>{
    const inp = document.getElementById(id);
    if(inp) inp.addEventListener('change', ()=>{
      dbg('compose.js: Platform switch, reloading users/table/tags', {id});
      loadUsers().catch(e => logError('compose.js: loadUsers on platform change', e));
    });
  });
}
// 2. Afficher une notification/toast si aucun user dans un tab après recherche/platforms
// (Dans renderRows déjà - sinon ajouter un toast si filtered.length === 0)
// ...
// 3. Les helpers sont exposés pour devtools (voir en fin de fichier).
// (en plus de l’exposition ci-dessus)
