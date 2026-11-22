import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { createMemoryRouter, RouterProvider } from 'react-router-dom';
import { I18nProvider } from '../i18n';
import es from '../i18n/locales/es.json';
import en from '../i18n/locales/en.json';
import { routeConfig } from '../app/routes';
import { loadModulesManifest } from '../modules/loader';
import { ModulesPage } from '../pages/ModulesPage';

function renderAt(path: string) {
  const router = createMemoryRouter(routeConfig, { initialEntries: [path] });
  return render(
    <I18nProvider>
      <RouterProvider router={router} />
    </I18nProvider>
  );
}

describe('Etapa 3 (F1b): Módulos por manifiesto', () => {
  it('el manifiesto tiene 6 módulos y /modules renderiza 6 tarjetas sin botones de acción', () => {
    const manifest = loadModulesManifest();
    expect(manifest.modules).toHaveLength(6);

    const { container } = renderAt('/modules');
    expect(container.querySelectorAll('.module-card')).toHaveLength(6);
    expect(screen.getAllByText('No disponible').length).toBe(6);
    expect(container.querySelectorAll('button')).toHaveLength(0);
  });

  it('cada módulo y las 17 categorías tienen claves i18n en es y en', () => {
    const manifest = loadModulesManifest();
    for (const mod of manifest.modules) {
      expect(es.modules.items[mod.id].name).toBeTruthy();
      expect(es.modules.items[mod.id].description).toBeTruthy();
      expect(en.modules.items[mod.id].name).toBeTruthy();
      expect(en.modules.items[mod.id].description).toBeTruthy();
    }
    for (let c = 1; c <= 17; c++) {
      const k = String(c) as keyof typeof es.modules.categories;
      expect(es.modules.categories[k]).toBeTruthy();
      expect(en.modules.categories[k]).toBeTruthy();
    }
  });

  it('/modules/<id> renderiza su detalle con "No disponible" y /modules/inexistente muestra "Módulo desconocido"', () => {
    const manifest = loadModulesManifest();
    for (const mod of manifest.modules) {
      const { unmount } = renderAt(`/modules/${mod.id}`);
      expect(screen.getByText('No disponible: motor no conectado')).toBeInTheDocument();
      expect(screen.getByText('EVO prepara y propone; con tu autorización, aplica.')).toBeInTheDocument();
      unmount();
    }

    renderAt('/modules/inexistente');
    expect(screen.getByRole('heading', { name: 'Módulo desconocido' })).toBeInTheDocument();
  });

  it('el texto renderizado de lista y detalles no contiene métricas ni palabras prohibidas', () => {
    const { container } = renderAt('/modules');
    const text = container.textContent || '';
    expect(text).not.toMatch(/\d+\s?%/);
    expect(text).not.toMatch(/\b\d+\s?(ms|MB|GB|s)\b/);
    expect(text.toLowerCase()).not.toMatch(/optimizado|protegido|activo|demostración estructural/);
  });

  it('manifiesto inválido renderiza pantalla de error honesta', () => {
    render(
      <I18nProvider>
        <ModulesPage manifestOverride={{ schemaVersion: 99, modules: [] }} />
      </I18nProvider>
    );
    expect(screen.getByRole('alert')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Error de manifiesto de módulos' })).toBeInTheDocument();
  });
});
