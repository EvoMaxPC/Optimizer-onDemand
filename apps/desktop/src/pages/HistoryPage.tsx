import { useEffect, useState } from 'react';
import { useI18n } from '../i18n';
import { listSnapshots, rollbackAction, type SnapshotEntry } from '../engine/engineStatus';

export function HistoryPage() {
  const { t } = useI18n();
  const [snapshots, setSnapshots] = useState<SnapshotEntry[]>([]);
  const [rollbackStatus, setRollbackStatus] = useState<Record<string, string>>({});

  useEffect(() => {
    let active = true;
    listSnapshots()
      .then((items) => {
        if (active) setSnapshots(items);
      })
      .catch(() => {
        if (active) setSnapshots([]);
      });
    return () => {
      active = false;
    };
  }, []);

  const handleRollback = async (actionId: string) => {
    setRollbackStatus((prev) => ({ ...prev, [actionId]: 'rolling back...' }));
    try {
      const res = await rollbackAction(actionId);
      setRollbackStatus((prev) => ({ ...prev, [actionId]: res.status }));
    } catch (err) {
      setRollbackStatus((prev) => ({
        ...prev,
        [actionId]: err instanceof Error ? err.message : String(err),
      }));
    }
  };

  return (
    <section className="page-section" aria-labelledby="history-heading">
      <header className="page-header">
        <h1 id="history-heading" className="page-title">
          {t.history.title}
        </h1>
      </header>

      {snapshots.length === 0 ? (
        <div className="empty-state-card">
          <h2 className="section-subtitle">{t.history.emptyTitle}</h2>
          <p className="body-text">{t.history.emptyBody}</p>
          <p className="body-text-secondary">{t.history.guaranteeNote}</p>
        </div>
      ) : (
        <div className="card">
          <ul>
            {snapshots.map((snap) => (
              <li key={snap.actionId}>
                <strong>{snap.actionId}</strong> ({snap.primitive}) — <span>{snap.target}</span>{' '}
                <small>{snap.capturedAtUtc}</small>
                <div>
                  <button type="button" onClick={() => handleRollback(snap.actionId)}>
                    Revertir
                  </button>{' '}
                  {rollbackStatus[snap.actionId] && (
                    <span className="badge">{rollbackStatus[snap.actionId]}</span>
                  )}
                </div>
              </li>
            ))}
          </ul>
        </div>
      )}
    </section>
  );
}
