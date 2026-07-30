import type { Bancada } from '@atmegapesta/partilhado';

/**
 * O passo entre a bancada verificada e a leitura: o chip tem de estar no ZIF antes de o
 * ISP ir procurá-lo, senão a primeira leitura falha sempre.
 */
export function EcranInserirChip({ bancada }: { bancada: Bancada }) {
  return (
    <section className="cartao" style={{ maxWidth: 720 }}>
      <div className="cartao__cabeca">
        <span aria-hidden="true" style={{ fontSize: 18 }}>
          ⚙
        </span>
        <h2 className="cartao__titulo">Inserir microcontrolador</h2>
      </div>
      <p className="cartao__descricao">Encaixe o ATmega328P no ZIF socket antes de continuar.</p>

      <div className="zif" style={{ marginTop: 18 }}>
        <div className="zif__caixa">ZIF vazio</div>
        <div className="zif__seta" aria-hidden="true">
          ▶▶
        </div>
        <div className="zif__caixa" style={{ color: 'var(--sucesso)', fontWeight: 600 }}>
          Chip inserido ✓
        </div>
      </div>

      <div className="aviso" style={{ marginTop: 18 }}>
        <span aria-hidden="true">⚠ </span>
        Certifique-se que a alavanca do ZIF está levantada, insira o chip com o{' '}
        <strong>pino 1 no canto marcado</strong> e baixe a alavanca para fixar.
      </div>

      <div className="linha" style={{ marginTop: 18, justifyContent: 'flex-end' }}>
        <button type="button" className="botao" onClick={bancada.voltarAVerificacao}>
          Voltar
        </button>
        <button
          type="button"
          className="botao botao--principal"
          onClick={() => void bancada.confirmarChipInserido()}
        >
          Chip inserido — Continuar
        </button>
      </div>
    </section>
  );
}
