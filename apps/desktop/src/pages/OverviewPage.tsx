import { useEffect, useState } from 'react';
import { useI18n } from '../i18n';
import {
  applyAction,
  collectDiagnostics,
  executePlan,
  generatePlan,
  measureLab,
  type LabMeasurement,
  type OperationPlan,
  type SystemDiagnostics,
} from '../engine/engineStatus';

const PILLAR_KEYS = [
  'explainable',
  'measurable',
  'verifiable',
  'reversible',
  'adaptive'
] as const;

export function OverviewPage() {
  const { t } = useI18n();
  const [diag, setDiag] = useState<SystemDiagnostics | null>(null);
  const [profile, setProfile] = useState<'gaming' | 'balanced' | 'minimal'>('balanced');
  const [plan, setPlan] = useState<OperationPlan | null>(null);
  const [opStatus, setOpStatus] = useState<Record<string, string>>({});
  const [labBefore, setLabBefore] = useState<LabMeasurement | null>(null);
  const [labAfter, setLabAfter] = useState<LabMeasurement | null>(null);

  useEffect(() => {
    let active = true;
    collectDiagnostics()
      .then((d) => {
        if (active) setDiag(d);
      })
      .catch(() => {});
    return () => {
      active = false;
    };
  }, []);

  const handleGeneratePlan = async () => {
    const p = await generatePlan(profile);
    setPlan(p);
  };

  const handleApplyOp = async (actionId: string) => {
    setOpStatus((prev) => ({ ...prev, [actionId]: 'applying...' }));
    try {
      const res = await applyAction(actionId);
      setOpStatus((prev) => ({ ...prev, [actionId]: res.status }));
    } catch (err) {
      setOpStatus((prev) => ({
        ...prev,
        [actionId]: err instanceof Error ? err.message : String(err),
      }));
    }
  };

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

      {diag && (
        <>
          <section className="card" aria-label="Diagnóstico del sistema">
            <h2>Diagnóstico del sistema</h2>
            <p>
              <strong>SO:</strong> {diag.osDescription} ({diag.architecture}) ·{' '}
              <strong>CPUs:</strong> {diag.processorCount} ·{' '}
              <strong>Acciones aplicadas:</strong> {diag.appliedActionIds.length}
            </p>
            <ul>
              {diag.drives.map((d) => (
                <li key={d.name}>
                  Disco {d.name} ({d.format}): {Math.round(d.freeBytes / (1024 ** 3))} GB libres de{' '}
                  {Math.round(d.totalBytes / (1024 ** 3))} GB
                </li>
              ))}
            </ul>
          </section>

          <section className="card" aria-label="Adaptive Core">
        <h2>Adaptive Core — Plan de operaciones</h2>
        <div>
          <label htmlFor="evo-profile-select">Perfil: </label>
          <select
            id="evo-profile-select"
            value={profile}
            onChange={(e) => setProfile(e.target.value as 'gaming' | 'balanced' | 'minimal')}
          >
            <option value="gaming">Gaming</option>
            <option value="balanced">Balanced</option>
            <option value="minimal">Minimal</option>
          </select>{' '}
          <button type="button" onClick={handleGeneratePlan}>
            Generar plan
          </button>{' '}
          {plan && plan.totalRecommended > 0 && (
            <button
              type="button"
              onClick={async () => {
                const res = await executePlan(profile);
                if (res) {
                  setPlan(await generatePlan(profile));
                }
              }}
            >
              Ejecutar plan completo
            </button>
          )}
        </div>

        {plan && (
          <div>
            <p>
              Perfil <strong>{plan.profile}</strong>: {plan.totalRecommended} operaciones recomendadas.
            </p>
            <ul>
              {plan.operations.map((op) => (
                <li key={op.actionId}>
                  <strong>{op.title}</strong> ({op.moduleId}, riesgo {op.riskLevel}) — {op.reason}{' '}
                  {op.alreadyApplied ? (
                    <span className="badge">ya aplicada</span>
                  ) : (
                    <button type="button" onClick={() => handleApplyOp(op.actionId)}>
                      Aplicar
                    </button>
                  )}{' '}
                  {opStatus[op.actionId] && <span className="badge">{opStatus[op.actionId]}</span>}
                </li>
              ))}
            </ul>
          </div>
        )}
          </section>

          <section className="card" aria-label="Laboratorio EVO">
            <h2>Laboratorio EVO — Medición A/B</h2>
            <div>
              <button type="button" onClick={async () => setLabBefore(await measureLab(7))}>
                Medir Antes (A)
              </button>{' '}
              <button type="button" onClick={async () => setLabAfter(await measureLab(7))}>
                Medir Después (B)
              </button>
            </div>
            {labBefore && (
              <p>
                <strong>Antes (A):</strong> mediana {labBefore.medianMs} ms (mín {labBefore.minMs} / máx {labBefore.maxMs}, σ {labBefore.stdDevMs})
              </p>
            )}
            {labAfter && (
              <p>
                <strong>Después (B):</strong> mediana {labAfter.medianMs} ms (mín {labAfter.minMs} / máx {labAfter.maxMs}, σ {labAfter.stdDevMs})
              </p>
            )}
          </section>
        </>
      )}
    </section>
  );
}
