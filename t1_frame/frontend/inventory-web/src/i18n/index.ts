import { createI18n } from 'vue-i18n'
import zhCN from './messages/zh-CN'
import enUS from './messages/en-US'

export type Locale = 'zh-CN' | 'en-US'

export const supportedLocales: Locale[] = ['zh-CN', 'en-US']

const STORAGE_KEY = 'inventory-locale'

function getInitialLocale(): Locale {
  const saved = localStorage.getItem(STORAGE_KEY)
  if (saved === 'zh-CN' || saved === 'en-US') {
    return saved
  }

  return navigator.language.toLowerCase().startsWith('zh') ? 'zh-CN' : 'en-US'
}

const initialLocale = getInitialLocale()

export const i18n = createI18n({
  legacy: false,
  locale: initialLocale,
  fallbackLocale: 'zh-CN',
  messages: {
    'zh-CN': zhCN,
    'en-US': enUS
  }
})

document.documentElement.lang = initialLocale

export function getLocale(): Locale {
  return i18n.global.locale.value as Locale
}

export function setLocale(locale: Locale) {
  i18n.global.locale.value = locale
  localStorage.setItem(STORAGE_KEY, locale)
  document.documentElement.lang = locale
}

export function translate(key: string, params?: Record<string, unknown>): string {
  return i18n.global.t(key, params ?? {}) as string
}
