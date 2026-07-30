/**
 * A lógica da bancada, partilhada pelo front end web e pelo React Native.
 *
 * Nada aqui toca no DOM nem em APIs de browser: só `fetch`, WebSocket (através do
 * SignalR) e hooks do React — que existem nos dois lados. É o que permite haver uma
 * cópia só desta lógica em vez de duas a divergir.
 */
export * from './contratos';
export * from './clienteApi';
export * from './tokens';
export * from './usarBancada';
