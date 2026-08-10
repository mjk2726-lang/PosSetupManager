/* C# ↔ JS bridge */
(function () {
  const _listeners = {};

  window.Bridge = {
    send(action, payload) {
      const msg = Object.assign({ action }, payload || {});
      window.chrome.webview.postMessage(JSON.stringify(msg));
    },
    on(type, fn) {
      (_listeners[type] || (_listeners[type] = [])).push(fn);
    },
    off(type, fn) {
      if (_listeners[type])
        _listeners[type] = _listeners[type].filter(f => f !== fn);
    },
    emit(type, msg) {
      (_listeners[type] || []).forEach(fn => fn(msg));
    }
  };

  window.chrome.webview.addEventListener('message', e => {
    try {
      const msg = JSON.parse(e.data);
      Bridge.emit(msg.type, msg);
    } catch (err) {
      console.error('[Bridge] parse error', err);
    }
  });
})();
