import { useMemo } from 'react';
import { RouterProvider } from 'react-router-dom';
import { createAppRouter } from './app/routes';
import { I18nProvider, type Locale } from './i18n';

interface AppProps {
  initialLocale?: Locale;
}

export function App({ initialLocale = 'es' }: AppProps) {
  const router = useMemo(() => createAppRouter(), []);

  return (
    <I18nProvider initialLocale={initialLocale}>
      <RouterProvider router={router} />
    </I18nProvider>
  );
}

export default App;
