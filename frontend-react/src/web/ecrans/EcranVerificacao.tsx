import type { Bancada } from '../../partilhado/usarBancada';
import { LinhaIndicador } from '../componentes/Led';

/**
 * O arranque: até a bancada estar completa e o equipamento identificado não se avança.
 * É o ecrã que o WPF mostra antes do painel principal.
 */
export function EcranVerificacao({ bancada }: { bancada: Bancada }) {
  const { deteccao, ocupacao, progresso, erro, config } = bancada;
  const aVerificar = ocupacao === 'detectar';
  const esgotado = deteccao?.esgotado === true;

  const restantes = deteccao ? deteccao.maxTentativas - deteccao.tentativa : null;

  return (
    <section className="cartao" style={{ maxWidth: 720 }}>
      <div className="cartao__cabeca">
        <h2 className="cartao__titulo">Verificação de dispositivos</h2>
      </div>
      <p className="cartao__descricao">
        A bancada precisa do conversor CH340 e do programador USBAsp ligados ao computador.
      </p>

      <div className="subcartao" style={{ marginTop: 16 }}>
        <LinhaIndicador
          nome="CH340"
          indicador={deteccao?.ch340 ?? { estado: 'inactivo', detalhe: 'Ainda não verificado' }}
        />
        <LinhaIndicador
          nome="USBAsp"
          indicador={deteccao?.usbAsp ?? { estado: 'inactivo', detalhe: 'Ainda não verificado' }}
        />
        <LinhaIndicador
          nome="Assinatura"
          indicador={
            deteccao?.assinatura ?? {
              estado: 'inactivo',
              detalhe:
                config?.verificarAssinatura === false
                  ? 'Verificação de assinatura desligada na configuração'
                  : 'A aguardar detecção do CH340...',
            }
          }
        />
      </div>

      <div className="pilha" style={{ marginTop: 16 }}>
        {aVerificar && (
          <p className="estado estado--inactivo" style={{ margin: 0 }} role="status">
            {progresso?.texto ?? 'A verificar dispositivos...'}
          </p>
        )}

        {!aVerificar && deteccao && (
          <p
            className={`estado estado--${deteccao.severidade}`}
            style={{ margin: 0, whiteSpace: 'pre-line' }}
            role="status"
          >
            {deteccao.mensagem}
          </p>
        )}

        {erro && (
          <p className="aviso aviso--erro" style={{ margin: 0 }}>
            {erro}
          </p>
        )}

        <div className="linha">
          {esgotado ? (
            <>
              <button
                type="button"
                className="botao botao--principal"
                onClick={() => void bancada.reiniciarCiclo()}
              >
                Recomeçar a verificação
              </button>
              <span className="subtil">
                Recomeçar devolve as tentativas. O WPF encerrava aqui; no browser não há
                aplicação para fechar.
              </span>
            </>
          ) : (
            <>
              <button
                type="button"
                className="botao botao--principal"
                onClick={() => void bancada.detectar()}
                disabled={aVerificar}
              >
                {aVerificar
                  ? 'A verificar...'
                  : deteccao
                    ? `Tentar novamente${restantes !== null ? ` (${restantes} restante${restantes !== 1 ? 's' : ''})` : ''}`
                    : 'Verificar dispositivos'}
              </button>

              {deteccao?.podeAvancar && (
                <button type="button" className="botao" onClick={bancada.irParaInserirChip}>
                  Continuar
                </button>
              )}
            </>
          )}
        </div>

        {deteccao && !esgotado && !deteccao.podeAvancar && (
          <p className="subtil" style={{ margin: 0 }}>
            Tentativa {deteccao.tentativa} de {deteccao.maxTentativas}
          </p>
        )}
      </div>
    </section>
  );
}
