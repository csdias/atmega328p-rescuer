import type { Catalog } from '@atmegapesta/shared';

/**
 * The ATmega328P pins and the buses the tests would touch.
 *
 * All the pins stay idle because nothing was measured — painting them green with no
 * measurement would be saying they are good. See the catalog's warning.
 */
export function PinMap({ catalog }: { catalog: Catalog }) {
  const tags = [...new Set(catalog.tests.map((t) => t.tag).filter((t): t is string => t !== null))];

  return (
    <div className="stack">
      <div>
        <div className="datum__label" style={{ marginBottom: 6 }}>
          Estado dos pins GPIO
        </div>
        <div className="pins">
          {catalog.pins.map((pin) => (
            <span key={pin} className="pin" title="Inactivo — nada foi medido">
              {pin}
            </span>
          ))}
        </div>
      </div>

      <div>
        <div className="datum__label" style={{ marginBottom: 6 }}>
          Barramentos cobertos
        </div>
        <div className="tags">
          {tags.map((tag) => (
            <span key={tag} className="tag">
              {tag}
            </span>
          ))}
          <span className="tag">ISP</span>
        </div>
      </div>

      <p className="subtle" style={{ margin: 0 }}>
        Todos os pins aparecem inactivos: nada foi medido.
      </p>
    </div>
  );
}
