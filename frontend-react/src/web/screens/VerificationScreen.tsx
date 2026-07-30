import type { Bench } from '@atmegapesta/shared';
import { IndicatorRow } from '../components/Led';

/**
 * The startup: until the bench is complete and the rig identified, nothing moves on.
 * It is the screen the WPF shows before the main panel.
 */
export function VerificationScreen({ bench }: { bench: Bench }) {
  const { detection, busy, progress, error, config } = bench;
  const aVerificar = busy === 'detect';
  const exhausted = detection?.exhausted === true;

  const restantes = detection ? detection.maxAttempts - detection.attempt : null;

  return (
    <section className="card" style={{ maxWidth: 720 }}>
      <div className="card__head">
        <h2 className="card__title">Verificação de dispositivos</h2>
      </div>
      <p className="card__description">
        A bench precisa do conversor CH340 e do programador USBAsp ligados ao computador.
      </p>

      <div className="subcard" style={{ marginTop: 16 }}>
        <IndicatorRow
          name="CH340"
          indicator={detection?.ch340 ?? { state: 'idle', detail: 'Ainda não verificado' }}
        />
        <IndicatorRow
          name="USBAsp"
          indicator={detection?.usbAsp ?? { state: 'idle', detail: 'Ainda não verificado' }}
        />
        <IndicatorRow
          name="Assinatura"
          indicator={
            detection?.signature ?? {
              state: 'idle',
              detail:
                config?.verifySignature === false
                  ? 'Verificação de assinatura desligada na configuração'
                  : 'A aguardar detecção do CH340...',
            }
          }
        />
      </div>

      <div className="stack" style={{ marginTop: 16 }}>
        {aVerificar && (
          <p className="status status--idle" style={{ margin: 0 }} role="status">
            {progress?.text ?? 'A verificar dispositivos...'}
          </p>
        )}

        {!aVerificar && detection && (
          <p
            className={`status estado--${detection.severity}`}
            style={{ margin: 0, whiteSpace: 'pre-line' }}
            role="status"
          >
            {detection.message}
          </p>
        )}

        {error && (
          <p className="notice notice--error" style={{ margin: 0 }}>
            {error}
          </p>
        )}

        <div className="row">
          {exhausted ? (
            <>
              <button
                type="button"
                className="button button--primary"
                onClick={() => void bench.resetCycle()}
              >
                Recomeçar a verificação
              </button>
              <span className="subtle">
                Recomeçar devolve as attempts. O WPF encerrava aqui; no browser não há
                aplicação para fechar.
              </span>
            </>
          ) : (
            <>
              <button
                type="button"
                className="button button--primary"
                onClick={() => void bench.detect()}
                disabled={aVerificar}
              >
                {aVerificar
                  ? 'A verificar...'
                  : detection
                    ? `Tentar novamente${restantes !== null ? ` (${restantes} restante${restantes !== 1 ? 's' : ''})` : ''}`
                    : 'Verificar dispositivos'}
              </button>

              {detection?.canProceed && (
                <button type="button" className="button" onClick={bench.goToInsertChip}>
                  Continuar
                </button>
              )}
            </>
          )}
        </div>

        {detection && !exhausted && !detection.canProceed && (
          <p className="subtle" style={{ margin: 0 }}>
            Tentativa {detection.attempt} de {detection.maxAttempts}
          </p>
        )}
      </div>
    </section>
  );
}
