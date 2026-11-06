/* ═══════════════════════════════════════════════════════════════════
   ARCHIVE PIECES — site.js
═══════════════════════════════════════════════════════════════════ */

document.addEventListener('DOMContentLoaded', () => {

  // ── Mobile drawer ─────────────────────────────────────────────────
  const mobileBtn     = document.getElementById('mobileMenuBtn');
  const mobileClose   = document.getElementById('mobileCloseBtn');
  const mobileDrawer  = document.getElementById('mobileDrawer');
  const mobileOverlay = document.getElementById('mobileOverlay');

  function openDrawer() {
    mobileDrawer?.classList.add('open');
    mobileOverlay?.classList.add('open');
    document.body.style.overflow = 'hidden';
  }
  function closeDrawer() {
    mobileDrawer?.classList.remove('open');
    mobileOverlay?.classList.remove('open');
    document.body.style.overflow = '';
  }

  mobileBtn?.addEventListener('click', openDrawer);
  mobileClose?.addEventListener('click', closeDrawer);
  mobileOverlay?.addEventListener('click', closeDrawer);

  // ── Cart count ───────────────────────────────────────────────────
  async function refreshCartCount() {
    try {
      const res = await fetch('/cart/count');
      if (res.ok) {
        const data = await res.json();
        const badge = document.getElementById('cartCount');
        if (badge) {
          badge.textContent = data.count;
          badge.style.display = data.count > 0 ? 'flex' : 'none';
        }
      }
    } catch (_) {}
  }
  refreshCartCount();

  // ── Auto-dismiss flash messages ──────────────────────────────────
  document.querySelectorAll('.ap-flash').forEach(el => {
    setTimeout(() => {
      el.style.transition = 'opacity .4s';
      el.style.opacity = '0';
      setTimeout(() => el.remove(), 400);
    }, 3500);
  });

  // ── Active nav link highlighting ─────────────────────────────────
  const currentPath = window.location.pathname;
  document.querySelectorAll('.ap-cat-link').forEach(link => {
    const href = link.getAttribute('href');
    if (href && currentPath.startsWith(href) && href !== '/') {
      link.classList.add('active');
    }
  });
});
