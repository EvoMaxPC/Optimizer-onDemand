import { describe, expect, it, beforeEach } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { App } from '../App';

const FORBIDDEN_VIEW_PATTERNS = [
  /\bMB\b/,
  /\bGB\b/,
  /%/,
  /demostraci[oó]n/i,
  /\bOptimizar\b/i,
  /\bLimpiar\b/i,
  /\bAplicar\b/i,
  /\bAnalizar\b/i,
  /\bEscanear\b/i,
  /\bReparar\b/i,
  /\bOptimize\b/i,
  /\bClean\b/i,
  /\bApply\b/i,
  /\bScan\b/i,
  /\bRepair\b/i
];

function assertHonestViewContent(container: HTMLElement) {
  const text = container.textContent ?? '';
  for (const pattern of FORBIDDEN_VIEW_PATTERNS) {
    expect(text).not.toMatch(pattern);
  }
  const buttons = container.querySelectorAll('button');
  expect(buttons.length).toBe(0);
}

describe('EVO Desktop Shell (Etapa 2 / F1a)', () => {
  beforeEach(() => {
    window.location.hash = '#/';
  });

  it('renderiza / en español con título, lema, 5 pilares y barra de estado honesta', () => {
    const { container } = render(<App />);

    expect(
      screen.getByRole('heading', { name: 'Resumen arquitectónico del shell' })
    ).toBeInTheDocument();
    expect(
      screen.getByText('Si no se puede explicar, medir, verificar y revertir, no se ejecuta.')
    ).toBeInTheDocument();

    expect(screen.getByRole('heading', { name: 'Explicable' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Medible' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Verificable' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Reversible' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Adaptativo' })).toBeInTheDocument();

    const statusBar = screen.getByRole('status');
    expect(within(statusBar).getByText('Motor no conectado')).toBeInTheDocument();
    expect(
      within(statusBar).getByText('Esta versión no puede modificar Windows')
    ).toBeInTheDocument();

    assertHonestViewContent(container);
  });

  it('navega por las 3 vistas reales sin botones de optimización ni unidades MB, GB o %', async () => {
    const user = userEvent.setup();
    const { container } = render(<App />);

    assertHonestViewContent(container);

    await user.click(screen.getByRole('link', { name: 'Historial' }));
    expect(
      screen.getByRole('heading', { name: 'Historial de acciones y evidencias' })
    ).toBeInTheDocument();
    expect(screen.getByText(/llegan en la Etapa 5 \(F3\)/)).toBeInTheDocument();
    assertHonestViewContent(container);

    await user.click(screen.getByRole('link', { name: 'Ajustes' }));
    expect(screen.getByRole('heading', { name: 'Ajustes del shell' })).toBeInTheDocument();
    assertHonestViewContent(container);
  });

  it('cambia el idioma a inglés en /settings y actualiza la interfaz y la barra de estado', async () => {
    const user = userEvent.setup();
    const { container } = render(<App />);

    await user.click(screen.getByRole('link', { name: 'Ajustes' }));
    const englishRadio = screen.getByRole('radio', { name: 'Inglés (en)' });
    await user.click(englishRadio);

    expect(screen.getByRole('heading', { name: 'Shell settings' })).toBeInTheDocument();

    const statusBar = screen.getByRole('status');
    expect(within(statusBar).getByText('Engine not connected')).toBeInTheDocument();
    expect(
      within(statusBar).getByText('This build cannot modify Windows')
    ).toBeInTheDocument();

    await user.click(screen.getByRole('link', { name: 'Overview' }));
    expect(
      screen.getByText(
        'If it cannot be explained, measured, verified, and reverted, it is not executed.'
      )
    ).toBeInTheDocument();

    assertHonestViewContent(container);
  });

  it('redirige una ruta inexistente a /', () => {
    window.location.hash = '#/ruta-inexistente-no-valida';
    render(<App />);

    expect(
      screen.getByRole('heading', { name: 'Resumen arquitectónico del shell' })
    ).toBeInTheDocument();
    expect(window.location.hash).toBe('#/');
  });
});
