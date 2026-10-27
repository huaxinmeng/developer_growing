export interface ThemeOption {
  id: string
  nameKey: string
  colors: [string, string, string]
  dark?: boolean
}

export const themes: ThemeOption[] = [
  { id: 'ledger', nameKey: 'theme.ledger', colors: ['#1c6b60', '#5fb3a5', '#e7ebe2'] },
  { id: 'ocean', nameKey: 'theme.ocean', colors: ['#2d8b8b', '#a8dadc', '#1a2332'], dark: true },
  { id: 'sunset', nameKey: 'theme.sunset', colors: ['#e76f51', '#f4a261', '#e9c46a'] },
  { id: 'forest', nameKey: 'theme.forest', colors: ['#2d4a2b', '#a4ac86', '#faf9f6'] },
  { id: 'minimal', nameKey: 'theme.minimal', colors: ['#36454f', '#708090', '#d3d3d3'] },
  { id: 'golden', nameKey: 'theme.golden', colors: ['#f4a900', '#c1666b', '#d4b896'] },
  { id: 'arctic', nameKey: 'theme.arctic', colors: ['#4a6fa5', '#a8dadc', '#d4e4f7'] },
  { id: 'desert', nameKey: 'theme.desert', colors: ['#b87d6d', '#d4a5a5', '#e8d5c4'] },
  { id: 'tech', nameKey: 'theme.tech', colors: ['#0066ff', '#00ffff', '#1e1e1e'], dark: true },
  { id: 'botanical', nameKey: 'theme.botanical', colors: ['#4a7c59', '#f9a620', '#f5f3ed'] },
  { id: 'galaxy', nameKey: 'theme.galaxy', colors: ['#a490c2', '#4a4e8f', '#2b1e3e'], dark: true }
]

export const defaultTheme = 'ledger'

const STORAGE_KEY = 'inventory-theme'

export function getInitialTheme(): string {
  const saved = localStorage.getItem(STORAGE_KEY)
  return saved && themes.some((theme) => theme.id === saved) ? saved : defaultTheme
}

export function applyTheme(id: string) {
  document.documentElement.dataset.theme = id
  localStorage.setItem(STORAGE_KEY, id)
}
