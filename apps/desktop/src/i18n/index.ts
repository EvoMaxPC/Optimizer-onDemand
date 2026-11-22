import { createContext, createElement, useContext, useMemo, useState, type ReactNode } from 'react';
import es from './locales/es.json';
import en from './locales/en.json';

export type Locale = 'es' | 'en';
export type Translations = typeof es;

export const locales: Record<Locale, Translations> = {
  es,
  en
};

interface I18nContextValue {
  locale: Locale;
  setLocale: (next: Locale) => void;
  t: Translations;
}

const I18nContext = createContext<I18nContextValue | undefined>(undefined);

interface I18nProviderProps {
  initialLocale?: Locale;
  children: ReactNode;
}

export function I18nProvider({ initialLocale = 'es', children }: I18nProviderProps) {
  const [locale, setLocale] = useState<Locale>(initialLocale);

  const value = useMemo<I18nContextValue>(
    () => ({
      locale,
      setLocale,
      t: locales[locale]
    }),
    [locale]
  );

  return createElement(I18nContext.Provider, { value }, children);
}

export function useI18n(): I18nContextValue {
  const context = useContext(I18nContext);
  if (!context) {
    throw new Error('useI18n must be used within an I18nProvider');
  }
  return context;
}
