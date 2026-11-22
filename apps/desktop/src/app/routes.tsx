import { createHashRouter, Navigate, type RouteObject } from 'react-router-dom';
import { AppShell } from '../components/layout/AppShell';
import { OverviewPage } from '../pages/OverviewPage';
import { HistoryPage } from '../pages/HistoryPage';
import { SettingsPage } from '../pages/SettingsPage';
import { ModulesPage } from '../pages/ModulesPage';
import { ModuleDetailPage } from '../pages/ModuleDetailPage';

export const routeConfig: RouteObject[] = [
  {
    path: '/',
    element: <AppShell />,
    children: [
      {
        index: true,
        element: <OverviewPage />
      },
      {
        path: 'modules',
        element: <ModulesPage />
      },
      {
        path: 'modules/:moduleId',
        element: <ModuleDetailPage />
      },
      {
        path: 'history',
        element: <HistoryPage />
      },
      {
        path: 'settings',
        element: <SettingsPage />
      },
      {
        path: '*',
        element: <Navigate to="/" replace />
      }
    ]
  }
];

export function createAppRouter() {
  return createHashRouter(routeConfig);
}
