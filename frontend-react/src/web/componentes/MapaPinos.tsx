import type { Catalogo } from '../../partilhado/contratos';

/**
 * Os pinos do ATmega328P e os barramentos que os testes tocariam.
 *
 * Todos os pinos ficam inactivos porque nada foi medido — pintá-los de verde sem medição
 * seria dizer que estão bons. Ver o aviso do catálogo.
 */
export function MapaPinos({ catalogo }: { catalogo: Catalogo }) {
  const tags = [...new Set(catalogo.testes.map((t) => t.tag).filter((t): t is string => t !== null))];

  return (
    <div className="pilha">
      <div>
        <div className="dado__rotulo" style={{ marginBottom: 6 }}>
          Estado dos pinos GPIO
        </div>
        <div className="pinos">
          {catalogo.pinos.map((pino) => (
            <span key={pino} className="pino" title="Inactivo — nada foi medido">
              {pino}
            </span>
          ))}
        </div>
      </div>

      <div>
        <div className="dado__rotulo" style={{ marginBottom: 6 }}>
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

      <p className="subtil" style={{ margin: 0 }}>
        Todos os pinos aparecem inactivos: nada foi medido.
      </p>
    </div>
  );
}
