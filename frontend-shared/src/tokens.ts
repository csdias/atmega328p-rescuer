/**
 * A paleta e as medidas da bancada, em valores. São os mesmos de
 * ATMegaPestaV1/Themes/LightTheme.xaml — o front end React não inventa um aspecto novo,
 * usa o do WPF para quem passa de um para o outro reconhecer a bancada.
 *
 * Ficam em valores, e não em CSS, porque o React Native não tem folhas de estilo: o web
 * converte-os em custom properties, o nativo passa-os a StyleSheet.
 */
export const cores = {
  fundoJanela: '#F5F5F5',
  fundoHeader: '#E0E0E0',
  fundoMenu: '#E8E8E8',
  fundoCard: '#FFFFFF',
  fundoSubCard: '#F0F0F0',
  fundoInput: '#EBEBEB',
  separador: '#CCCCCC',

  textoTitulo: '#1A1A1A',
  textoNormal: '#444444',
  textoSubtil: '#888888',
  textoDesactivado: '#BBBBBB',

  menuAccento: '#555555',

  btnEscuro: '#1A1A1A',
  btnEscuroHover: '#333333',
  btnEscuroTexto: '#FFFFFF',

  btnClaro: '#DDDDDD',
  btnClaroHover: '#CCCCCC',
  btnClaroTexto: '#1A1A1A',

  btnDesactivado: '#CCCCCC',
  btnDesactivadoTexto: '#999999',

  pino: '#DDDDDD',
  pinoTexto: '#1A1A1A',

  tag: '#E2E2E2',
  tagTexto: '#333333',

  badge: '#6B7280',
  badgeTexto: '#FFFFFF',

  progressoFundo: '#DDDDDD',
  progressoBarra: '#1A1A1A',

  erro: '#D9222B',
  sucesso: '#2D7A4F',
  aviso: '#D97706',
  inactivo: '#CCCCCC',
} as const;

/** A cor de cada estado de indicador — o que os LEDs do WPF pintavam. */
export const coresLed = {
  inactivo: cores.inactivo,
  ok: cores.sucesso,
  aviso: cores.aviso,
  erro: cores.erro,
} as const;

export const raios = {
  tenue: 2,
  pequeno: 4,
  medio: 6,
  grande: 8,
  enorme: 12,
  pilula: 11,
} as const;

export const espacos = {
  xs: 4,
  sm: 8,
  md: 12,
  lg: 16,
  xl: 24,
  xxl: 32,
} as const;

export const tipografia = {
  familia: '"Segoe UI", system-ui, -apple-system, sans-serif',
  // Para os fuses e a assinatura: hexadecimal alinha-se melhor a largura fixa.
  familiaMono: '"Cascadia Mono", "Consolas", ui-monospace, monospace',
} as const;
