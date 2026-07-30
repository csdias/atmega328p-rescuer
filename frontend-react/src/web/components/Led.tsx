import type { LedState, Indicator } from '@atmegapesta/shared';

/** O ponto colorido dos LEDs do WPF. */
export function Led({ state }: { state: LedState }) {
  return <span className={`led led--${state}`} role="presentation" />;
}

/**
 * One indicator row: LED, name and detail.
 *
 * The state also travels as text in the accessible label — colour cannot be the only way
 * to know whether the CH340 showed up.
 */
export function IndicatorRow({ name, indicator }: { name: string; indicator: Indicator }) {
  return (
    <div className="indicator">
      <Led state={indicator.state} />
      <span className="indicator__name">{name}</span>
      <span className="indicator__detail">
        <span className="subtle">{palavraEstado(indicator.state)} · </span>
        {indicator.detail}
      </span>
    </div>
  );
}

function palavraEstado(state: LedState): string {
  switch (state) {
    case 'ok':
      return 'OK';
    case 'warning':
      return 'Aviso';
    case 'error':
      return 'Falha';
    case 'idle':
      return 'Inactivo';
  }
}
