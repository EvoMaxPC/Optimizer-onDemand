import { useI18n } from '../../i18n';
import { engineStatus } from '../../engine/engineStatus';

export function StatusBar() {
  const { t } = useI18n();

  return (
    <footer className="status-bar" role="status" data-engine-status={engineStatus}>
      <div className="status-bar-group">
        <span className="status-indicator" aria-hidden="true" />
        <span className="status-bar-item">{t.statusBar.engineDisconnected}</span>
        <span className="status-bar-separator" aria-hidden="true">
          ·
        </span>
        <span className="status-bar-item">{t.statusBar.cannotModifyWindows}</span>
      </div>
      <span className="status-bar-badge">{t.statusBar.stageBadge}</span>
    </footer>
  );
}
