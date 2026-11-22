import { Link } from 'react-router-dom';
import { useI18n } from '../i18n';
import { engineStatus } from '../engine/engineStatus';
import { loadModulesManifest } from '../modules/loader';

interface ModulesPageProps {
  manifestOverride?: unknown;
}

export function ModulesPage({ manifestOverride }: ModulesPageProps) {
  const { t } = useI18n();

  try {
    const manifest = loadModulesManifest(manifestOverride);

    return (
      <section className="page">
        <header className="page-header">
          <h1>{t.modules.title}</h1>
          <p className="page-subtitle">{t.modules.subtitle}</p>
        </header>

        <div className="modules-grid" data-engine-status={engineStatus}>
          {manifest.modules.map((mod) => (
            <article key={mod.id} className="card module-card">
              <div className="module-card-header">
                <h2>
                  <Link to={`/modules/${mod.id}`}>{t.modules.items[mod.id].name}</Link>
                </h2>
                <span className="badge badge-muted">{t.modules.unavailableBadge}</span>
              </div>
              <p>{t.modules.items[mod.id].description}</p>
              <p className="module-status-note">{t.modules.motorDisconnected}</p>
            </article>
          ))}
        </div>
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
