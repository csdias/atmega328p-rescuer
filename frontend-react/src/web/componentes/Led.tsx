import type { EstadoLed, Indicador } from '@atmegapesta/partilhado';

/** O ponto colorido dos LEDs do WPF. */
export function Led({ estado }: { estado: EstadoLed }) {
  return <span className={`led led--${estado}`} role="presentation" />;
}

/**
 * Uma linha de indicador: LED, nome e detalhe.
 *
 * O estado vai também em texto na etiqueta acessível — a cor não pode ser a única
 * forma de saber se o CH340 apareceu.
 */
export function LinhaIndicador({ nome, indicador }: { nome: string; indicador: Indicador }) {
  return (
    <div className="indicador">
      <Led estado={indicador.estado} />
      <span className="indicador__nome">{nome}</span>
      <span className="indicador__detalhe">
        <span className="subtil">{palavraEstado(indicador.estado)} · </span>
        {indicador.detalhe}
      </span>
    </div>
  );
}

function palavraEstado(estado: EstadoLed): string {
  switch (estado) {
    case 'ok':
      return 'OK';
    case 'aviso':
      return 'Aviso';
    case 'erro':
      return 'Falha';
    case 'inactivo':
      return 'Inactivo';
  }
}
