(function () {
  'use strict';

  /* ---------- Scroll-spy: highlight the active sidebar link ---------- */
  var ids = ['overview', 'install', 'config', 'service', 'prometheus'];
  var links = {};
  ids.forEach(function (id) {
    var l = document.querySelector('.side-link[data-target="' + id + '"]');
    if (l) links[id] = l;
  });

  var current = null;
  var raf = null;

  function setActive(id) {
    if (id === current) return;
    current = id;
    Object.keys(links).forEach(function (key) {
      links[key].classList.toggle('is-active', key === id);
    });
  }

  function onScroll() {
    if (raf) return;
    raf = requestAnimationFrame(function () {
      raf = null;
      var cur = ids[0];
      ids.forEach(function (id) {
        var el = document.getElementById(id);
        if (el && el.getBoundingClientRect().top <= 140) cur = id;
      });
      setActive(cur);
    });
  }

  window.addEventListener('scroll', onScroll, { passive: true });
  onScroll();

  /* ---------- Tab groups (install + service) ---------- */
  document.querySelectorAll('[data-tabs]').forEach(function (group) {
    var buttons = group.querySelectorAll('.tab-btn');
    var panels = document.querySelectorAll('[data-tabgroup="' + group.getAttribute('data-tabs') + '"]');
    buttons.forEach(function (btn) {
      btn.addEventListener('click', function () {
        var key = btn.getAttribute('data-tab');
        buttons.forEach(function (b) {
          b.classList.toggle('is-active', b === btn);
        });
        panels.forEach(function (p) {
          p.classList.toggle('is-active', p.getAttribute('data-tabpanel') === key);
        });
      });
    });
  });

  /* ---------- Copy buttons ---------- */
  document.querySelectorAll('.copy-btn').forEach(function (btn) {
    btn.addEventListener('click', function () {
      var box = btn.closest('[data-codeblock]');
      var pre = box && box.querySelector('pre');
      if (pre && navigator.clipboard) {
        navigator.clipboard.writeText(pre.innerText).catch(function () {});
      }
      var old = btn.textContent;
      btn.textContent = 'Copied';
      setTimeout(function () { btn.textContent = old; }, 1300);
    });
  });
})();
