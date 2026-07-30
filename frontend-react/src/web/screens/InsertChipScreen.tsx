import type { Bench } from '@atmegapesta/shared';

/**
 * The step between the verified bench and the read: the chip has to be in the ZIF before
 * ISP goes looking for it, or the first read always fails.
 */
export function InsertChipScreen({ bench }: { bench: Bench }) {
  return (
    <section className="card" style={{ maxWidth: 720 }}>
      <div className="card__head">
        <span aria-hidden="true" style={{ fontSize: 18 }}>
          ⚙
        </span>
        <h2 className="card__title">Inserir microcontrolador</h2>
      </div>
      <p className="card__description">Encaixe o ATmega328P no ZIF socket antes de continuar.</p>

      <div className="zif" style={{ marginTop: 18 }}>
        <div className="zif__box">ZIF vazio</div>
        <div className="zif__arrow" aria-hidden="true">
          ▶▶
        </div>
        <div className="zif__box" style={{ color: 'var(--success)', fontWeight: 600 }}>
          Chip inserido ✓
        </div>
      </div>

      <div className="notice" style={{ marginTop: 18 }}>
        <span aria-hidden="true">⚠ </span>
        Certifique-se que a alavanca do ZIF está levantada, insira o chip com o{' '}
        <strong>pin 1 no canto marcado</strong> e baixe a alavanca para fixar.
      </div>

      <div className="row" style={{ marginTop: 18, justifyContent: 'flex-end' }}>
        <button type="button" className="button" onClick={bench.backToVerification}>
          Voltar
        </button>
        <button
          type="button"
          className="button button--primary"
          onClick={() => void bench.confirmChipInserted()}
        >
          Chip inserido — Continuar
        </button>
      </div>
    </section>
  );
}
