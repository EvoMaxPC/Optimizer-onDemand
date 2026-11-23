import { useI18n } from '../i18n';

export function HistoryPage() {
  const { t } = useI18n();

  return (
    <section className="page-section" aria-labelledby="history-heading">
      <header className="page-header">
        <h1 id="history-heading" className="page-title">
          {t.history.title}
        </h1>
      </header>

      <div className="empty-state-card">
        <h2 className="section-subtitle">{t.history.emptyTitle}</h2>
        <p className="body-text">{t.history.emptyBody}</p>
        <p className="body-text-secondary">{t.history.guaranteeNote}</p>
      </div>
    </section>
  );
}
