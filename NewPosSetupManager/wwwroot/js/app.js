/* POS Setup Manager — main app */
(function () {
'use strict';

// ── State ──────────────────────────────────────────
const S = {
  sessions: { active: [], completed: [] },
  currentId: null,
  tab: 0,
  saveTimer: null,
  showCompleted: false,
  search: '',
  dragging: null,
  filterDate: new Date().toISOString().slice(0, 10),
};

// ── Helpers ────────────────────────────────────────
function $(id) { return document.getElementById(id); }
function el(tag, cls, txt) {
  const e = document.createElement(tag);
  if (cls) e.className = cls;
  if (txt !== undefined) e.textContent = txt;
  return e;
}
function show(id) { const e = $(id); if (e) e.classList.remove('hidden'); }
function hide(id) { const e = $(id); if (e) e.classList.add('hidden'); }

let toastTimer;
function toast(msg, type) {
  const t = $('toast');
  t.textContent = msg;
  t.className = 'toast show' + (type ? ' ' + type : '');
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => t.classList.remove('show'), 2500);
}

function fmtDate(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  if (isNaN(d)) return '';
  return (d.getMonth() + 1) + '/' + d.getDate();
}

function fmtDateFull(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  if (isNaN(d)) return '';
  return d.getFullYear() + '년 ' + (d.getMonth() + 1) + '월 ' + d.getDate() + '일';
}

function getPath(obj, path) {
  return path.split('.').reduce((o, k) => (o ? o[k] : undefined), obj);
}
function setPath(obj, path, val) {
  const keys = path.split('.');
  let cur = obj;
  for (let i = 0; i < keys.length - 1; i++) {
    if (!cur[keys[i]]) cur[keys[i]] = {};
    cur = cur[keys[i]];
  }
  cur[keys[keys.length - 1]] = val;
}

function currentSession() {
  if (!S.currentId) return null;
  return S.sessions.active.find(s => s.id === S.currentId)
      || S.sessions.completed.find(s => s.id === S.currentId);
}

// ── Time datalist ──────────────────────────────────
function pad(n) { return n < 10 ? '0' + n : '' + n; }

function buildTimeDatelist() {
  const dl = $('time-list');
  if (!dl) return;
  for (let h = 8; h <= 20; h++) {
    const o1 = document.createElement('option'); o1.value = pad(h) + ':00'; dl.appendChild(o1);
    if (h < 20) { const o2 = document.createElement('option'); o2.value = pad(h) + ':30'; dl.appendChild(o2); }
  }
}

// ── Router autocomplete (localStorage) ────────────
const AC_PREFIX = 'router_ac_';
const AC_MAX = 10;

function acLoad(key) {
  try { return JSON.parse(localStorage.getItem(AC_PREFIX + key) || '[]'); } catch { return []; }
}
function acSave(key, value) {
  if (!value || !value.trim()) return;
  let list = acLoad(key).filter(v => v !== value);
  list.unshift(value);
  if (list.length > AC_MAX) list = list.slice(0, AC_MAX);
  localStorage.setItem(AC_PREFIX + key, JSON.stringify(list));
}
function showAcDropdown(inp, key) {
  const dd = $('acDropdown');
  const q = inp.value.trim().toLowerCase();
  const items = acLoad(key).filter(v => !q || v.toLowerCase().includes(q));
  if (items.length === 0) { dd.classList.add('hidden'); return; }
  dd.innerHTML = '';
  items.forEach(v => {
    const item = document.createElement('div');
    item.className = 'ac-item';
    const text = document.createElement('span');
    text.className = 'ac-text';
    text.textContent = v;
    const del = document.createElement('button');
    del.className = 'ac-del';
    del.textContent = '×';
    del.addEventListener('mousedown', e => {
      e.preventDefault();
      const list = acLoad(key).filter(x => x !== v);
      localStorage.setItem(AC_PREFIX + key, JSON.stringify(list));
      showAcDropdown(inp, key);
    });
    text.addEventListener('mousedown', e => {
      e.preventDefault();
      inp.value = v;
      inp.dispatchEvent(new Event('input'));
      dd.classList.add('hidden');
    });
    item.appendChild(text);
    item.appendChild(del);
    dd.appendChild(item);
  });
  const r = inp.getBoundingClientRect();
  dd.style.left = r.left + 'px';
  dd.style.top = (r.bottom + 2) + 'px';
  dd.style.width = r.width + 'px';
  dd.classList.remove('hidden');
}

function initRouterAutocomplete() {
  document.querySelectorAll('.router-ac').forEach(inp => {
    const key = inp.dataset.acKey;
    if (!key) return;
    inp.removeAttribute('list');
    inp.addEventListener('focus', () => showAcDropdown(inp, key));
    inp.addEventListener('input', () => showAcDropdown(inp, key));
    inp.addEventListener('blur', () => {
      setTimeout(() => $('acDropdown').classList.add('hidden'), 200);
      if (!inp.value.trim()) return;
      acSave(key, inp.value.trim());
    });
  });
  document.addEventListener('mousedown', e => {
    if (!$('acDropdown').contains(e.target)) $('acDropdown').classList.add('hidden');
  });
}

// ── Progress calculation ───────────────────────────
function calcProgress(session) {
  if (!session) return { pct: 0, dots: [false, false, false, false] };
  const d = session.data;
  const dots = [
    !!(d.basic.storeName && d.basic.startTime && d.basic.endTime && d.basic.installTime),
    !!(d.pos.remoteAccount && d.pos.remoteAdmin && d.pos.lmmAccount
        && d.pos.posTypes && d.pos.posTypes.length >= 1 && d.pos.vanType && d.pos.tableMode),
    !!(d.checklist.checkDHCP && d.checklist.checkExternalIP
        && d.checklist.localModeMenuBoard && d.checklist.localModeNoticeBoard),
    !!(d.checklist.checkFirewall && d.checklist.checkFirewallPopup
        && d.checklist.checkHiorderLogin && d.checklist.checkSyncOrder
        && d.checklist.checkTableSort && d.checklist.wifiStatus
        && d.checklist.checkMenuImage && d.checklist.checkMenuBoardAutoRun
        && d.checklist.checkNoticeBoardVer && d.checklist.checkMenuBoardVer
        && d.checklist.checkCoupon),
  ];
  const done = dots.filter(Boolean).length;
  return { pct: Math.round(done / 4 * 100), dots };
}

function getMissingFields(d) {
  const missing = [];
  if (!d.basic.storeName)   missing.push('매장명');
  if (!d.basic.installTime) missing.push('설치 예정 시간');
  if (!d.basic.startTime)   missing.push('작업 시작 시간');
  if (!d.basic.endTime)     missing.push('작업 종료 시간');
  if (!d.pos.tableMode)     missing.push('테이블 모드');
  if (!d.pos.remoteAccount) missing.push('원격 계정 종류');
  if (!d.pos.remoteAdmin)   missing.push('원격어드민');
  if (!d.pos.lmmAccount)    missing.push('LMM 매장명');
  if (!d.pos.posTypes || d.pos.posTypes.length === 0) missing.push('POS 종류');
  if (!d.pos.vanType)       missing.push('밴사');
  if (!d.checklist.checkExternalIP)      missing.push('외부 공인 IP 확인');
  if (!d.checklist.checkDHCP)            missing.push('DHCP 및 포트포워딩');
  if (!d.checklist.localModeMenuBoard)   missing.push('로컬 모드 — 메뉴판');
  if (!d.checklist.localModeNoticeBoard) missing.push('로컬 모드 — 알림판');
  if (!d.checklist.checkFirewall)        missing.push('방화벽 OFF');
  if (!d.checklist.checkFirewallPopup)   missing.push('방화벽 팝업 OFF');
  if (!d.checklist.checkHiorderLogin)    missing.push('하이오더 포스 계정 로그인');
  if (!d.checklist.checkSyncOrder)       missing.push('동기화 및 주문 테스트 출력');
  if (!d.checklist.checkTableSort)       missing.push('동기화 후 환경설정 테이블관리');
  if (!d.checklist.wifiStatus)           missing.push('와이파이 상태');
  if (!d.checklist.checkMenuImage)       missing.push('메뉴 이미지 요청');
  if (!d.checklist.checkMenuBoardAutoRun) missing.push('매니저 자동 실행 확인');
  if (!d.checklist.checkNoticeBoardVer)  missing.push('알림판 Ver 확인');
  if (!d.checklist.checkMenuBoardVer)    missing.push('메뉴판 Ver 확인');
  if (!d.checklist.checkCoupon)          missing.push('쿠폰 생성 확인');
  return missing;
}

function calcElapsed(start, end) {
  if (!start || !end) return '';
  try {
    const [sh, sm] = start.split(':').map(Number);
    const [eh, em] = end.split(':').map(Number);
    if (isNaN(sh) || isNaN(sm) || isNaN(eh) || isNaN(em)) return '';
    let mins = (eh * 60 + em) - (sh * 60 + sm);
    if (mins < 0) mins += 24 * 60;
    return mins + '분';
  } catch { return ''; }
}

// ── Sidebar rendering ──────────────────────────────
function renderSidebar() {
  const q = S.search.toLowerCase();
  const active = S.sessions.active.filter(s =>
    !q || (s.data.basic.storeName || '').toLowerCase().includes(q)
  );
  const completed = S.sessions.completed.filter(s => {
    if (q && !(s.data.basic.storeName || '').toLowerCase().includes(q)) return false;
    if (S.filterDate && (s.data.basic.installDate || '') !== S.filterDate) return false;
    return true;
  });

  $('activeCount').textContent = S.sessions.active.length;
  $('completedCount').textContent = S.sessions.completed.length;

  renderList($('activeList'), active, true);
  renderList($('completedList'), completed, false);
}

function renderList(container, sessions, isDraggable) {
  container.innerHTML = '';
  if (sessions.length === 0) {
    const empty = el('div', null, isDraggable ? '매장이 없습니다' : '');
    empty.style.cssText = 'color:rgba(220,225,235,.3);font-size:12px;padding:8px 12px;text-align:center';
    container.appendChild(empty);
    return;
  }
  sessions.forEach(s => container.appendChild(makeCard(s, isDraggable)));
}

function makeCard(session, isDraggable) {
  const div = el('div', 'store-card');
  div.dataset.id = session.id;
  if (session.id === S.currentId) div.classList.add('active');
  if (isDraggable) {
    div.draggable = true;
    div.addEventListener('dragstart', onDragStart);
    div.addEventListener('dragover', onDragOver);
    div.addEventListener('drop', onDrop);
    div.addEventListener('dragend', onDragEnd);
  }

  const { pct } = calcProgress(session);
  const _sn = session.data.basic.storeName || '새 매장';
  const _sid = session.data.basic.storeId;
  const name = _sid ? `${_sn} (${_sid})` : _sn;
  const date = fmtDate(session.data.basic.installDate);
  const tableMode = session.data.basic.tableMode || session.data.pos.tableMode || '';
  const isActive = session.status === '작성중';

  const statusCls = session.status === '완료' ? 'done' : isActive ? 'active' : 'idle';
  const statusTxt = session.status || '대기';

  div.innerHTML = `
    <div class="card-name">${escHtml(name)}</div>
    <div class="card-meta">
      <span class="card-status ${statusCls}">${escHtml(statusTxt)}</span>
      ${tableMode ? `<span class="card-mode ${escHtml(tableMode)}">${escHtml(tableMode)}</span>` : ''}
    </div>
    <div class="card-prog"><div class="card-prog-fill" style="width:${pct}%"></div></div>
  `;
  div.addEventListener('click', () => selectSession(session.id));
  return div;
}

function escHtml(s) {
  return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

// ── Drag & drop ────────────────────────────────────
function onDragStart(e) {
  S.dragging = this.dataset.id;
  this.classList.add('dragging');
  e.dataTransfer.effectAllowed = 'move';
}
function onDragOver(e) {
  e.preventDefault();
  e.dataTransfer.dropEffect = 'move';
  document.querySelectorAll('.store-card.drag-over').forEach(c => c.classList.remove('drag-over'));
  this.classList.add('drag-over');
}
function onDrop(e) {
  e.preventDefault();
  const targetId = this.dataset.id;
  if (!S.dragging || S.dragging === targetId) return;
  const arr = S.sessions.active;
  const fi = arr.findIndex(s => s.id === S.dragging);
  const ti = arr.findIndex(s => s.id === targetId);
  if (fi < 0 || ti < 0) return;
  arr.splice(ti, 0, arr.splice(fi, 1)[0]);
  Bridge.send('reorderSessions', { ids: arr.map(s => s.id) });
  renderSidebar();
}
function onDragEnd() {
  document.querySelectorAll('.store-card.dragging,.store-card.drag-over')
    .forEach(c => c.classList.remove('dragging','drag-over'));
  S.dragging = null;
}

// ── Session selection ──────────────────────────────
function selectSession(id) {
  S.currentId = id;
  renderSidebar();
  const session = currentSession();
  if (!session) return;
  show('sessionView');
  hide('emptyState');
  loadForm(session);
  updateHeader(session);
  updateProgress(session);
}

function updateHeader(session) {
  if (!session) return;
  const name = session.data.basic.storeName || '새 매장';
  $('sTitle').textContent = name;
  const date = fmtDateFull(session.data.basic.installDate);
  $('sDate').textContent = date;

  const badge = $('sBadge');
  badge.className = 's-badge';
  if (session.status === '완료') { badge.textContent = '완료'; badge.classList.add('done'); }
  else if (session.status === '작성중') { badge.textContent = '진행중'; badge.classList.add('active'); }
  else { badge.textContent = '대기'; badge.classList.add('idle'); }

  const isCompleted = session.status === '완료';
  $('btnComplete').style.display = isCompleted ? 'none' : '';
  $('btnRestore').style.display = isCompleted ? '' : 'none';
}

function updateProgress(session) {
  const { pct, dots } = calcProgress(session);
  $('progFill').style.width = pct + '%';
  $('progText').textContent = pct + '%';
  dots.forEach((done, i) => {
    const dot = $('dot' + i);
    if (dot) { if (done) dot.classList.remove('hidden'); else dot.classList.add('hidden'); }
  });
}

// ── Form load ──────────────────────────────────────
let _loading = false;

function loadForm(session) {
  _loading = true;
  const d = session.data;

  // Text/select inputs by data-path
  document.querySelectorAll('[data-path]').forEach(el => {
    if (el.tagName === 'INPUT' || el.tagName === 'SELECT' || el.tagName === 'TEXTAREA') {
      const p = el.dataset.path;
      const val = getPath(d, p);
      if (el.type === 'date') {
        el.value = val ? (typeof val === 'string' ? val.split('T')[0] : '') : '';
      } else {
        el.value = val || '';
      }
    }
  });

  // Radio groups
  document.querySelectorAll('[id^="rg-"]').forEach(rg => {
    const p = rg.dataset.path;
    const val = getPath(d, p) || '';
    rg.querySelectorAll('.radio-option').forEach(opt => {
      opt.classList.toggle('sel', opt.dataset.val === val);
    });
  });

  // OX groups
  document.querySelectorAll('[id^="ox-"]').forEach(og => {
    const p = og.dataset.path;
    const val = getPath(d, p) || '';
    og.querySelectorAll('.ox-btn').forEach(btn => {
      btn.classList.toggle('sel', btn.dataset.val === val);
    });
  });

  // Tag groups (generic: array or string, supports data-radio)
  document.querySelectorAll('[id^="tg-"]').forEach(tg => {
    const v = getPath(d, tg.dataset.path);
    tg.querySelectorAll('.tag').forEach(tag => {
      tag.classList.toggle('sel', Array.isArray(v) ? v.includes(tag.dataset.val) : v === tag.dataset.val);
    });
  });

  // WiFi status
  const wifiRg = $('rg-wifiStatus');
  if (wifiRg) {
    const wv = d.checklist.wifiStatus || '';
    wifiRg.querySelectorAll('.wifi-opt').forEach(o => o.classList.toggle('sel', o.dataset.val === wv));
  }

  // Attachment list
  renderAttachments(d.basic.attachmentPaths || []);

  // Coupon reason visibility
  toggleCouponReason(d.checklist.checkCoupon, false);

  // Prepaid section visibility
  togglePrepaid(d.basic.tableMode || d.pos.tableMode);

  // tableMode sync (tab0 and tab1 are synced)
  syncTableMode(d.basic.tableMode || d.pos.tableMode);

  // 시간 포맷 정규화
  ['f-installTime','f-startTime','f-endTime','f-linkEndTime'].forEach(tid => {
    const te = $(tid);
    if (!te || !te.value) return;
    const fmt = fmtTime(te.value);
    if (fmt && fmt !== te.value) { te.value = fmt; setPath(d, te.dataset.path, fmt); }
  });

  // 전화번호 포맷 정규화
  ['f-engineerContact','f-remoteEduContact'].forEach(pid => {
    const pe = $(pid);
    if (!pe || !pe.value) return;
    const fmt = fmtPhone(pe.value);
    if (fmt !== pe.value) { pe.value = fmt; setPath(d, pe.dataset.path, fmt); }
  });

  // 소요시간 재계산 (저장된 값이 NaN이거나 비어있으면)
  const _recalc = calcElapsed(d.basic.startTime, d.basic.endTime);
  const ef2 = $('f-elapsedTime');
  if (_recalc) {
    d.basic.elapsedTime = _recalc;
    if (ef2) ef2.value = _recalc;
  } else if (d.basic.elapsedTime && d.basic.elapsedTime.includes('NaN')) {
    d.basic.elapsedTime = '';
    if (ef2) ef2.value = '';
  }

  _loading = false;
}

function syncTableMode(val) {
  ['rg-tableMode','rg-tableMode2'].forEach(id => {
    const rg = $(id);
    if (!rg) return;
    rg.querySelectorAll('.radio-option').forEach(opt => {
      opt.classList.toggle('sel', opt.dataset.val === val);
    });
  });
}

function toggleCouponReason(couponVal, animate) {
  const wrap = $('couponReasonWrap');
  if (!wrap) return;
  wrap.style.display = couponVal === 'X' ? '' : 'none';
}

function togglePrepaid(tableMode) {
  const sec = $('prepaidSection');
  if (!sec) return;
  sec.style.display = tableMode === '선불' ? '' : 'none';
}

function renderAttachments(paths) {
  const list = $('attachList');
  if (!list) return;
  list.innerHTML = '';
  (paths || []).forEach((p, i) => {
    const item = el('div', 'attach-item');
    const name = p.split('\\').pop().split('/').pop();
    item.innerHTML = `<span>📎 ${escHtml(name)}</span>`;
    const rm = el('button', 'remove-attach', '×');
    rm.title = '제거';
    rm.addEventListener('click', () => removeAttachment(i));
    item.appendChild(rm);
    list.appendChild(item);
  });
}

function removeAttachment(index) {
  const session = currentSession();
  if (!session) return;
  const paths = session.data.basic.attachmentPaths || [];
  paths.splice(index, 1);
  session.data.basic.attachmentPaths = paths;
  renderAttachments(paths);
  scheduleSave();
}

// ── Form change handling ───────────────────────────
function onFormChange(e) {
  if (_loading) return;
  const session = currentSession();
  if (!session) return;
  const el = e.target;
  const p = el.dataset.path;
  if (!p) return;

  const d = session.data;
  if (el.type === 'checkbox') {
    setPath(d, p, el.checked);
  } else {
    setPath(d, p, el.value);
  }

  // Side effects
  if (p === 'basic.storeName') {
    $('sTitle').textContent = el.value || '새 매장';
    renderSidebar();
  }
  if (p === 'basic.installDate') {
    $('sDate').textContent = fmtDateFull(el.value);
    renderSidebar();
  }
  if (p === 'basic.startTime' || p === 'basic.endTime') {
    const elapsed = calcElapsed(d.basic.startTime, d.basic.endTime);
    d.basic.elapsedTime = elapsed;
    const ef = $('f-elapsedTime');
    if (ef) ef.value = elapsed;
  }
  if (p === 'finish.couponXReason') { /* no special handling */ }

  updateProgress(session);
  scheduleSave();
}

document.addEventListener('change', onFormChange);
document.addEventListener('input', e => {
  if (_loading) return;
  const el = e.target;
  if (el.tagName !== 'INPUT' && el.tagName !== 'TEXTAREA') return;
  const p = el.dataset.path;
  if (!p) return;
  const session = currentSession();
  if (!session) return;
  setPath(session.data, p, el.value);
  if (p === 'basic.storeName') {
    $('sTitle').textContent = el.value || '새 매장';
  }
  updateProgress(session);
  scheduleSave();
});

// OX buttons
document.querySelectorAll('.ox-group').forEach(og => {
  og.querySelectorAll('.ox-btn').forEach(btn => {
    btn.addEventListener('click', () => {
      if (_loading) return;
      const session = currentSession();
      if (!session) return;
      const p = og.dataset.path;
      const val = btn.dataset.val;
      const cur = getPath(session.data, p);
      const newVal = cur === val ? '' : val; // toggle off
      setPath(session.data, p, newVal);
      og.querySelectorAll('.ox-btn').forEach(b => b.classList.toggle('sel', b.dataset.val === newVal));

      if (p === 'checklist.checkCoupon') toggleCouponReason(newVal, true);
      updateProgress(session);
      scheduleSave();
    });
  });
});

// Radio options
document.querySelectorAll('.radio-row').forEach(rg => {
  rg.querySelectorAll('.radio-option').forEach(opt => {
    opt.addEventListener('click', () => {
      if (_loading) return;
      const session = currentSession();
      if (!session) return;
      const p = rg.dataset.path;
      const val = opt.dataset.val;
      setPath(session.data, p, val);
      rg.querySelectorAll('.radio-option').forEach(o => o.classList.toggle('sel', o.dataset.val === val));

      // Sync tableMode between tab0 and tab1
      if (p === 'basic.tableMode' || p === 'pos.tableMode') {
        session.data.basic.tableMode = val;
        session.data.pos.tableMode = val;
        syncTableMode(val);
        togglePrepaid(val);
        renderSidebar();
      }
      updateProgress(session);
      scheduleSave();
    });
  });
});

// WiFi status
const wifiRg = $('rg-wifiStatus');
if (wifiRg) {
  wifiRg.querySelectorAll('.wifi-opt').forEach(opt => {
    opt.addEventListener('click', () => {
      if (_loading) return;
      const session = currentSession();
      if (!session) return;
      const val = opt.dataset.val;
      const cur = session.data.checklist.wifiStatus;
      const newVal = cur === val ? '' : val;
      session.data.checklist.wifiStatus = newVal;
      wifiRg.querySelectorAll('.wifi-opt').forEach(o => o.classList.toggle('sel', o.dataset.val === newVal));
      updateProgress(session);
      scheduleSave();
    });
  });
}

// Tag grid (generic: supports radio single-select and multi-select)
document.querySelectorAll('.tag-grid').forEach(tg => {
  const isRadio = tg.dataset.radio === 'true';
  const isString = tg.dataset.type === 'string';
  const path = tg.dataset.path;

  tg.querySelectorAll('.tag').forEach(tag => {
    tag.addEventListener('click', () => {
      if (_loading) return;
      const session = currentSession();
      if (!session) return;
      const val = tag.dataset.val;

      // Resolve parent object and key from path
      const parts = path.split('.');
      let obj = session.data;
      for (let i = 0; i < parts.length - 1; i++) obj = obj[parts[i]];
      const key = parts[parts.length - 1];

      if (isString) {
        // Single-select string: toggle off if same, else set
        obj[key] = obj[key] === val ? '' : val;
      } else {
        // Array-backed (multi or radio)
        const arr = Array.isArray(obj[key]) ? obj[key] : [];
        if (isRadio) {
          obj[key] = arr.includes(val) ? [] : [val];
        } else {
          const idx = arr.indexOf(val);
          if (idx >= 0) arr.splice(idx, 1); else arr.push(val);
          obj[key] = arr;
        }
      }

      // Update sel class for all tags in this grid
      const v = obj[key];
      tg.querySelectorAll('.tag').forEach(t => {
        t.classList.toggle('sel', Array.isArray(v) ? v.includes(t.dataset.val) : v === t.dataset.val);
      });

      updateProgress(session);
      scheduleSave();
    });
  });
});


// ── Phone format on blur ───────────────────────────
function fmtPhone(raw) {
  const d = raw.replace(/\D/g, '');
  if (!d) return raw;
  if (d.startsWith('02')) {
    if (d.length === 9)  return d.slice(0,2) + '-' + d.slice(2,5) + '-' + d.slice(5);
    if (d.length === 10) return d.slice(0,2) + '-' + d.slice(2,6) + '-' + d.slice(6);
  }
  if (d.length === 10) return d.slice(0,3) + '-' + d.slice(3,6) + '-' + d.slice(6);
  if (d.length === 11) return d.slice(0,3) + '-' + d.slice(3,7) + '-' + d.slice(7);
  return d;
}

['f-engineerContact','f-remoteEduContact'].forEach(id => {
  const el = $(id);
  if (!el) return;
  el.addEventListener('blur', () => {
    const formatted = fmtPhone(el.value);
    if (formatted !== el.value) {
      el.value = formatted;
      el.dispatchEvent(new Event('input'));
    }
  });
});

// ── Time format on blur ────────────────────────────
['f-installTime','f-startTime','f-endTime','f-linkEndTime'].forEach(id => {
  const el = $(id);
  if (!el) return;
  el.addEventListener('blur', () => {
    const formatted = fmtTime(el.value);
    if (formatted && formatted !== el.value) {
      el.value = formatted;
      el.dispatchEvent(new Event('input'));
    }
    if (id === 'f-startTime' || id === 'f-endTime') {
      const session = currentSession();
      if (!session) return;
      const d = session.data;
      if (id === 'f-startTime') d.basic.startTime = el.value;
      else d.basic.endTime = el.value;
      const elapsed = calcElapsed(d.basic.startTime, d.basic.endTime);
      d.basic.elapsedTime = elapsed;
      const ef = $('f-elapsedTime');
      if (ef) ef.value = elapsed;
      scheduleSave();
    }
  });
});

function fmtTime(s) {
  if (!s) return s;
  s = s.trim().replace(':', '');
  if (s.length === 1) s = '0' + s + '00';
  if (s.length === 2) s = s + '00';
  if (s.length === 3) s = '0' + s;
  if (s.length === 4) {
    const h = parseInt(s.slice(0, 2), 10), m = parseInt(s.slice(2), 10);
    if (h < 24 && m < 60) return pad(h) + ':' + pad(m);
  }
  return s;
}

// ── Auto save ──────────────────────────────────────
function scheduleSave() {
  clearTimeout(S.saveTimer);
  S.saveTimer = setTimeout(doSave, 600);
}

function doSave() {
  const session = currentSession();
  if (!session) return;
  Bridge.send('saveSession', { sessionId: session.id, data: session.data });
}

// ── Tab switching ──────────────────────────────────
function switchTab(t) {
  S.tab = t;
  document.querySelectorAll('.tab-btn').forEach(b => b.classList.toggle('active', parseInt(b.dataset.tab) === t));
  document.querySelectorAll('.tab-page').forEach((p, i) => p.classList.toggle('active', i === t));
}

document.querySelectorAll('.tab-btn').forEach(btn => {
  btn.addEventListener('click', () => switchTab(parseInt(btn.dataset.tab)));
});

// Alt+1~5 단축키
document.addEventListener('keydown', e => {
  if (!e.altKey || e.ctrlKey || e.metaKey) return;
  const n = parseInt(e.key);
  if (n >= 1 && n <= 5 && currentSession()) {
    e.preventDefault();
    switchTab(n - 1);
  }
});

// ── Sidebar controls ───────────────────────────────
$('btnNew').addEventListener('click', () => App.addSession());
$('btnCalendar').addEventListener('click', () => Bridge.send('openCalendar'));
$('btnSettings').addEventListener('click', () => Bridge.send('openSettings'));

// ── Theme (sidebar color) ──────────────────────────
const THEMES = [
  { name: '다크 네이비',  nav: '#1E1E28', hover: '#2D2D3A' },
  { name: '차콜',        nav: '#1C1C1C', hover: '#2B2B2B' },
  { name: '딥 그린',     nav: '#1A2B1E', hover: '#25392A' },
  { name: '버건디',      nav: '#2B1A1E', hover: '#3A252A' },
  { name: '오션',        nav: '#162235', hover: '#1F3049' },
  { name: '퍼플',        nav: '#211A2E', hover: '#2F263E' },
  { name: '포레스트',    nav: '#1B2C1F', hover: '#263D2A' },
  { name: '다크 브라운', nav: '#26200F', hover: '#352D1A' },
];
const THEME_KEY = 'sidebarTheme';
const DEFAULT_THEME = THEMES[0];

function hexToRgb(hex) {
  const r = parseInt(hex.slice(1,3),16), g = parseInt(hex.slice(3,5),16), b = parseInt(hex.slice(5,7),16);
  return { r, g, b };
}
function lighten(hex, amt) {
  const { r, g, b } = hexToRgb(hex);
  const clamp = v => Math.min(255, v + amt);
  return '#' + [clamp(r), clamp(g), clamp(b)].map(v => v.toString(16).padStart(2,'0')).join('');
}

const DEFAULT_TEXT = '#DCE1EB';

function applyTheme(nav, hover, textNav) {
  textNav = textNav || DEFAULT_TEXT;
  document.documentElement.style.setProperty('--nav', nav);
  document.documentElement.style.setProperty('--nav-hover', hover);
  document.documentElement.style.setProperty('--text-nav', textNav);
  document.querySelectorAll('.theme-swatch').forEach(s => {
    s.classList.toggle('active', s.dataset.nav === nav);
  });
  $('themeColorPicker').value = nav;
  $('themeTextPicker').value = textNav;
}

function saveTheme(nav, hover, textNav) {
  textNav = textNav || $('themeTextPicker').value || DEFAULT_TEXT;
  localStorage.setItem(THEME_KEY, JSON.stringify({ nav, hover, textNav }));
  applyTheme(nav, hover, textNav);
}

function loadTheme() {
  try {
    const t = JSON.parse(localStorage.getItem(THEME_KEY));
    if (t && t.nav) { applyTheme(t.nav, t.hover, t.textNav); return; }
  } catch {}
  applyTheme(DEFAULT_THEME.nav, DEFAULT_THEME.hover, DEFAULT_TEXT);
}

// 스와치 생성
const swatchContainer = $('themeSwatches');
THEMES.forEach(t => {
  const s = document.createElement('div');
  s.className = 'theme-swatch';
  s.dataset.nav = t.nav;
  s.dataset.hover = t.hover;
  s.style.background = t.nav;
  s.title = t.name;
  s.innerHTML = '<span class="swatch-check">✓</span>';
  s.addEventListener('click', () => saveTheme(t.nav, t.hover));
  swatchContainer.appendChild(s);
});

// 테마 패널 토글
$('btnTheme').addEventListener('click', e => {
  e.stopPropagation();
  $('themePanel').classList.toggle('hidden');
});
document.addEventListener('click', e => {
  if (!$('themePanel').contains(e.target) && e.target !== $('btnTheme'))
    $('themePanel').classList.add('hidden');
});

// 커스텀 컬러 피커 — 배경색
$('themeColorPicker').addEventListener('input', e => {
  const nav = e.target.value;
  const hover = lighten(nav, 20);
  saveTheme(nav, hover);
});

// 커스텀 컬러 피커 — 글씨색
$('themeTextPicker').addEventListener('input', e => {
  const stored = JSON.parse(localStorage.getItem(THEME_KEY) || '{}');
  const nav = stored.nav || DEFAULT_THEME.nav;
  const hover = stored.hover || DEFAULT_THEME.hover;
  saveTheme(nav, hover, e.target.value);
});

// 초기화
$('btnThemeReset').addEventListener('click', () => {
  saveTheme(DEFAULT_THEME.nav, DEFAULT_THEME.hover, DEFAULT_TEXT);
});

// 앱 시작 시 테마 복원
loadTheme();
$('searchInput').addEventListener('input', e => {
  S.search = e.target.value;
  renderSidebar();
});

$('toggleCompleted').addEventListener('click', () => {
  S.showCompleted = !S.showCompleted;
  const list = $('completedList');
  const actions = $('completedActions');
  const filter = $('completedFilter');
  const arrow = $('toggleArrow');
  list.classList.toggle('hidden', !S.showCompleted);
  actions.classList.toggle('hidden', !S.showCompleted);
  filter.classList.toggle('hidden', !S.showCompleted);
  arrow.classList.toggle('open', S.showCompleted);
  if (S.showCompleted) {
    $('filterDate').value = S.filterDate;
  }
});

$('filterDate').addEventListener('change', e => {
  S.filterDate = e.target.value;
  renderSidebar();
});

$('btnFilterClear').addEventListener('click', () => {
  S.filterDate = '';
  $('filterDate').value = '';
  renderSidebar();
});

$('btnDeleteBatch').addEventListener('click', () => {
  if (!S.sessions.completed.length) return;
  if (!confirm('완료된 매장 목록을 전체 삭제하시겠습니까?\n(복구할 수 없습니다)')) return;
  Bridge.send('deleteBatch', { ids: S.sessions.completed.map(s => s.id) });
});

// ── Header buttons ─────────────────────────────────
$('btnComplete').addEventListener('click', () => {
  const session = currentSession();
  if (!session) return;
  if (!confirm('작업을 완료로 표시하시겠습니까?')) return;
  Bridge.send('completeSession', { sessionId: session.id });
});

$('btnRestore').addEventListener('click', () => {
  const session = currentSession();
  if (!session) return;
  Bridge.send('restoreSession', { sessionId: session.id });
});

$('btnDelete').addEventListener('click', () => {
  const session = currentSession();
  if (!session) return;
  const name = session.data.basic.storeName || '새 매장';
  if (!confirm(`"${name}" 매장을 삭제하시겠습니까?`)) return;
  Bridge.send('deleteSession', { sessionId: session.id });
});

// ── Finish tab actions ─────────────────────────────
$('btnStartWork').addEventListener('click', () => {
  const now = new Date();
  const t = pad(now.getHours()) + ':' + pad(now.getMinutes());
  const sel = $('f-startTime');
  if (sel) { sel.value = t; sel.dispatchEvent(new Event('change')); }
  const session = currentSession();
  if (session) {
    session.data.basic.startTime = t;
    updateProgress(session);
    scheduleSave();
  }
  toast('작업 시작 시간: ' + t);
});

$('btnFinishWork').addEventListener('click', () => {
  const now = new Date();
  const t = pad(now.getHours()) + ':' + pad(now.getMinutes());
  const sel = $('f-endTime');
  if (sel) { sel.value = t; sel.dispatchEvent(new Event('change')); }
  const session = currentSession();
  if (session) {
    session.data.basic.endTime = t;
    const elapsed = calcElapsed(session.data.basic.startTime, t);
    session.data.basic.elapsedTime = elapsed;
    const ef = $('f-elapsedTime');
    if (ef) ef.value = elapsed;
    updateProgress(session);
    scheduleSave();
  }
  toast('작업 종료 시간: ' + t);
});

$('btnSendSms').addEventListener('click', () => {
  const session = currentSession();
  if (!session) return;
  const msg = session.data.finish.smsMessage || '';
  const phone = session.data.basic.engineerContact || '';
  if (!msg) { toast('문자 내용을 입력해주세요', 'warn'); return; }
  Bridge.send('sendSms', { phone, message: msg });
});

Bridge.on('smsSent', () => toast('문자 앱 열림 — 내용이 클립보드에 복사됐어요 (Ctrl+V)', 'success'));
Bridge.on('smsCopied', () => toast('문자 앱을 열 수 없어 클립보드에 복사했습니다', 'warn'));

$('btnSelectFiles').addEventListener('click', () => Bridge.send('selectFiles'));

$('btnRegister').addEventListener('click', () => {
  const session = currentSession();
  if (!session) return;
  const missing = getMissingFields(session.data);
  if (missing.length > 0) {
    const log = $('regLog');
    log.classList.add('visible');
    log.textContent = '⚠ 필수 항목을 먼저 입력해주세요:\n\n' + missing.map(m => '  • ' + m).join('\n');
    return;
  }
  const log = $('regLog');
  log.textContent = '';
  log.classList.add('visible');
  $('btnRegister').disabled = true;
  Bridge.send('startRegistration', { sessionId: session.id });
});

// ── Bridge listeners ───────────────────────────────
Bridge.on('sessions', msg => {
  S.sessions = { active: msg.active || [], completed: msg.completed || [] };
  renderSidebar();
  if (S.currentId) {
    const session = currentSession();
    if (session) { updateHeader(session); updateProgress(session); }
    else { S.currentId = null; hide('sessionView'); show('emptyState'); }
  }
});

Bridge.on('sessionAdded', msg => {
  S.sessions.active.push(msg.session);
  renderSidebar();
  selectSession(msg.session.id);
});

Bridge.on('sessionSaved', () => {
  renderSidebar();
});

Bridge.on('sessionDeleted', msg => {
  S.sessions.active = S.sessions.active.filter(s => s.id !== msg.id);
  S.sessions.completed = S.sessions.completed.filter(s => s.id !== msg.id);
  if (S.currentId === msg.id) {
    S.currentId = null;
    hide('sessionView');
    show('emptyState');
  }
  renderSidebar();
  toast('삭제되었습니다');
});

Bridge.on('sessionCompleted', msg => {
  const session = S.sessions.active.find(s => s.id === msg.id);
  if (session) {
    session.status = '완료';
    S.sessions.active = S.sessions.active.filter(s => s.id !== msg.id);
    S.sessions.completed.unshift(session);
  }
  renderSidebar();
  if (S.currentId === msg.id) updateHeader(currentSession());
  toast('완료 처리되었습니다', 'success');
});

Bridge.on('sessionRestored', msg => {
  const session = S.sessions.completed.find(s => s.id === msg.id);
  if (session) {
    session.status = '작성중';
    S.sessions.completed = S.sessions.completed.filter(s => s.id !== msg.id);
    S.sessions.active.push(session);
  }
  renderSidebar();
  if (S.currentId === msg.id) updateHeader(currentSession());
  toast('복원되었습니다');
});

Bridge.on('sessionsReordered', () => {
  // already updated locally
});

Bridge.on('filesSelected', msg => {
  const session = currentSession();
  if (!session) return;
  const paths = session.data.basic.attachmentPaths || [];
  (msg.paths || []).forEach(p => { if (!paths.includes(p)) paths.push(p); });
  session.data.basic.attachmentPaths = paths;
  renderAttachments(paths);
  scheduleSave();
  toast(msg.paths.length + '개 파일 추가됨');
});

Bridge.on('registrationProgress', msg => {
  const log = $('regLog');
  if (log) { log.textContent += msg.message + '\n'; log.scrollTop = log.scrollHeight; }
});

Bridge.on('registrationResult', msg => {
  $('btnRegister').disabled = false;
  const log = $('regLog');
  if (msg.success) {
    if (log) { log.textContent += '\n✔ 완료\n'; log.scrollTop = log.scrollHeight; }
    const session = currentSession();
    if (session) showRegCompleteModal(session);
    else toast('자동 등록 완료!', 'success');
  } else {
    toast('등록 실패: ' + (msg.message || '알 수 없는 오류'), 'error');
    if (log) { log.textContent += '\n✖ 실패: ' + msg.message + '\n'; log.scrollTop = log.scrollHeight; }
  }
});

function buildKakaoMsg(session) {
  const d = session.data;
  const name = d.basic.storeName || '(매장명 없음)';
  const wifi = d.checklist.wifiStatus || '-';
  const coupon = d.checklist.checkCoupon || '-';
  const issue = (d.finish.installIssue || '').trim() || '없음';
  return [
    `[ ${name} ]`,
    '',
    '- 연동완료',
    `- 네트워크 상태 ${wifi}`,
    `- 쿠폰 ${coupon}`,
    `- 매장 특이사항 ${issue}`,
  ].join('\n');
}

function showRegCompleteModal(session) {
  $('regCompleteMsg').value = buildKakaoMsg(session);
  $('regCompleteOverlay').classList.remove('hidden');
}

$('btnCopyMsg').addEventListener('click', () => {
  const ta = $('regCompleteMsg');
  navigator.clipboard.writeText(ta.value).then(() => {
    $('btnCopyMsg').textContent = '복사됨';
    setTimeout(() => { $('btnCopyMsg').textContent = '복사'; }, 1500);
  }).catch(() => {
    ta.select();
    document.execCommand('copy');
  });
});

$('btnCompleteYes').addEventListener('click', () => {
  $('regCompleteOverlay').classList.add('hidden');
  const session = currentSession();
  if (session) Bridge.send('completeSession', { sessionId: session.id });
});

$('btnCompleteNo').addEventListener('click', () => {
  $('regCompleteOverlay').classList.add('hidden');
});


Bridge.on('error', msg => toast(msg.message, 'error'));

// ── Public API ─────────────────────────────────────
window.App = {
  addSession() { Bridge.send('addSession'); },
  callPhone(inputId) {
    const inp = $(inputId);
    if (!inp || !inp.value) { toast('번호를 입력해주세요', 'warn'); return; }
    Bridge.send('callPhone', { number: inp.value });
  },
};

// ── Sidebar resize ─────────────────────────────────
(function() {
  const resizer = document.querySelector('.sidebar-resizer');
  const sidebar = document.querySelector('.sidebar');
  if (!resizer || !sidebar) return;
  let startX, startW;
  resizer.addEventListener('mousedown', e => {
    startX = e.clientX;
    startW = sidebar.offsetWidth;
    resizer.classList.add('dragging');
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
    e.preventDefault();
  });
  function onMove(e) {
    const w = Math.max(200, Math.min(500, startW + e.clientX - startX));
    sidebar.style.width = w + 'px';
  }
  function onUp() {
    resizer.classList.remove('dragging');
    document.removeEventListener('mousemove', onMove);
    document.removeEventListener('mouseup', onUp);
  }
})();

// ── 체크리스트 전체 O ────────────────────────────────
$('btnAllO').addEventListener('click', () => {
  if (_loading) return;
  const session = currentSession();
  if (!session) return;
  document.querySelectorAll('#page3 .ox-group').forEach(og => {
    const p = og.dataset.path;
    if (!p) return;
    setPath(session.data, p, 'O');
    og.querySelectorAll('.ox-btn').forEach(b => b.classList.toggle('sel', b.dataset.val === 'O'));
    if (p === 'checklist.checkCoupon') toggleCouponReason('O', false);
  });
  updateProgress(session);
  scheduleSave();
  toast('체크리스트 전체 O 설정됨', 'success');
});

// ── Init ───────────────────────────────────────────
(function init() {
  buildTimeDatelist();
  initRouterAutocomplete();
  Bridge.send('getSessions');
})();

})();
