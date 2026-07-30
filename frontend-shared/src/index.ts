/**
 * The bench logic, shared by the web front end and by React Native.
 *
 * Nothing here touches the DOM or any browser API: only `fetch`, WebSocket (through
 * SignalR) and React hooks — which exist on both sides. That is what allows there to be a
 * single copy of this logic instead of two drifting apart.
 */
export * from './contracts';
export * from './apiClient';
export * from './tokens';
export * from './useBench';
