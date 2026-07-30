/**
 * Os contratos da API, em TypeScript. Espelham ATMegaPestaV1.Api/Bancada/Contratos.cs —
 * se um destes ficheiros mudar, o outro muda com ele.
 *
 * Nada aqui toca no DOM: este é o material que o React Native vai reutilizar tal como está.
 */

/** Estado de um indicador, no vocabulário que os LEDs do WPF já usavam. */
export type EstadoLed = 'inactivo' | 'ok' | 'aviso' | 'erro';

/** Um indicador: a cor e a linha de texto que a acompanha. */
export interface Indicador {
  estado: EstadoLed;
  detalhe: string;
}

export interface Config {
  maxTentativas: number;
  verificarAssinatura: boolean;
  maxTentativasLeitura: number;
  pastaCopias: string;
}

export interface Deteccao {
  ch340: Indicador;
  usbAsp: Indicador;
  assinatura: Indicador;
  portaCom: string | null;
  /** Bancada completa e equipamento identificado. */
  podeAvancar: boolean;
  tentativa: number;
  maxTentativas: number;
  /** Tentativas gastas: mostra-se o encerramento, como no WPF. */
  esgotado: boolean;
  mensagem: string;
  severidade: EstadoLed;
}

/** Os quatro bytes, como o avrdude os devolveu. */
export interface Fuses {
  low: string | null;
  high: string | null;
  extended: string | null;
  lock: string | null;
}

/** Os mesmos bytes em linguagem humana. */
export interface FusesDecodificados {
  relogio: string;
  ckdiv8Activo: boolean;
  brownOut: string;
  spiActivo: boolean;
  resetActivo: boolean;
  eepromPreservada: boolean;
  bootRstActivo: boolean;
  bloqueio: string;
  leituraLivre: boolean;
  descricaoLow: string;
  descricaoHigh: string;
  descricaoExtended: string;
  descricaoLock: string;
}

export interface Mcu {
  nome: string;
  flashBytes: number;
  eepromBytes: number;
  sramBytes: number;
  flash: string;
  eeprom: string;
  sram: string;
}

/**
 * O mapa da Flash. A fatia da aplicação é o que sobra depois de reservado o bootloader —
 * capacidade, não ocupação: o conteúdo da Flash não é lido.
 */
export interface MapaFlash {
  totalBytes: number;
  bootloaderBytes: number;
  aplicacaoBytes: number;
  bootloader: string;
  aplicacao: string;
  bootloaderReservado: boolean;
}

export interface Leitura {
  /** O chip respondeu ao ISP. */
  identificado: boolean;
  /**
   * Chip identificado *e* fuses descodificados. É isto — e não `identificado` — que
   * destranca a verificação de integridade.
   */
  configuracoesLidas: boolean;
  mcu: Mcu | null;
  assinatura: string | null;
  fuses: Fuses | null;
  descodificado: FusesDecodificados | null;
  mapaFlash: MapaFlash | null;
  estado: string;
  severidade: EstadoLed;
  instrucao: string | null;
  tentativas: string | null;
  tentativa: number;
  maxTentativas: number;
  /** Tentativas de ISP gastas: propõe-se a programação de alta tensão. */
  esgotado: boolean;
  /** O barramento voltou a Hi-Z. Falso é um aviso a mostrar, não um detalhe interno. */
  barramentoIsolado: boolean;
  saidaAvrdude: string | null;
}

export interface FicheiroCopia {
  nome: string;
  url: string;
  bytes: number;
}

export interface Copia {
  sucesso: boolean;
  carimbo: string;
  pasta: string;
  ficheiros: FicheiroCopia[];
  fuses: Fuses | null;
  mensagem: string;
  severidade: EstadoLed;
  saidaAvrdude: string | null;
  barramentoIsolado: boolean;
}

/** O que se decidiu sobre a cópia antes de deixar avançar a verificação. */
export type EscolhaTransferencia = 'prosseguirComCopia' | 'prosseguirSemCopia';

export type EstadoTeste = 'pendente' | 'passou' | 'falhou';

export interface ResultadoTeste {
  nome: string;
  estado: EstadoTeste;
  tempo: string;
}

export interface Integridade {
  barramentoComutado: boolean;
  barramentoIsolado: boolean;
  copia: Copia | null;
  resultados: ResultadoTeste[];
  progressoPct: number;
  mensagem: string;
  severidade: EstadoLed;
}

export interface TesteCatalogo {
  nome: string;
  /** Pinos que o teste toca — o que se ilumina no mapa GPIO. */
  pinos: string[];
  /** Barramento a que pertence (UART, SPI, I2C, PWM), ou null. */
  tag: string | null;
}

export interface Catalogo {
  pinos: string[];
  testes: TesteCatalogo[];
  /** Porque é que os testes não são executados. */
  aviso: string;
}

/** Uma linha de progresso vinda do hub, enquanto uma operação corre. */
export interface Progresso {
  texto: string;
  severidade: EstadoLed;
}
