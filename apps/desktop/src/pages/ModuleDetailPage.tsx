import { Link, useParams } from 'react-router-dom';
import { useI18n } from '../i18n';
import { engineStatus } from '../engine/engineStatus';
import { loadModulesManifest } from '../modules/loader';

interface ModuleDetailPageProps {
  manifestOverride?: unknown;
}

export function ModuleDetailPage({ manifestOverride }: ModuleDetailPageProps) {
  const { moduleId } = useParams();
  const { t } = useI18n();

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
