/**
 * Modules that exist in the menu but do not do anything yet. The WPF has the same screen —
 * saying "not implemented" is more honest than hiding the entry and leaving whoever is at
 * the bench wondering where it went.
 */
export function UnderConstructionScreen({ modulo }: { modulo: string }) {
  return (
    <section className="card" style={{ maxWidth: 560 }}>
      <div className="card__head">
        <span aria-hidden="true" style={{ fontSize: 18 }}>
          🚧
        </span>
        <h2 className="card__title">{modulo}</h2>
      </div>
      <p className="card__description">Por implementar.</p>
    </section>
  );
}
