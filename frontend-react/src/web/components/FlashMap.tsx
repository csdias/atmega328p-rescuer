import type { FlashMap as Mapa } from '@atmegapesta/shared';

/**
 * The Flash bar: how much is left for the application after the bootloader is reserved.
 *
 * It is capacity, not occupancy — the Flash contents are not read, and the legend says so
 * to stop anyone reading the green slice as "space used".
 */
export function FlashMap({ mapa }: { mapa: Mapa }) {
  const pctApp = Math.round((mapa.applicationBytes / mapa.totalBytes) * 100);

  return (
    <div>
      <div className="row" style={{ justifyContent: 'space-between', marginBottom: 6 }}>
        <span className="datum__label">Mapa da Flash</span>
        <span className="subtle">
          {mapa.bootloaderReserved
            ? `bootloader ${mapa.bootloader} reservado`
            : 'sem bootloader reservado'}
        </span>
      </div>

      <div
        className="flash-map__bar"
        role="img"
        aria-label={
          mapa.bootloaderReserved
            ? `Flash de ${mapa.totalBytes} bytes: ${mapa.application} disponíveis para a aplicação, ${mapa.bootloader} reservados ao bootloader.`
            : `Flash de ${mapa.totalBytes} bytes, ${mapa.application} disponíveis para a aplicação.`
        }
      >
        <div className="flash-map__slice flash-map__slice--application" style={{ width: `${pctApp}%` }} />
        {mapa.bootloaderReserved && (
          <div
            className="flash-map__slice flash-map__slice--bootloader"
            style={{ width: `${100 - pctApp}%` }}
          />
        )}
      </div>

      <div className="legend">
        <span className="legend__item">
          <span className="legend__swatch" style={{ background: 'var(--success)' }} />
          Disponível para a aplicação — {mapa.application}
        </span>
        {mapa.bootloaderReserved && (
          <span className="legend__item">
            <span className="legend__swatch" style={{ background: 'var(--badge)' }} />
            Bootloader — {mapa.bootloader}
          </span>
        )}
        <span className="legend__item subtle">Capacidade, não ocupação: a Flash não é lida.</span>
      </div>
    </div>
  );
}
