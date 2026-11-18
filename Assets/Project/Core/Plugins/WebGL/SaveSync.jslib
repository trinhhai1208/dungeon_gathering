// Save trên itch.io (MODULES §10.2): đồng bộ IDBFS, flush khi rời trang, khóa một tab ghi.
mergeInto(LibraryManager.library, {
  DG_SyncFs: function (populate, callbackId, callback) {
    try {
      FS.syncfs(populate !== 0, function (err) {
        if (err) console.warn('[Save] syncfs', err);
        if (callbackId !== 0) {{{ makeDynCall('vii', 'callback') }}}(callbackId, err ? 0 : 1);
      });
    } catch (e) {
      console.warn('[Save] syncfs exception', e);
      if (callbackId !== 0) {{{ makeDynCall('vii', 'callback') }}}(callbackId, 0);
    }
  },

  // hideCallback: tab bị ẩn / rời trang (flush). showCallback: tab hiện lại (kiểm tra lại khóa ghi).
  DG_RegisterPageHide: function (hideCallback, showCallback) {
    var last = 0;
    var fireHide = function () {
      // visibilitychange(hidden) + pagehide thường bắn liền nhau → gộp để không chạy 2 syncfs song song.
      var now = Date.now();
      if (now - last < 250) return;
      last = now;
      {{{ makeDynCall('v', 'hideCallback') }}}();
    };
    var fireShow = function () {
      if (showCallback) {{{ makeDynCall('v', 'showCallback') }}}();
    };
    document.addEventListener('visibilitychange', function () {
      if (document.visibilityState === 'hidden') fireHide();
      else if (document.visibilityState === 'visible') fireShow();
    });
    window.addEventListener('pagehide', fireHide);
    window.addEventListener('pageshow', function (ev) { if (ev && ev.persisted) fireShow(); });
  },

  // Trả 1 nếu tab này giữ được khóa ghi save (heartbeat mỗi 2s, hết hạn sau 6s).
  // Khóa chỉ được giành MỘT lần lúc khởi động: tab đã mất khóa (tab khác giành khi tab này bị throttle) không
  // bao giờ ghi lại nữa — dữ liệu trong bộ nhớ của nó có thể đã cũ hơn save của tab kia.
  DG_AcquireTabLock: function () {
    var KEY = 'dg-owner';
    var me = Math.random().toString(36).slice(2);
    var now = Date.now();
    var st = (window.__dgTabLock = window.__dgTabLock || { id: me, owned: false, lost: false, timer: 0 });
    st.id = me;
    try {
      var raw = localStorage.getItem(KEY);
      if (raw) {
        var cur = JSON.parse(raw);
        if (cur && cur.id !== me && now - cur.ts < 6000) return 0;
      }
      var beat = function () {
        if (st.lost) return;
        try {
          var c = JSON.parse(localStorage.getItem(KEY) || 'null');
          // Tab khác đang giữ khóa còn hạn → mình đã mất khóa (heartbeat bị trình duyệt throttle khi ẩn).
          if (c && c.id !== st.id && Date.now() - c.ts < 6000) {
            st.lost = true;
            st.owned = false;
            if (st.timer) clearInterval(st.timer);
            console.warn('[Save] Tab khác đã giành khóa ghi save — tab này ngừng ghi.');
            return;
          }
          localStorage.setItem(KEY, JSON.stringify({ id: st.id, ts: Date.now() }));
        } catch (e) {}
      };
      st.owned = true;
      localStorage.setItem(KEY, JSON.stringify({ id: me, ts: now }));
      st.timer = setInterval(beat, 2000);
      window.addEventListener('pagehide', function () {
        try {
          var c = JSON.parse(localStorage.getItem(KEY));
          if (c && c.id === st.id) localStorage.removeItem(KEY);
        } catch (e) {}
      });
      window.addEventListener('pageshow', function (ev) { if (ev && ev.persisted) beat(); });
      return 1;
    } catch (e) {
      st.owned = true;
      st.blocked = true;
      return 1; // localStorage bị chặn: vẫn cho ghi
    }
  },

  // Kiểm tra lại khóa trước mỗi lần ghi / khi tab hiện lại. Trả 1 nếu vẫn giữ khóa (và gia hạn), 0 nếu đã mất.
  DG_CheckTabLock: function () {
    var KEY = 'dg-owner';
    var st = window.__dgTabLock;
    if (!st || !st.owned || st.lost) return 0;
    if (st.blocked) return 1;
    try {
      var c = JSON.parse(localStorage.getItem(KEY) || 'null');
      if (c && c.id !== st.id && Date.now() - c.ts < 6000) {
        st.lost = true;
        st.owned = false;
        if (st.timer) clearInterval(st.timer);
        return 0;
      }
      localStorage.setItem(KEY, JSON.stringify({ id: st.id, ts: Date.now() }));
      return 1;
    } catch (e) {
      return 1;
    }
  },

  // Boot lỗi không phục hồi được trong phiên (Addressables init) → THỬ LẠI = tải lại trang.
  DG_ReloadPage: function () {
    try { window.location.reload(); } catch (e) { console.warn('[Boot] reload', e); }
  }
});
