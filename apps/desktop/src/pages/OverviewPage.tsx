import { useI18n } from '../i18n';

const PILLAR_KEYS = [
  'explainable',
  'measurable',
  'verifiable',
  'reversible',
  'adaptive'
] as const;

export function OverviewPage() {
  const { t } = useI18n();

  return (
    <section className="page-section" aria-labelledby="overview-heading">
      <header className="page-header">
        <h1 id="overview-heading" className="page-title">
          {t.overview.title}
        </h1>
      </header>

      <div className="motto-card">
        <p className="motto-label">{t.overview.mottoLabel}</p>
        <blockquote className="motto-quote">{t.overview.motto}</blockquote>
      </div>

      <div className="notice-card" role="note">
        <h2 className="section-subtitle">{t.overview.noticeTitle}</h2>
        <p className="body-text">{t.overview.noticeBody}</p>
        <p className="body-text-secondary">{t.overview.nextStagesNote}</p>
      </div>

      <section className="pillars-section" aria-labelledby="pillars-heading">
        <h2 id="pillars-heading" className="section-subtitle">
          {t.overview.pillarsTitle}
        </h2>
        <ul className="pillars-grid">
          {PILLAR_KEYS.map((key) => {
            const pillar = t.overview.pillars[key];
            return (
              <li key={key} className="pillar-card">
                <h3 className="pillar-title">{pillar.name}</h3>
                <p className="pillar-description">{pillar.description}</p>
              </li>
            );
          })}
        </ul>
      </section>
    </section>
  );
}
