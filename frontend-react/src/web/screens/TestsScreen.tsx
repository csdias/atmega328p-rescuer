import { useEffect, useState } from 'react';
import type { Bench, Backup, Read } from '@atmegapesta/shared';
import { Card } from '../components/Card';
import { Dialog } from '../components/Dialog';
import { FlashMap } from '../components/FlashMap';
import { PinMap } from '../components/PinMap';
import { TestTable } from '../components/TestTable';

/** Which dialog is open. One at a time — they are all decisions that stop the flow. */
type DialogoAberto = 'nenhum' | 'transferencia' | 'altaTensao' | 'semAltaTensao';

/**
 * The work screen: read the target chip's settings and, when that read comes out whole,
 * run the integrity check.
 */
export function TestsScreen({
  bench,
  onOpenHighVoltage,
}: {
  bench: Bench;
  onOpenHighVoltage: () => void;
}) {
  const { read, integrity, catalog, busy, progress, error } = bench;
  const [dialog, setDialogo] = useState<DialogoAberto>('nenhum');

  // With the ISP attempts spent, the only way left is high voltage. The dialog opens
  // on its own because the read is over and the bus is already isolated — as in the
  // WPF, which did not open the modal with ISP still connected to the target.
  useEffect(() => {
    if (bench.escalateHighVoltage) setDialogo('altaTensao');
  }, [bench.escalateHighVoltage]);

  const aLer = busy === 'read';
  const aVerificar = busy === 'integrity';

  return (
    <>
      <Card
        title="Configurações atuais"
        state={aLer ? (progress?.text ?? 'A ler o chip-alvo...') : read?.state}
        severity={aLer ? 'warning' : read?.severity}
        actions={
          <button
            type="button"
            className="button"
            onClick={() => void bench.readSettings()}
            disabled={aLer || aVerificar}
          >
            {aLer ? 'A ler...' : read ? 'Ler novamente' : 'Detetar'}
          </button>
        }
      >
        {error && <p className="notice notice--error" style={{ margin: 0 }}>{error}</p>}

        {read && !read.identified && <PainelFalha read={read} bench={bench} />}

        {read?.identified && <ReadData read={read} />}

        {read && !read.busIsolated && (
          <p className="notice" style={{ margin: 0 }}>
            <span aria-hidden="true">⚠ </span>
            Falha ao isolar o barramento — o chip-alvo pode ter ficado ligado ao ISP.
          </p>
        )}
      </Card>

      {bench.canCheckIntegrity && (
        <Card
          title="Verificação de integridade"
          state={
            aVerificar
              ? (progress?.text ?? 'A preparar a verificação...')
              : (integrity?.message ??
                'Exercita os pinos do ATmega328P a partir do ATmega2560. Requer transferir uma aplicação verificadora para a Flash do chip-alvo.')
          }
          severity={aVerificar ? 'warning' : integrity?.severity}
          actions={
            <button
              type="button"
              className="button button--primary"
              onClick={() => setDialogo('transferencia')}
              disabled={aLer || aVerificar}
            >
              {aVerificar ? 'A verificar...' : 'Iniciar verificação'}
            </button>
          }
        >
          {catalog && (
            <>
              <p className="notice" style={{ margin: 0 }}>
                <span aria-hidden="true">⚠ </span>
                {catalog.warning}
              </p>

              <div className="subcard">
                <PinMap catalog={catalog} />
              </div>

              <div>
                <div className="row" style={{ justifyContent: 'space-between', marginBottom: 6 }}>
                  <span className="datum__label">Progress</span>
                  <span className="mono">{integrity?.progressPct ?? 0}%</span>
                </div>
                <div className="progress">
                  <div
                    className="progress__bar"
                    style={{ width: `${integrity?.progressPct ?? 0}%` }}
                  />
                </div>
              </div>

              <TestTable catalog={catalog} results={integrity?.results ?? null} />
            </>
          )}

          {integrity?.backup && <FichaCopia backup={integrity.backup} bench={bench} />}

          {integrity && !integrity.busIsolated && (
            <p className="notice" style={{ margin: 0 }}>
              <span aria-hidden="true">⚠ </span>
              Falha ao isolar o barramento — o ATmega2560 pode ter ficado a conduzir o
              barramento.
            </p>
          )}
        </Card>
      )}

      <Dialog
        open={dialog === 'transferencia'}
        title="É necessário programar o microcontrolador"
        icon="⚠"
        onClose={() => setDialogo('nenhum')}
        actions={
          <>
            <button
              type="button"
              className="button button--primary"
              onClick={() => {
                setDialogo('nenhum');
                void bench.runIntegrity('proceedWithBackup');
              }}
            >
              Guardar cópia e prosseguir
            </button>
            <button
              type="button"
              className="button"
              onClick={() => {
                setDialogo('nenhum');
                void bench.runIntegrity('proceedWithoutBackup');
              }}
            >
              Prosseguir sem cópia
            </button>
            <button type="button" className="button" onClick={() => setDialogo('nenhum')}>
              Cancelar
            </button>
          </>
        }
      >
        <p style={{ margin: 0 }}>
          A verificação de integrity não se faz por read. Para se poder exercitar os
          pins, o ATmega328P tem de correr uma aplicação verificadora, que é transferida
          para a sua memória Flash através do USBAsp.
        </p>
        <p style={{ margin: 0 }}>
          <strong>
            Essa transferência substitui o programa que o seu microcontrolador tem now
          </strong>{' '}
          e pode apagar os dados guardados na EEPROM. Não há como desfazer.
        </p>
        <p style={{ margin: 0 }}>
          Antes de prosseguir pode guardar uma cópia da Flash e da EEPROM do seu
          microcontrolador. É a única forma de mais tarde repor o que lá está hoje.
        </p>
        <p className="subtle" style={{ margin: 0 }}>
          A cópia fica no computador da bench, em{' '}
          <span className="mono">{bench.config?.backupFolder ?? '...'}</span>, e pode ser
          descarregada a seguir.
        </p>
      </Dialog>

      <Dialog
        open={dialog === 'altaTensao'}
        title="Restauro das configurações"
        icon="⚡"
        onClose={() => setDialogo('semAltaTensao')}
        actions={
          <>
            <button
              type="button"
              className="button button--primary"
              onClick={() => {
                setDialogo('nenhum');
                onOpenHighVoltage();
              }}
            >
              Sim
            </button>
            <button type="button" className="button" onClick={() => setDialogo('semAltaTensao')}>
              Não
            </button>
          </>
        }
      >
        <p style={{ margin: 0 }}>
          Para restaurar as configurações é necessário efetuar a programação de alta tensão.
          Gostaria de efetuar essa programação now?
        </p>
        <p className="subtle" style={{ margin: 0 }}>
          O chip-alvo não respondeu ao ISP em {read?.maxAttempts ?? 3} attempts.
        </p>
      </Dialog>

      <Dialog
        open={dialog === 'semAltaTensao'}
        title="Sem restauro das configurações"
        onClose={() => setDialogo('nenhum')}
        actions={
          <>
            <button
              type="button"
              className="button button--primary"
              onClick={() => {
                setDialogo('nenhum');
                void bench.resetCycle();
              }}
            >
              Recomeçar com outra peça
            </button>
            <button type="button" className="button" onClick={() => setDialogo('nenhum')}>
              Continuar a navegar
            </button>
          </>
        }
      >
        <p style={{ margin: 0 }}>
          Sem a programação de alta tensão não é possível restaurar as configurações deste
          microcontrolador.
        </p>
        <p className="subtle" style={{ margin: 0 }}>
          Recomeçar devolve as attempts de read e volta à verificação de dispositivos.
        </p>
      </Dialog>
    </>
  );
}

/** The chip did not answer ISP: what to do, and how many attempts are left. */
function PainelFalha({ read, bench }: { read: Read; bench: Bench }) {
  return (
    <div className="stack">
      <p className="notice notice--error" style={{ margin: 0 }}>
        {read.instruction ??
          'Verifique se o microcontrolador está corretamente inserido no ZIF socket.'}
      </p>

      {read.attempts && (
        <p className="subtle" style={{ margin: 0 }}>
          {read.attempts}
        </p>
      )}

      <div className="row">
        <button
          type="button"
          className="button button--primary"
          onClick={() => void bench.readSettings()}
          disabled={read.exhausted || bench.busy !== 'none'}
        >
          Tentar detetar novamente
        </button>
      </div>

      {read.avrdudeOutput && <AvrdudeOutput text={read.avrdudeOutput} />}
    </div>
  );
}

/** O que a leitura identificou: o chip, os fuses, e o que eles significam. */
function ReadData({ read }: { read: Read }) {
  const { mcu, fuses, decoded, flashMap } = read;

  return (
    <div className="stack">
      {mcu && (
        <div className="subcard">
          <div className="data">
            <Datum label="Microcontrolador" value={mcu.name} />
            <Datum label="Assinatura" value={read.signature ?? '—'} mono />
            <Datum label="Flash" value={mcu.flash} />
            <Datum label="EEPROM" value={mcu.eeprom} />
            <Datum label="SRAM" value={mcu.sram} />
          </div>
        </div>
      )}

      {fuses && (
        <div className="fuses">
          <Fuse name="lfuse" value={fuses.low} significado={decoded?.lowDescription} />
          <Fuse name="hfuse" value={fuses.high} significado={decoded?.highDescription} />
          <Fuse name="efuse" value={fuses.extended} significado={decoded?.extendedDescription} />
          <Fuse name="lock" value={fuses.lock} significado={decoded?.lockDescription} />
        </div>
      )}

      {decoded && (
        <div className="properties">
          <Propriedade name="Relógio" value={decoded.clock} />
          <Propriedade
            name="CKDIV8"
            value={decoded.ckdiv8Enabled ? 'activo — clock ÷8' : 'desligado'}
          />
          <Propriedade name="Brown-out" value={decoded.brownOut} />
          <Propriedade
            name="ISP (SPIEN)"
            value={decoded.spiEnabled ? 'habilitado' : 'desactivado'}
          />
          <Propriedade
            name="RESET (RSTDISBL)"
            value={decoded.resetEnabled ? 'activo' : 'desactivado'}
          />
          <Propriedade
            name="EEPROM em erase"
            value={decoded.eepromPreserved ? 'preservada' : 'apagada'}
          />
          <Propriedade name="Bloqueio" value={decoded.lockLevel} />
        </div>
      )}

      {flashMap && (
        <div className="subcard">
          <FlashMap mapa={flashMap} />
        </div>
      )}

      {read.avrdudeOutput && <AvrdudeOutput text={read.avrdudeOutput} />}

      <p className="subtle" style={{ margin: 0 }}>
        Read apenas — esta aplicação nunca grava fuses.
      </p>
    </div>
  );
}

/** The backup that was saved and where it can be downloaded from. */
function FichaCopia({ backup, bench }: { backup: Backup; bench: Bench }) {
  return (
    <div className="subcard">
      <div className="datum__label">Cópia de segurança</div>
      <p style={{ margin: '4px 0 0', fontSize: 12 }}>{backup.message}</p>

      {backup.files.length > 0 && (
        <ul className="files">
          {backup.files.map((f) => (
            <li key={f.name}>
              <a href={bench.fileUrl(f.url)} download>
                {f.name}
              </a>{' '}
              <span className="subtle">{Math.max(1, Math.round(f.bytes / 1024))} KB</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function Datum({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <div>
      <div className="datum__label">{label}</div>
      <div className={`dado__valor${mono ?' mono' : ''}`}>{value}</div>
    </div>
  );
}

function Fuse({
  name,
  value,
  significado,
}: {
  name: string;
  value: string | null;
  significado?: string | undefined;
}) {
  return (
    <div className="fuse">
      <div className="fuse__name">{name}</div>
      <div className="fuse__value">{value ?? 'n/d'}</div>
      {significado && <div className="fuse__meaning">{significado}</div>}
    </div>
  );
}

function Propriedade({ name, value }: { name: string; value: string }) {
  return (
    <div className="property">
      <span className="property__name">{name}</span>
      <span className="property__value">{value}</span>
    </div>
  );
}

/** avrdude's raw output. Closed by default: at the bench it is noise, in diagnosis it is the clue. */
function AvrdudeOutput({ text }: { text: string }) {
  return (
    <details>
      <summary>Saída do avrdude</summary>
      <pre className="output" style={{ marginTop: 8 }}>
        {text}
      </pre>
    </details>
  );
}
