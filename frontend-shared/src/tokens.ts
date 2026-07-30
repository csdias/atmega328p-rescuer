/**
 * The bench palette and measurements, as values. They are the same ones as in
 * ATMegaPestaV1/Themes/LightTheme.xaml — the React front end does not invent a new look,
 * it uses the WPF's so whoever moves from one to the other recognises the bench.
 *
 * They stay as values, and not as CSS, because React Native has no stylesheets: the web
 * turns them into custom properties, native turns them into a StyleSheet.
 */
export const colours = {
  windowBackground: '#F5F5F5',
  headerBackground: '#E0E0E0',
  menuBackground: '#E8E8E8',
  cardBackground: '#FFFFFF',
  subCardBackground: '#F0F0F0',
  inputBackground: '#EBEBEB',
  separator: '#CCCCCC',

  textTitle: '#1A1A1A',
  textNormal: '#444444',
  textSubtle: '#888888',
  textDisabled: '#BBBBBB',

  menuAccent: '#555555',

  btnDark: '#1A1A1A',
  btnDarkHover: '#333333',
  btnDarkText: '#FFFFFF',

  btnLight: '#DDDDDD',
  btnLightHover: '#CCCCCC',
  btnLightText: '#1A1A1A',

  btnDisabled: '#CCCCCC',
  btnDisabledText: '#999999',

  pin: '#DDDDDD',
  pinText: '#1A1A1A',

  tag: '#E2E2E2',
  tagText: '#333333',

  badge: '#6B7280',
  badgeText: '#FFFFFF',

  progressBackground: '#DDDDDD',
  progressBar: '#1A1A1A',

  error: '#D9222B',
  success: '#2D7A4F',
  warning: '#D97706',
  idle: '#CCCCCC',
} as const;

/** The colour of each indicator state — what the WPF LEDs painted. */
export const ledColours = {
  idle: colours.idle,
  ok: colours.success,
  warning: colours.warning,
  error: colours.error,
} as const;

export const radii = {
  tiny: 2,
  small: 4,
  medium: 6,
  large: 8,
  huge: 12,
  pill: 11,
} as const;

export const spacing = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 24,
  xxl: 32,
} as const;

export const typography = {
  family: '"Segoe UI", system-ui, -apple-system, sans-serif',
  // For the fuses and the signature: hexadecimal lines up better at a fixed width.
  familyMono: '"Cascadia Mono", "Consolas", ui-monospace, monospace',
} as const;
