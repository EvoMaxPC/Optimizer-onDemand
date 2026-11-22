import { NavLink } from 'react-router-dom';
import { useI18n } from '../../i18n';

export function Sidebar() {
  const { t } = useI18n();

  return (
    <aside className="sidebar">
      <div className="sidebar-header">
        <span className="sidebar-brand">{t.app.brand}</span>
        <span className="sidebar-subtitle">{t.app.subtitle}</span>
      </div>
      <nav className="sidebar-nav" aria-label={t.app.navAriaLabel}>
        <NavLink
          to="/"
          end
          className={({ isActive }) => (isActive ? 'sidebar-link sidebar-link-active' : 'sidebar-link')}
        >
          {t.nav.overview}
        </NavLink>
        <NavLink
          to="/modules"
          className={({ isActive }) => (isActive ? 'sidebar-link sidebar-link-active' : 'sidebar-link')}
        >
          {t.nav.modules}
        </NavLink>
        <NavLink
          to="/history"
          className={({ isActive }) => (isActive ? 'sidebar-link sidebar-link-active' : 'sidebar-link')}
        >
          {t.nav.history}
        </NavLink>
        <NavLink
          to="/settings"
          className={({ isActive }) => (isActive ? 'sidebar-link sidebar-link-active' : 'sidebar-link')}
        >
          {t.nav.settings}
        </NavLink>
      </nav>
    </aside>
  );
}
