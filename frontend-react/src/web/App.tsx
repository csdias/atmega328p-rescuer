import { useEffect, useState } from 'react';
import { usarBancada } from '@atmegapesta/partilhado';
import { EcranEmConstrucao } from './ecrans/EcranEmConstrucao';
import { EcranInserirChip } from './ecrans/EcranInserirChip';
import { EcranTestes } from './ecrans/EcranTestes';
import { EcranVerificacao } from './ecrans/EcranVerificacao';

/** Os módulos do menu lateral, como no WPF. */
type Modulo = 'testes' | 'programacao' | 'configuracoes';

/**
 * A janela da bancada. A navegação é por estado e não por URL: o fluxo é sequencial e um
 * refresh a meio não deve devolver alguém a um passo cujo hardware já mudou de estado.
 * É também o padrão que o React Native vai usar.
 */
export function App() {
  const bancada = usarBancada();
  const [modulo, setModulo] = useState<Modulo>('testes');
  const relogio = usarRelogio();

  const verificada = bancada.fase === 'trabalho';

  return (
    <div className="app">
      <header className="cabecalho">
        <div>
          <h1 className="cabecalho__titulo">ATMegaPesta V1 — Banca de recuperação</h1>
          <div className="cabecalho__sub">
            ATmega328P via USBAsp · master ATmega2560 via CH340
            {bancada.deteccao?.portaCom && ` (${bancada.deteccao.portaCom})`}
          </div>
        </div>
        <time className="cabecalho__relogio">{relogio}</time>
      </header>

      <div className="corpo">
        <nav className="menu" aria-label="Módulos">
          <ItemMenu
            rotulo="Testes"
            activo={modulo === 'testes'}
            aoEscolher={() => setModulo('testes')}
          />
          <ItemMenu
            rotulo="Programação de alta tensão"
            activo={modulo === 'programacao'}
            // Enquanto a bancada não estiver verificada não há nada a programar.
            desactivado={!verificada}
            aoEscolher={() => setModulo('programacao')}
          />
          <ItemMenu
            rotulo="Configurações"
            activo={modulo === 'configuracoes'}
            desactivado={!verificada}
            aoEscolher={() => setModulo('configuracoes')}
          />
        </nav>

        <main className="conteudo">
          {bancada.fase === 'verificacao' && <EcranVerificacao bancada={bancada} />}
          {bancada.fase === 'inserirChip' && <EcranInserirChip bancada={bancada} />}

          {verificada && modulo === 'testes' && (
            <EcranTestes bancada={bancada} aoAbrirAltaTensao={() => setModulo('programacao')} />
          )}
          {verificada && modulo === 'programacao' && (
            <EcranEmConstrucao modulo="Programação de alta tensão" />
          )}
          {verificada && modulo === 'configuracoes' && <EcranEmConstrucao modulo="Configurações" />}
        </main>
      </div>
    </div>
  );
}

function ItemMenu({
  rotulo,
  activo,
  desactivado,
  aoEscolher,
}: {
  rotulo: string;
  activo: boolean;
  desactivado?: boolean;
  aoEscolher: () => void;
}) {
  return (
    <button
      type="button"
      className="menu__item"
      {...(activo ? { 'aria-current': 'page' as const } : {})}
      disabled={desactivado === true}
      onClick={aoEscolher}
    >
      {rotulo}
    </button>
  );
}

/** O relógio do cabeçalho, ao segundo, como o DispatcherTimer do WPF. */
function usarRelogio(): string {
  const [agora, setAgora] = useState(() => new Date());

  useEffect(() => {
    const id = setInterval(() => setAgora(new Date()), 1000);
    return () => clearInterval(id);
  }, []);

  return agora.toLocaleString('pt-PT', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
}
