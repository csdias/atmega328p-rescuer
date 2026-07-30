import { useEffect, useState } from 'react';
import { useBench } from '@atmegapesta/shared';
import { UnderConstructionScreen } from './screens/UnderConstructionScreen';
import { InsertChipScreen } from './screens/InsertChipScreen';
import { TestsScreen } from './screens/TestsScreen';
import { VerificationScreen } from './screens/VerificationScreen';

/** The side menu modules, as in the WPF. */
type Modulo = 'testes' | 'programacao' | 'configuracoes';

/**
 * The bench window. Navigation goes by state and not by URL: the flow is sequential and a
 * refresh halfway through should not put someone back at a step whose hardware has already
 * changed state. It is also the pattern React Native will use.
 */
export function App() {
  const bench = useBench();
  const [modulo, setModulo] = useState<Modulo>('testes');
  const clock = useClock();

  const verificada = bench.phase === 'work';

  return (
    <div className="app">
      <header className="header">
        <div>
          <h1 className="header__title">ATMegaPesta V1 — Banca de recuperação</h1>
          <div className="header__sub">
            ATmega328P via USBAsp · master ATmega2560 via CH340
            {bench.detection?.comPort && ` (${bench.detection.comPort})`}
          </div>
        </div>
        <time className="header__clock">{clock}</time>
      </header>

      <div className="body">
        <nav className="menu" aria-label="Módulos">
          <ItemMenu
            label="Testes"
            active={modulo === 'testes'}
            onChoose={() => setModulo('testes')}
          />
          <ItemMenu
            label="Programação de alta tensão"
            active={modulo === 'programacao'}
            // While the bench is not verified there is nothing to program.
            desactivado={!verificada}
            onChoose={() => setModulo('programacao')}
          />
          <ItemMenu
            label="Configurações"
            active={modulo === 'configuracoes'}
            desactivado={!verificada}
            onChoose={() => setModulo('configuracoes')}
          />
        </nav>

        <main className="content">
          {bench.phase === 'verification' && <VerificationScreen bench={bench} />}
          {bench.phase === 'insertChip' && <InsertChipScreen bench={bench} />}

          {verificada && modulo === 'testes' && (
            <TestsScreen bench={bench} onOpenHighVoltage={() => setModulo('programacao')} />
          )}
          {verificada && modulo === 'programacao' && (
            <UnderConstructionScreen modulo="Programação de alta tensão" />
          )}
          {verificada && modulo === 'configuracoes' && <UnderConstructionScreen modulo="Configurações" />}
        </main>
      </div>
    </div>
  );
}

function ItemMenu({
  label,
  active,
  desactivado,
  onChoose,
}: {
  label: string;
  active: boolean;
  desactivado?: boolean;
  onChoose: () => void;
}) {
  return (
    <button
      type="button"
      className="menu__item"
      {...(active ? { 'aria-current': 'page' as const } : {})}
      disabled={desactivado === true}
      onClick={onChoose}
    >
      {label}
    </button>
  );
}

/** The header clock, to the second, like the WPF's DispatcherTimer. */
function useClock(): string {
  const [now, setNow] = useState(() => new Date());

  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(id);
  }, []);

  return now.toLocaleString('pt-PT', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
}
