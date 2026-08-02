import {
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType,
  type HubConnection,
} from '@microsoft/signalr';
import type {
  Backup,
  Catalog,
  Config,
  Detection,
  Integrity,
  Progress,
  Read,
  TransferChoice,
} from './contracts';

/**
 * The bench API client. It does not touch the DOM — React Native uses it exactly as it is,
 * passing the bench PC's `baseUrl` instead of leaving it empty.
 *
 * An empty `baseUrl` is the browser's case: in development Vite forwards /api to Kestrel,
 * in production the API serves the React build itself. In both the origin is the same and
 * there is no address to configure.
 */
export class BenchClient {
  private readonly baseUrl: string;
  private hub: HubConnection | null = null;

  constructor(baseUrl = '') {
    // No trailing slash: the paths below already start with one.
    this.baseUrl = baseUrl.replace(/\/$/, '');
  }

  config = () => this.request<Config>('GET', '/api/bench/config');

  /**
   * Scans the USB and asks the bench for its signature. Each call is one attempt, while the
   * bench is not yet complete.
   */
  detect = () => this.request<Detection>('POST', '/api/bench/detect');

  /** Puts the cycle back to the start. It is what you do when swapping the part in the ZIF. */
  reset = () => this.request<void>('POST', '/api/bench/reset');

  /** Reads the target chip's identification and fuses. Strictly a read. */
  readSettings = () => this.request<Read>('POST', '/api/target/read-settings');

  saveBackup = () => this.request<Backup>('POST', '/api/target/backup');

  catalog = () => this.request<Catalog>('GET', '/api/integrity/catalog');

  runIntegrity = (choice: TransferChoice) =>
    this.request<Integrity>('POST', '/api/integrity/run', { choice });

  /** Download URL for a backup file, absolute when there is a `baseUrl`. */
  fileUrl = (url: string) => `${this.baseUrl}${url}`;

  /**
   * Connects to the progress channel and calls `onProgress` on every line. Returns the
   * function that disconnects.
   *
   * An ISP access takes seconds; without this the screen sits frozen waiting for the HTTP
   * response. If the connection does not come up, the front end carries on working — what
   * is lost is the live progress, not the operation.
   */
  async connectProgress(onProgress: (p: Progress) => void): Promise<() => void> {
    const hub = new HubConnectionBuilder()
      // WebSockets explicitly: it is the only transport that behaves the same in the
      // browser and in React Native. Left to SignalR's judgement, it can fall back to
      // Server-Sent Events or long polling, which on native depend on browser APIs that
      // do not exist.
      .withUrl(`${this.baseUrl}/hub/bench`, { transport: HttpTransportType.WebSockets })
      .withAutomaticReconnect()
      .build();

    hub.on('progress', onProgress);
    this.hub = hub;

    try {
      await hub.start();
    } catch {
      // With no progress channel the bench still answers over HTTP.
    }

    return () => {
      hub.off('progress', onProgress);
      if (hub.state !== HubConnectionState.Disconnected) void hub.stop();
      if (this.hub === hub) this.hub = null;
    };
  }

  private async request<T>(method: 'GET' | 'POST', path: string, body?: unknown): Promise<T> {
    const response = await fetch(`${this.baseUrl}${path}`, {
      method,
      headers: body === undefined ? {} : { 'Content-Type': 'application/json' },
      ...(body === undefined ? {} : { body: JSON.stringify(body) }),
    });

    if (!response.ok)
      throw new ApiError(
        `A bancada respondeu ${response.status} a ${method} ${path}.`,
        response.status,
      );

    // 204 from the operations that return nothing (reset).
    if (response.status === 204) return undefined as T;

    return (await response.json()) as T;
  }
}

/** An API failure, with the code for anyone wanting to tell "did not answer" from "refused". */
export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}
