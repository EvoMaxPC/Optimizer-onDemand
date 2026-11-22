import { useI18n } from '../i18n';

export function SettingsPage() {
  const { locale, setLocale, t } = useI18n();

  return (
    <section className="page-section" aria-labelledby="settings-heading">
      <header className="page-header">
        <h1 id="settings-heading" className="page-title">
          {t.settings.title}
        </h1>
      </header>

      <div className="settings-card">
        <h2 className="section-subtitle">{t.settings.languageSectionTitle}</h2>
        <p className="body-text">{t.settings.languageDescription}</p>

        <fieldset className="language-fieldset">
          <legend className="sr-only">{t.settings.languageSectionTitle}</legend>
          <label className="language-option">
            <input
              type="radio"
              name="language"
              value="es"
              checked={locale === 'es'}
              onChange={() => setLocale('es')}
            />
            <span>{t.settings.languageEs}</span>
          </label>
          <label className="language-option">
            <input
              type="radio"
              name="language"
              value="en"
              checked={locale === 'en'}
              onChange={() => setLocale('en')}
            />
            <span>{t.settings.languageEn}</span>
          </label>
        </fieldset>
      </div>

      <div className="settings-card">
        <h2 className="section-subtitle">{t.settings.systemInfoTitle}</h2>
        <dl className="status-list">
          <div className="status-row">
            <dt className="status-term">{t.settings.stageLabel}</dt>
            <dd className="status-detail">{t.settings.stageValue}</dd>
          </div>
          <div className="status-row">
            <dt className="status-term">{t.settings.engineLabel}</dt>
            <dd className="status-detail">{t.settings.engineValue}</dd>
          </div>
          <div className="status-row">
            <dt className="status-term">{t.settings.storageLabel}</dt>
            <dd className="status-detail">{t.settings.storageValue}</dd>
          </div>
          <div className="status-row">
            <dt className="status-term">{t.settings.windowsAccessLabel}</dt>
            <dd className="status-detail">{t.settings.windowsAccessValue}</dd>
          </div>
        </dl>
      </div>
    </section>
  );
}
