export const usersState = {
  usersList: [],
  usersDefList: [],
  usersGrhList: [],
  refreshed: false,
  loading: false,
  listeners: [], // for dev/devtools
  allUsers() { return [...this.usersDefList, ...this.usersGrhList]; }
};

export async function ensureUsersLoaded(forceReload = false) {
  if (usersState.loading) return;
  if (usersState.refreshed && !forceReload) return;
  usersState.loading = true;
  try {
    const resp = await fetch('/Dashboard/GetUsers', { cache:'no-store', credentials: 'same-origin' });
    const data = await resp.json();
    usersState.usersDefList = Array.isArray(data.defUsers) ? data.defUsers : [];
    usersState.usersGrhList = Array.isArray(data.grhUsers) ? data.grhUsers : [];
    usersState.usersList = usersState.allUsers();
    usersState.refreshed = true;
    if (typeof window.dbg === 'function') window.dbg('usersState: loaded', {def: usersState.usersDefList.length, grh: usersState.usersGrhList.length});
    // Notif listeners
    usersState.listeners.forEach(f=>{try{f(usersState);}catch{}});
  }catch(e){
    if (typeof window.logError === 'function') window.logError('usersState: failed', e);
    usersState.usersDefList = [];
    usersState.usersGrhList = [];
    usersState.usersList = [];
    usersState.refreshed = false;
  }
  usersState.loading = false;
}

if (typeof window !== "undefined") {
  window.usersState = usersState;
  window.ensureUsersLoaded = ensureUsersLoaded;
}
