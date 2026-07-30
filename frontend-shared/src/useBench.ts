import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { BenchClient } from './apiClient';
import type {
  Catalog,
  Config,
  Detection,
  Integrity,
  Progress,
  Read,
  TransferChoice,
} from './contracts';

/**
 * The phases of the flow, in the order they happen. They are the same as the WPF's: until
 * the bench is verified you are not asked to insert the chip, and until the chip is in the
 * ZIF nothing is read.
 */
export type Phase = 'verification' | 'insertChip' | 'work';

/** Which operation is running. There is only one bench — there are never two at once. */
export type Busy = 'none' | 'detect' | 'read' | 'integrity';

export interface Bench {
  config: Config | null;
  catalog: Catalog | null;
  phase: Phase;
  detection: Detection | null;
  read: Read | null;
  integrity: Integrity | null;
  progress: Progress | null;
  busy: Busy;
  error: string | null;

  /** A complete read: it is what unlocks the integrity check. */
  canCheckIntegrity: boolean;
  /** ISP attempts spent — high-voltage programming has to be proposed. */
  escalateHighVoltage: boolean;

  detect: () => Promise<void>;
  goToInsertChip: () => void;
  backToVerification: () => void;
  confirmChipInserted: () => Promise<void>;
  readSettings: () => Promise<void>;
  runIntegrity: (choice: TransferChoice) => Promise<void>;
  resetCycle: () => Promise<void>;
  fileUrl: (url: string) => string;
}

/**
 * The whole bench flow, without a line of DOM: state, transitions and API calls. React
 * Native builds its own screens on top of this hook, passing the bench PC's `baseUrl`.
 */
export function useBench(baseUrl = ''): Bench {
  const client = useMemo(() => new BenchClient(baseUrl), [baseUrl]);

  const [config, setConfig] = useState<Config | null>(null);
  const [catalog, setCatalog] = useState<Catalog | null>(null);
  const [phase, setPhase] = useState<Phase>('verification');
  const [detection, setDetection] = useState<Detection | null>(null);
  const [read, setRead] = useState<Read | null>(null);
  const [integrity, setIntegrity] = useState<Integrity | null>(null);
  const [progress, setProgress] = useState<Progress | null>(null);
  const [busy, setBusy] = useState<Busy>('none');
  const [error, setError] = useState<string | null>(null);

  // The bench is a single resource and the API serialises access to it. Refusing the
  // second request here avoids leaving whoever pressed twice waiting in an invisible queue.
  const running = useRef(false);

  useEffect(() => {
    let alive = true;

    void (async () => {
      try {
        const [c, cat] = await Promise.all([client.config(), client.catalog()]);
        if (!alive) return;
        setConfig(c);
        setCatalog(cat);
      } catch (e) {
        if (alive) setError(messageOf(e));
      }
    })();

    return () => {
      alive = false;
    };
  }, [client]);

  useEffect(() => {
    let disconnect: (() => void) | null = null;
    let alive = true;

    void client.connectProgress(setProgress).then((f) => {
      if (alive) disconnect = f;
      else f();
    });

    return () => {
      alive = false;
      disconnect?.();
    };
  }, [client]);

  /**
   * Runs a bench operation, with the busy state and the errors handled in one place.
   * The progress line is cleared at the end: what stays is the result, not the last step.
   */
  const operate = useCallback(
    async (which: Exclude<Busy, 'none'>, action: () => Promise<void>) => {
      if (running.current) return;

      running.current = true;
      setBusy(which);
      setError(null);

      try {
        await action();
      } catch (e) {
        setError(messageOf(e));
      } finally {
        running.current = false;
        setBusy('none');
        setProgress(null);
      }
    },
    [],
  );

  const detect = useCallback(
    () => operate('detect', async () => setDetection(await client.detect())),
    [client, operate],
  );

  const readSettings = useCallback(
    () =>
      operate('read', async () => {
        // A new read invalidates what the previous one unlocked: whoever swapped the part
        // in the ZIF should not carry on seeing another chip's results.
        setIntegrity(null);
        setRead(await client.readSettings());
      }),
    [client, operate],
  );

  const runIntegrity = useCallback(
    (choice: TransferChoice) =>
      operate('integrity', async () => setIntegrity(await client.runIntegrity(choice))),
    [client, operate],
  );

  const goToInsertChip = useCallback(() => setPhase('insertChip'), []);
  const backToVerification = useCallback(() => setPhase('verification'), []);

  const confirmChipInserted = useCallback(async () => {
    setPhase('work');
    await readSettings();
  }, [readSettings]);

  const resetCycle = useCallback(async () => {
    await client.reset();
    setDetection(null);
    setRead(null);
    setIntegrity(null);
    setError(null);
    setPhase('verification');
  }, [client]);

  return {
    config,
    catalog,
    phase,
    detection,
    read,
    integrity,
    progress,
    busy,
    error,
    canCheckIntegrity: read?.settingsRead === true,
    escalateHighVoltage: read?.exhausted === true,
    detect,
    goToInsertChip,
    backToVerification,
    confirmChipInserted,
    readSettings,
    runIntegrity,
    resetCycle,
    fileUrl: client.fileUrl,
  };
}

function messageOf(e: unknown): string {
  if (e instanceof Error) return e.message;
  return 'A bancada não respondeu.';
}
