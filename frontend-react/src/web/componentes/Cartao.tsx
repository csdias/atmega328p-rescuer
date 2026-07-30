import type { ReactNode } from 'react';
import type { EstadoLed } from '../../partilhado/contratos';
import { Led } from './Led';

/**
 * Um passo da bancada. O LED e o estado no cabeçalho são o resumo denso — a única coisa
 * visível quando o resto está fechado.
 */
export function Cartao({
  titulo,
  estado,
  severidade,
  acoes,
  children,
}: {
  titulo: string;
  estado?: string | undefined;
  severidade?: EstadoLed | undefined;
  acoes?: ReactNode | undefined;
  children?: ReactNode | undefined;
}) {
  return (
    <section className="cartao">
      <div className="cartao__cabeca">
        {severidade && <Led estado={severidade} />}
        <h2 className="cartao__titulo">{titulo}</h2>
        <span style={{ flex: 1 }} />
        {acoes}
      </div>

      {estado && (
        <p className={`cartao__descricao estado estado--${severidade ?? 'inactivo'}`}>{estado}</p>
      )}

      {children && <div className="pilha" style={{ marginTop: 14 }}>{children}</div>}
    </section>
  );
}
