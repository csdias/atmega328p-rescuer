/**
 * Módulos que existem no menu mas ainda não fazem nada. O WPF tem o mesmo ecrã — dizer
 * "por implementar" é mais honesto do que esconder a entrada e deixar quem está à bancada
 * a perguntar onde ela foi.
 */
export function EcranEmConstrucao({ modulo }: { modulo: string }) {
  return (
    <section className="cartao" style={{ maxWidth: 560 }}>
      <div className="cartao__cabeca">
        <span aria-hidden="true" style={{ fontSize: 18 }}>
          🚧
        </span>
        <h2 className="cartao__titulo">{modulo}</h2>
      </div>
      <p className="cartao__descricao">Por implementar.</p>
    </section>
  );
}
