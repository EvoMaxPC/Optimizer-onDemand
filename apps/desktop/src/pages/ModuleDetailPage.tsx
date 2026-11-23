import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useI18n } from '../i18n';
import {
  applyAction,
  engineStatus,
  listCatalogActions,
  rollbackAction,
  type CatalogAction,
} from '../engine/engineStatus';
import { loadModulesManifest } from '../modules/loader';

interface ModuleDetailPageProps {
  manifestOverride?: unknown;
}

export function ModuleDetailPage({ manifestOverride }: ModuleDetailPageProps) {
  const { moduleId } = useParams();
  const { t } = useI18n();
  const [actions, setActions] = useState<CatalogAction[]>([]);
  const [actionStatus, setActionStatus] = useState<Record<string, string>>({});

  useEffect(() => {
    if (!moduleId) return;
    let active = true;
    listCatalogActions(moduleId)
      .then((items) => {
        if (active) setActions(items);
      })
      .catch(() => {
        if (active) setActions([]);
      });
    return () => {
      active = false;
    };
  }, [moduleId]);

  const handleApply = async (id: string) => {
    setActionStatus((prev) => ({ ...prev, [id]: 'applying...' }));
    try {
      const res = await applyAction(id);
      setActionStatus((prev) => ({ ...prev, [id]: res.status }));
    } catch (err) {
      setActionStatus((prev) => ({
        ...prev,
        [id]: err instanceof Error ? err.message : String(err),
      }));
    }
  };

  const handleRollback = async (id: string) => {
    setActionStatus((prev) => ({ ...prev, [id]: 'rolling back...' }));
    try {
      const res = await rollbackAction(id);
      setActionStatus((prev) => ({ ...prev, [id]: res.status }));
    } catch (err) {
      setActionStatus((prev) => ({
        ...prev,
        [id]: err instanceof Error ? err.message : String(err),
      }));
    }
  };

  try {
    const manifest = loadModulesManifest(manifestOverride);
    const mod = manifest.modules.find((m) => m.id === moduleId);

    if (!mod) {
      return (
        <section className="page">
          <header className="page-header">
            <h1>{t.modules.unknownTitle}</h1>
            <p className="page-subtitle">{t.modules.unknownDescription}</p>
          </header>
          <p>
            <Link to="/modules">{t.modules.backToModules}</Link>
          </p>
        </section>
      );
    }

    return (
      <section className="page" data-engine-status={engineStatus}>
        <header className="page-header">
          <div className="module-card-header">
            <h1>{t.modules.items[mod.id].name}</h1>
            <span className="badge badge-muted">{t.modules.unavailableDetailBadge}</span>
          </div>
          <p className="page-subtitle">{t.modules.items[mod.id].description}</p>
        </header>

        <div className="card">
          <h2>{t.modules.categoriesLabel}</h2>
          <ul>
            {mod.categories.map((catId) => (
              <li key={catId}>{t.modules.categories[String(catId) as keyof typeof t.modules.categories]}</li>
            ))}
          </ul>
        </div>

        <div className="card">
          <h2>{t.modules.capabilitiesLabel}</h2>
          <ul>
            {mod.capabilities.map((cap) => (
              <li key={cap}>
                <span>{t.modules.capabilities[cap]}</span>{' '}
                <span className="badge badge-muted">{t.modules.plannedLabel}</span>
              </li>
            ))}
          </ul>
        </div>

        <p className="module-proposal-note">{t.modules.proposalPhrase}</p>

        {actions.length > 0 && (
          <div className="card">
            <h2>Acciones disponibles</h2>
            <ul>
              {actions.map((act) => (
                <li key={act.id}>
                  <strong>{act.title}</strong> — <span>{act.description}</span>
                  <div>
                    <button type="button" onClick={() => handleApply(act.id)}>
                      Aplicar
                    </button>{' '}
                    <button type="button" onClick={() => handleRollback(act.id)}>
                      Revertir
                    </button>{' '}
                    {actionStatus[act.id] && <span className="badge">{actionStatus[act.id]}</span>}
                  </div>
                </li>
              ))}
            </ul>
          </div>
        )}

        <p>
          <Link to="/modules">{t.modules.backToModules}</Link>
        </p>
      </section>
    );
  } catch {
    return (
      <section className="page" role="alert">
        <header className="page-header">
          <h1>{t.modules.manifestErrorTitle}</h1>
          <p className="page-subtitle">{t.modules.manifestErrorDescription}</p>
        </header>
      </section>
    );
  }
}
