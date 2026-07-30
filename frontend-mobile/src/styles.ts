import { StyleSheet } from 'react-native';
import { colours, ledColours, radii } from '@atmegapesta/shared';

/**
 * The native styles, built from the same tokens the web front end uses. This is why
 * `tokens.ts` holds values and not CSS: here it feeds a StyleSheet with no conversion.
 */
export { colours, ledColours };

export const estilos = StyleSheet.create({
  screen: {
    flex: 1,
    backgroundColor: colours.windowBackground,
  },

  // ── Header ───────────────────────────────────────────────────────────────
  header: {
    backgroundColor: colours.headerBackground,
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: colours.separator,
  },
  headerTitle: {
    fontSize: 15,
    fontWeight: '600',
    color: colours.textTitle,
  },
  headerSub: {
    fontSize: 11,
    color: colours.textSubtle,
    marginTop: 2,
  },

  content: {
    padding: 14,
    paddingBottom: 32,
    gap: 12,
  },

  // ── Cards ────────────────────────────────────────────────────────────────
  card: {
    backgroundColor: colours.cardBackground,
    borderRadius: radii.large,
    padding: 16,
    gap: 10,
  },
  cardHead: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  cardTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: colours.textTitle,
    flexShrink: 1,
  },
  subcard: {
    backgroundColor: colours.subCardBackground,
    borderRadius: radii.medium,
    padding: 12,
    gap: 8,
  },

  // ── Texto ────────────────────────────────────────────────────────────────
  text: {
    fontSize: 13,
    color: colours.textNormal,
  },
  smallText: {
    fontSize: 11,
    color: colours.textSubtle,
  },
  label: {
    fontSize: 10,
    letterSpacing: 0.6,
    color: colours.textSubtle,
    textTransform: 'uppercase',
  },
  value: {
    fontSize: 14,
    fontWeight: '600',
    color: colours.textTitle,
  },
  mono: {
    fontFamily: 'monospace',
  },

  // ── LED e indicadores ────────────────────────────────────────────────────
  led: {
    width: 11,
    height: 11,
    borderRadius: 6,
  },
  indicator: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: 10,
    paddingVertical: 7,
  },
  indicatorName: {
    fontSize: 13,
    fontWeight: '600',
    color: colours.textTitle,
    width: 84,
  },

  // ── Buttons ──────────────────────────────────────────────────────────────
  button: {
    backgroundColor: colours.btnLight,
    borderRadius: radii.small,
    paddingVertical: 12,
    paddingHorizontal: 18,
    alignItems: 'center',
  },
  buttonPrimary: {
    backgroundColor: colours.btnDark,
  },
  buttonDisabled: {
    backgroundColor: colours.btnDisabled,
  },
  buttonText: {
    fontSize: 13,
    fontWeight: '600',
    color: colours.btnLightText,
  },
  buttonTextPrimary: {
    color: colours.btnDarkText,
  },
  buttonTextDisabled: {
    color: colours.btnDisabledText,
  },

  // ── Avisos ───────────────────────────────────────────────────────────────
  warning: {
    backgroundColor: colours.subCardBackground,
    borderLeftWidth: 3,
    borderLeftColor: colours.warning,
    borderRadius: radii.small,
    padding: 12,
  },
  noticeError: {
    borderLeftColor: colours.error,
  },

  // ── Fuses e dados ────────────────────────────────────────────────────────
  fuse: {
    backgroundColor: colours.inputBackground,
    borderRadius: radii.small,
    padding: 10,
    flexGrow: 1,
    flexBasis: '47%',
  },
  fuseValue: {
    fontSize: 15,
    fontWeight: '600',
    color: colours.textTitle,
    fontFamily: 'monospace',
  },
  grid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  property: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    gap: 10,
    paddingVertical: 6,
    borderBottomWidth: 1,
    borderBottomColor: colours.separator,
  },

  // ── Mapa da Flash ────────────────────────────────────────────────────────
  bar: {
    flexDirection: 'row',
    height: 22,
    borderRadius: radii.small,
    overflow: 'hidden',
    backgroundColor: colours.progressBackground,
  },
  progress: {
    height: 8,
    borderRadius: radii.pill,
    overflow: 'hidden',
    backgroundColor: colours.progressBackground,
  },
  progressBar: {
    height: '100%',
    backgroundColor: colours.progressBar,
  },

  // ── Pinos e tags ─────────────────────────────────────────────────────────
  pin: {
    backgroundColor: colours.pin,
    borderRadius: radii.small,
    paddingVertical: 5,
    paddingHorizontal: 7,
    minWidth: 36,
    alignItems: 'center',
  },
  pinText: {
    fontSize: 10,
    fontWeight: '600',
    color: colours.pinText,
    fontFamily: 'monospace',
  },
  tag: {
    backgroundColor: colours.tag,
    borderRadius: radii.small,
    paddingVertical: 3,
    paddingHorizontal: 8,
  },
  tagText: {
    fontSize: 10,
    fontWeight: '600',
    color: colours.tagText,
  },
  badge: {
    backgroundColor: colours.badge,
    borderRadius: radii.small,
    paddingVertical: 2,
    paddingHorizontal: 8,
  },
  badgeText: {
    fontSize: 10,
    fontWeight: '600',
    color: colours.badgeText,
  },

  // ── Lista de testes ──────────────────────────────────────────────────────
  testRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 10,
    paddingVertical: 8,
    borderBottomWidth: 1,
    borderBottomColor: colours.separator,
  },

  // ── Text field (bench address) ───────────────────────────────────────────
  field: {
    backgroundColor: colours.inputBackground,
    borderRadius: radii.small,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 15,
    color: colours.textTitle,
    fontFamily: 'monospace',
  },

  // ── Dialog ───────────────────────────────────────────────────────────────
  dialogBackdrop: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.35)',
    justifyContent: 'center',
    padding: 20,
  },
  dialog: {
    backgroundColor: colours.cardBackground,
    borderRadius: radii.huge,
    padding: 20,
    gap: 10,
    maxHeight: '85%',
  },
  dialogTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: colours.textTitle,
  },

  // ── Utilidades ───────────────────────────────────────────────────────────
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    flexWrap: 'wrap',
  },
  stack: {
    gap: 10,
  },
  output: {
    backgroundColor: colours.inputBackground,
    borderRadius: radii.small,
    padding: 10,
  },
  outputText: {
    fontSize: 10,
    fontFamily: 'monospace',
    color: colours.textNormal,
  },
});
