import type { MapaFlash as Mapa } from '@atmegapesta/partilhado';

/**
 * A barra da Flash: quanto sobra para a aplicação depois de reservado o bootloader.
 *
 * É capacidade, não ocupação — o conteúdo da Flash não é lido, e a legenda diz isso para
 * ninguém ler a fatia verde como "espaço usado".
 */
export function MapaFlash({ mapa }: { mapa: Mapa }) {
  const pctApp = Math.round((mapa.aplicacaoBytes / mapa.totalBytes) * 100);

  return (
    <div>
      <div className="linha" style={{ justifyContent: 'space-between', marginBottom: 6 }}>
        <span className="dado__rotulo">Mapa da Flash</span>
        <span className="subtil">
          {mapa.bootloaderReservado
            ? `bootloader ${mapa.bootloader} reservado`
            : 'sem bootloader reservado'}
        </span>
      </div>

      <div
        className="mapa-flash__barra"
        role="img"
        aria-label={
          mapa.bootloaderReservado
            ? `Flash de ${mapa.totalBytes} bytes: ${mapa.aplicacao} disponíveis para a aplicação, ${mapa.bootloader} reservados ao bootloader.`
            : `Flash de ${mapa.totalBytes} bytes, ${mapa.aplicacao} disponíveis para a aplicação.`
        }
      >
        <div className="mapa-flash__fatia mapa-flash__fatia--aplicacao" style={{ width: `${pctApp}%` }} />
        {mapa.bootloaderReservado && (
          <div
            className="mapa-flash__fatia mapa-flash__fatia--bootloader"
            style={{ width: `${100 - pctApp}%` }}
          />
        )}
      </div>

      <div className="legenda">
        <span className="legenda__item">
          <span className="legenda__amostra" style={{ background: 'var(--sucesso)' }} />
          Disponível para a aplicação — {mapa.aplicacao}
        </span>
        {mapa.bootloaderReservado && (
          <span className="legenda__item">
            <span className="legenda__amostra" style={{ background: 'var(--badge)' }} />
            Bootloader — {mapa.bootloader}
          </span>
        )}
        <span className="legenda__item subtil">Capacidade, não ocupação: a Flash não é lida.</span>
      </div>
    </div>
  );
}
