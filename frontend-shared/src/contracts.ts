/**
 * The API contracts, in TypeScript. They mirror ATMegaPestaV1.Api/Bench/Contracts.cs —
 * if one of these files changes, the other changes with it.
 *
 * Nothing here touches the DOM: this is the material React Native reuses exactly as it is.
 */

/** State of an indicator, in the vocabulary the WPF LEDs already used. */
export type LedState = 'idle' | 'ok' | 'warning' | 'error';

/** An indicator: the colour and the line of text that goes with it. */
export interface Indicator {
  state: LedState;
  detail: string;
}

export interface Config {
  maxAttempts: number;
  verifySignature: boolean;
  maxReadAttempts: number;
  backupFolder: string;
}

export interface Detection {
  ch340: Indicator;
  usbAsp: Indicator;
  signature: Indicator;
  comPort: string | null;
  /** Rig complete and equipment identified. */
  canProceed: boolean;
  attempt: number;
  maxAttempts: number;
  /** Attempts spent: the shutdown is shown, as in the WPF. */
  exhausted: boolean;
  message: string;
  severity: LedState;
}

/** The four bytes, as avrdude returned them. */
export interface Fuses {
  low: string | null;
  high: string | null;
  extended: string | null;
  lock: string | null;
}

/** The same bytes in human language. */
export interface DecodedFuses {
  clock: string;
  ckdiv8Enabled: boolean;
  brownOut: string;
  spiEnabled: boolean;
  resetEnabled: boolean;
  eepromPreserved: boolean;
  bootRstEnabled: boolean;
  lockLevel: string;
  readEnabled: boolean;
  lowDescription: string;
  highDescription: string;
  extendedDescription: string;
  lockDescription: string;
}

export interface Mcu {
  name: string;
  flashBytes: number;
  eepromBytes: number;
  sramBytes: number;
  flash: string;
  eeprom: string;
  sram: string;
}

/**
 * The Flash map. The application's slice is what is left after the bootloader is reserved —
 * capacity, not occupancy: the Flash contents are not read.
 */
export interface FlashMap {
  totalBytes: number;
  bootloaderBytes: number;
  applicationBytes: number;
  bootloader: string;
  application: string;
  bootloaderReserved: boolean;
}

export interface Read {
  /** The chip answered over ISP. */
  identified: boolean;
  /**
   * Chip identified *and* fuses decoded. It is this — and not `identified` — that unlocks
   * the integrity check.
   */
  settingsRead: boolean;
  mcu: Mcu | null;
  signature: string | null;
  fuses: Fuses | null;
  decoded: DecodedFuses | null;
  flashMap: FlashMap | null;
  state: string;
  severity: LedState;
  instruction: string | null;
  attempts: string | null;
  attempt: number;
  maxAttempts: number;
  /** ISP attempts spent: high-voltage programming is proposed. */
  exhausted: boolean;
  /** The bus went back to Hi-Z. False is a warning to show, not an internal detail. */
  busIsolated: boolean;
  avrdudeOutput: string | null;
}

export interface BackupFile {
  name: string;
  url: string;
  bytes: number;
}

export interface Backup {
  success: boolean;
  timestamp: string;
  folder: string;
  files: BackupFile[];
  fuses: Fuses | null;
  message: string;
  severity: LedState;
  avrdudeOutput: string | null;
  busIsolated: boolean;
}

/** What was decided about the backup before letting the check go ahead. */
export type TransferChoice = 'proceedWithBackup' | 'proceedWithoutBackup';

export type TestState = 'pending' | 'passed' | 'failed';

export interface TestResult {
  name: string;
  state: TestState;
  time: string;
}

export interface Integrity {
  busSwitched: boolean;
  busIsolated: boolean;
  backup: Backup | null;
  results: TestResult[];
  progressPct: number;
  message: string;
  severity: LedState;
}

export interface CatalogTest {
  name: string;
  /** Pins the test touches — what lights up on the GPIO map. */
  pins: string[];
  /** Bus it belongs to (UART, SPI, I2C, PWM), or null. */
  tag: string | null;
}

export interface Catalog {
  pins: string[];
  tests: CatalogTest[];
  /** Why the tests are not run. */
  warning: string;
}

/** A progress line coming from the hub while an operation runs. */
export interface Progress {
  text: string;
  severity: LedState;
}
