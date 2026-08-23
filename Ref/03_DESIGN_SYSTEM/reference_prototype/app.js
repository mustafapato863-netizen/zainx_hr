/**
 * ZainX Workforce Design System — Master Controller v2.0
 * Handles site-wide theme, RTL, density, motion, spotlight tracking, interactive demos.
 */

document.addEventListener('DOMContentLoaded', () => {
  initTheme();
  initRTL();
  initDensity();
  initMotion();
  initSidebar();
  initSpotlightFollower();
  initActiveNavLink();
  initKeyboardShortcuts();
  initCopyTokens();
  initInteractiveDemos();
});

/* ── 1. Theme Controller ── */
function initTheme() {
  const btn = document.getElementById('themeToggle');
  const html = document.documentElement;
  
  // Restore saved theme or default to dark
  const saved = localStorage.getItem('zx-theme') || 'dark';
  html.setAttribute('data-theme', saved);
  updateThemeButton(btn, saved);

  if (btn) {
    btn.addEventListener('click', () => {
      const current = html.getAttribute('data-theme') || 'dark';
      const next = current === 'dark' ? 'light' : 'dark';
      html.setAttribute('data-theme', next);
      localStorage.setItem('zx-theme', next);
      updateThemeButton(btn, next);
    });
  }
}

function updateThemeButton(btn, theme) {
  if (!btn) return;
  const label = btn.querySelector('span');
  if (label) label.textContent = theme === 'dark' ? 'Dark' : 'Light';
}

/* ── 2. RTL / LTR Controller ── */
function initRTL() {
  const btn = document.getElementById('rtlToggle');
  const html = document.documentElement;
  
  const saved = localStorage.getItem('zx-dir') || 'ltr';
  html.setAttribute('dir', saved);
  updateRtlButton(btn, saved);

  if (btn) {
    btn.addEventListener('click', () => {
      const current = html.getAttribute('dir') || 'ltr';
      const next = current === 'ltr' ? 'rtl' : 'ltr';
      html.setAttribute('dir', next);
      localStorage.setItem('zx-dir', next);
      updateRtlButton(btn, next);
    });
  }
}

function updateRtlButton(btn, dir) {
  if (!btn) return;
  const label = btn.querySelector('span');
  if (label) label.textContent = dir.toUpperCase();
}

/* ── 3. Density Controller (Compact / Standard / Comfortable) ── */
function initDensity() {
  const btn = document.getElementById('densityToggle');
  const html = document.documentElement;
  
  const modes = ['compact', 'standard', 'comfortable'];
  let currentIdx = modes.indexOf(localStorage.getItem('zx-density') || 'compact');
  if (currentIdx === -1) currentIdx = 0;
  
  html.setAttribute('data-density', modes[currentIdx]);
  updateDensityButton(btn, modes[currentIdx]);

  if (btn) {
    btn.addEventListener('click', () => {
      currentIdx = (currentIdx + 1) % modes.length;
      const next = modes[currentIdx];
      html.setAttribute('data-density', next);
      localStorage.setItem('zx-density', next);
      updateDensityButton(btn, next);
    });
  }
}

function updateDensityButton(btn, mode) {
  if (!btn) return;
  const label = btn.querySelector('span');
  if (label) label.textContent = mode.charAt(0).toUpperCase() + mode.slice(1);
}

/* ── 4. Motion Controller (Full / Reduced) ── */
function initMotion() {
  const btn = document.getElementById('motionToggle');
  let isReduced = localStorage.getItem('zx-motion') === 'reduced';
  
  if (isReduced) {
    document.documentElement.classList.add('reduced-motion-forced');
  }
  updateMotionButton(btn, isReduced);

  if (btn) {
    btn.addEventListener('click', () => {
      isReduced = !isReduced;
      document.documentElement.classList.toggle('reduced-motion-forced', isReduced);
      localStorage.setItem('zx-motion', isReduced ? 'reduced' : 'full');
      updateMotionButton(btn, isReduced);
    });
  }
}

function updateMotionButton(btn, isReduced) {
  if (!btn) return;
  const label = btn.querySelector('span');
  if (label) label.textContent = isReduced ? 'Reduced' : 'Full';
}

/* ── 5. Responsive Sidebar Toggle ── */
function initSidebar() {
  const toggleBtn = document.getElementById('sidebarToggle');
  const sidebar = document.getElementById('dsSidebar');
  
  if (toggleBtn && sidebar) {
    toggleBtn.addEventListener('click', () => {
      sidebar.classList.toggle('open');
    });

    // Close when clicking outside on mobile
    document.addEventListener('click', (e) => {
      if (window.innerWidth <= 768 && sidebar.classList.contains('open')) {
        if (!sidebar.contains(e.target) && e.target !== toggleBtn && !toggleBtn.contains(e.target)) {
          sidebar.classList.remove('open');
        }
      }
    });
  }
}

/* ── 6. Spotlight Cursor Follower (Fine pointer only) ── */
function initSpotlightFollower() {
  const cards = document.querySelectorAll('.ds-card-spotlight, .zx-spotlight-card');
  
  cards.forEach(card => {
    card.addEventListener('mousemove', (e) => {
      const rect = card.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const y = e.clientY - rect.top;
      card.style.setProperty('--mouse-x', `${x}px`);
      card.style.setProperty('--mouse-y', `${y}px`);
    });
  });
}

/* ── 7. Active Nav Link Highlighting ── */
function initActiveNavLink() {
  const currentPath = window.location.pathname.replace(/\\/g, '/');
  const links = document.querySelectorAll('.ds-nav-link');
  
  links.forEach(link => {
    const href = link.getAttribute('href');
    if (!href) return;
    
    // Check if current URL ends with this href or matches page data attribute
    const isMatch = currentPath.endsWith(href) || 
      (href === 'index.html' && (currentPath.endsWith('/') || currentPath.endsWith('Design System/') || currentPath.endsWith('index.html')));
    
    if (isMatch) {
      links.forEach(l => l.classList.remove('active'));
      link.classList.add('active');
    }
  });
}

/* ── 8. Global Keyboard Shortcuts ── */
function initKeyboardShortcuts() {
  document.addEventListener('keydown', (e) => {
    // Cmd+K or Ctrl+K for search
    if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
      e.preventDefault();
      const searchInput = document.querySelector('.search-input, .ds-search-input');
      if (searchInput) {
        searchInput.focus();
      }
    }
    
    // Escape to close open overlays/sidebar
    if (e.key === 'Escape') {
      const sidebar = document.getElementById('dsSidebar');
      if (sidebar && sidebar.classList.contains('open')) {
        sidebar.classList.remove('open');
      }
    }
  });
}

/* ── 9. Copy Token Values on Click ── */
function initCopyTokens() {
  document.querySelectorAll('[data-copy]').forEach(elem => {
    elem.addEventListener('click', async () => {
      const textToCopy = elem.getAttribute('data-copy');
      if (!textToCopy) return;
      
      try {
        await navigator.clipboard.writeText(textToCopy);
        showToast(`Copied "${textToCopy}" to clipboard`);
      } catch (err) {
        console.warn('Clipboard write failed:', err);
      }
    });
  });
}

/* ── 10. Interactive Demos & Motion Replays ── */
function initInteractiveDemos() {
  // Brand animation replay
  document.querySelectorAll('[data-action="replay-brand"]').forEach(btn => {
    btn.addEventListener('click', () => {
      const target = document.querySelector(btn.getAttribute('data-target') || '.zx-brand-animated');
      if (target) {
        target.classList.remove('animating');
        void target.offsetWidth;
        target.classList.add('animating');
      }
    });
  });

  // Access gate scan replay
  document.querySelectorAll('[data-action="replay-scan"]').forEach(btn => {
    btn.addEventListener('click', () => {
      const target = document.querySelector(btn.getAttribute('data-target') || '.zx-access-gate');
      if (target) {
        target.classList.remove('scanning');
        void target.offsetWidth;
        target.classList.add('scanning');
      }
    });
  });

  // Success resolve replay
  document.querySelectorAll('[data-action="replay-success"]').forEach(btn => {
    btn.addEventListener('click', () => {
      const target = document.querySelector(btn.getAttribute('data-target') || '.zx-success-moment');
      if (target) {
        target.classList.remove('resolving');
        void target.offsetWidth;
        target.classList.add('resolving');
      }
    });
  });
}

/* ── Utility: Toast Message ── */
function showToast(message) {
  let toast = document.getElementById('dsGlobalToast');
  if (!toast) {
    toast = document.createElement('div');
    toast.id = 'dsGlobalToast';
    toast.style.cssText = `
      position: fixed;
      bottom: 24px;
      right: 24px;
      background: var(--zx-neutral-900);
      color: var(--zx-neutral-0);
      padding: 8px 16px;
      border-radius: 6px;
      font-size: 12px;
      font-family: var(--zx-font-mono);
      box-shadow: var(--zx-shadow-floating);
      z-index: 9999;
      opacity: 0;
      transform: translateY(8px);
      transition: all 200ms ease;
      pointer-events: none;
    `;
    document.body.appendChild(toast);
  }
  
  toast.textContent = message;
  toast.style.opacity = '1';
  toast.style.transform = 'translateY(0)';
  
  clearTimeout(toast._timeout);
  toast._timeout = setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transform = 'translateY(8px)';
  }, 2200);
}
