import { dbg, normalizePhone } from '../helpers.js';

export function makeTagInput(inputId, separatorRegex, normalizer) {
  const input = document.getElementById(inputId);
  if (!input) return;
  if (input.dataset.tagInit === '1') { dbg('tag:init:already', { inputId }); return; }
  const wrap = document.createElement('div');
  wrap.className = 'tag-input form-control';
  input.parentNode.insertBefore(wrap, input);
  input.style.display = 'none';
  const list = document.createElement('div'); list.className = 'tags'; wrap.appendChild(list);
  const editor = document.createElement('input');
  editor.type = 'text';
  editor.className = 'tag-editor';
  // Accessibility: ensure dynamically injected editor has stable id/name
  try {
    const base = (input.id || input.name || 'tags') + '_taginput';
    editor.id = base;
    // Keep name to help autofill/tools; avoid clashing with the hidden field's name
    editor.name = base;
  } catch (_) { /* best-effort only */ }
  wrap.appendChild(editor);
  input.dataset.tagInit = '1';
  const addTag = (raw)=>{
    let v = (raw||'').trim();
    if (!v) return;
    if (normalizer) v = normalizer(v);
    // Prevent duplicates
    const existing = Array.from(list.querySelectorAll('.tag')).map(t=>t.firstChild.nodeValue);
    if (existing.includes(v)) return;
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
  (input.value||'').split(',').filter(x=>x).forEach(addTag);
}

export function setupTagInputs() {
  makeTagInput('destEmails', /[\,\s]/);
  makeTagInput('destPhones', /[\,\s]/, normalizePhone);
}

export function addTagTo(inputId, value) {
  const hidden = document.getElementById(inputId);
  if (!hidden) return;
  const wrap = hidden.previousSibling;
  if (!wrap || !wrap.classList || !wrap.classList.contains('tag-input')) return;
  const list = wrap.querySelector('.tags');
  const vals = Array.from(list.querySelectorAll('.tag')).map(t=>t.firstChild.nodeValue);
  let v = value;
  if (inputId === 'destPhones') v = normalizePhone(value);
  if (vals.includes(v)) return;
  const tag = document.createElement('span'); tag.className='tag'; tag.textContent = v;
  const x = document.createElement('button'); x.type='button'; x.className='tag-x'; x.textContent='×';
  x.onclick = ()=>{ list.removeChild(tag); const arr = Array.from(list.querySelectorAll('.tag')).map(t=>t.firstChild.nodeValue); hidden.value = arr.join(','); };
  tag.appendChild(x); list.appendChild(tag);
  const arr = Array.from(list.querySelectorAll('.tag')).map(t=>t.firstChild.nodeValue);
  hidden.value = arr.join(',');
}

export function getTagValues(inputId) {
  const hidden = document.getElementById(inputId);
  if (!hidden) return [];
  return (hidden.value||'').split(',').map(s=>s.trim()).filter(Boolean);
}

// Expose helpers to window for legacy/devtools
if (typeof window !== "undefined") {
  window.makeTagInput = makeTagInput;
  window.setupTagInputs = setupTagInputs;
  window.addTagTo = addTagTo;
  window.getTagValues = getTagValues;
}
