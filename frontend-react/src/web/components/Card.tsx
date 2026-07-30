import type { ReactNode } from 'react';
import type { LedState } from '@atmegapesta/shared';
import { Led } from './Led';

/**
 * One bench step. The LED and the state in the header are the dense summary — the only
 * thing visible when the rest is closed.
 */
export function Card({
  title,
  state,
  severity,
  actions,
  children,
}: {
  title: string;
  state?: string | undefined;
  severity?: LedState | undefined;
  actions?: ReactNode | undefined;
  children?: ReactNode | undefined;
}) {
  return (
    <section className="card">
      <div className="card__head">
        {severity && <Led state={severity} />}
        <h2 className="card__title">{title}</h2>
        <span style={{ flex: 1 }} />
        {actions}
      </div>

      {state && (
        <p className={`card__description status estado--${severity ??'idle'}`}>{state}</p>
      )}

      {children && <div className="stack" style={{ marginTop: 14 }}>{children}</div>}
    </section>
  );
}
