import { useEffect, useState } from 'react';
import { useI18n } from '../../i18n';
import { engineStatus, probeEngine, type EngineSnapshot } from '../../engine/engineStatus';

export function StatusBar() {
  const { t } = useI18n();
  const [snapshot, setSnapshot] = useState<EngineSnapshot>({ status: engineStatus });

  useEffect(() => {
    let active = true;
    probeEngine().then((next) => {
      if (active) setSnapshot(next);
    });
    return () => {
      active = false;
    };
  }, []);

  const statusLabel =
    snapshot.status === 'connected' && snapshot.hello && snapshot.version
      ? `${snapshot.hello.engineName} v${snapshot.version.engineVersion}`
      : t.statusBar.engineDisconnected;

  return (
    <footer className="status-bar" role="status" data-engine-status={snapshot.status}>
      <div className="status-bar-group">
        <span className="status-indicator" aria-hidden="true" />
        <span className="status-bar-item">{statusLabel}</span>
        <span className="status-bar-separator" aria-hidden="true">
          ·
        </span>
        <span className="status-bar-item">
          {snapshot.osInfo ? snapshot.osInfo.osDescription : t.statusBar.cannotModifyWindows}
        </span>
      </div>
      <span className="status-bar-badge">{t.statusBar.stageBadge}</span>
    </footer>
  );
}
