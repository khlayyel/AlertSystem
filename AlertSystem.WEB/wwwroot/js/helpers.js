export const __ALERT_DEBUG = true;
export function dbg() {
  try {
    if (__ALERT_DEBUG && window.console) {
      const timestamp = new Date().toISOString();
      const args = Array.from(arguments);
      args.unshift(`[${timestamp}]`);
      console.debug.apply(console, args);
    }
  } catch (e) {
    console.error('Debug logging error:', e);
  }
}
export function logError(context, error, additionalData = {}) {
  dbg('ERROR:', context, {
    error: error.message || error,
    stack: error.stack,
    additionalData,
    timestamp: new Date().toISOString()
  });
  console.error(`[${context}]`, error, additionalData);
}
export function logSuccess(context, data = {}) {
  dbg('SUCCESS:', context, {
    data,
    timestamp: new Date().toISOString()
  });
}
export function showLoading(containerId) {
  const c = document.getElementById(containerId);
  if (!c) return;
  c.innerHTML = '<div class="gmail-loading"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Chargement...</span></div><span class="ms-2">Chargement des alertes...</span></div>';
}
export async function fetchJson(url) {
  dbg('fetchJson: START', url);
  try {
    const r = await fetch(url, { cache: 'no-store', credentials: 'same-origin' });
    if (r.status === 401) {
      dbg('fetchJson: HTTP 401 Unauthorized', {url});
      if(window.showFinalStatusToast) window.showFinalStatusToast('Vous devez être connecté pour accéder à cette donnée. (401)', 'warning');
      throw new Error('HTTP 401 Unauthorized');
    }
    if (!r.ok) {
      const text = await r.text();
      dbg('fetchJson: HTTP FAIL', {url, status: r.status, text});
      if(window.showFinalStatusToast) window.showFinalStatusToast('Erreur API backend : '+r.status, 'danger');
      throw new Error(`HTTP ${r.status} @ ${url}: ${text}`);
    }
    const j = await r.json();
    dbg('fetchJson: JSON OK', {url, keys: Object.keys(j||{})});
    return j;
  } catch(error) {
    dbg('fetchJson: CATCH', {url, error});
    if(window.showFinalStatusToast) window.showFinalStatusToast('Erreur connexion au backend/API', 'danger');
    throw error;
  }
}
export function normalizePhone(raw) {
  try {
    let p = (raw || '').toString().trim();
    if (!p) return '';
    p = p.replace(/[^+\d]/g, '');
    if (p.startsWith('00')) p = '+' + p.substring(2);
    if (!p.startsWith('+') && /^\d{8,15}$/.test(p)) {
      if (p.length === 8) p = '+216' + p;
      else p = '+' + p;
    }
    return p;
  } catch { return (raw || ''); }
}
export function splitPhones(raw) {
  const text = (raw || '').toString();
  const groups = text.match(/\d{8,15}/g) || [];
  const uniques = new Set();
  const result = [];
  for (const g of groups) {
    const norm = normalizePhone(g);
    if (norm && !uniques.has(norm)) { uniques.add(norm); result.push(norm); }
  }
  return result;
}

if (typeof window !== "undefined") {
  window.dbg = dbg;
  window.logError = logError;
  window.logSuccess = logSuccess;
  window.showLoading = showLoading;
  window.fetchJson = fetchJson;
  window.normalizePhone = normalizePhone;
  window.splitPhones = splitPhones;
}
