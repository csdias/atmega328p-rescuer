import { useEffect, useState } from 'react';
import { Linking, Text, View } from 'react-native';
import type { Bench, Backup, Read } from '@atmegapesta/shared';
import { Notice, Botao, Card, Datum, Propriedade } from '../components/Basics';
import { Dialog } from '../components/Dialog';
import { ListaTestes, FlashMap, PinMap } from '../components/Panels';
import { colours, estilos } from '../styles';

/** Which dialog is open. One at a time — they are all decisions that stop the flow. */
type DialogoAberto = 'nenhum' | 'transferencia' | 'altaTensao' | 'semAltaTensao';

/**
 * The work screen: read the target chip's settings and, when that read comes out whole,
 * run the integrity check.
 */
export function WorkScreen({ bench }: { bench: Bench }) {
  const { read, integrity, catalog, busy, progress, error } = bench;
  const [dialog, setDialogo] = useState<DialogoAberto>('nenhum');

  // With the ISP attempts spent, the only way left is high voltage. It opens here
  // because the read is over and the bus is already isolated.
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
      >
        {error && <Notice error>{error}</Notice>}

        {read && !read.identified && <PainelFalha read={read} bench={bench} />}
        {read?.identified && <ReadData read={read} />}

        {read && !read.busIsolated && (
          <Notice>Falha ao isolar o barramento — o chip-alvo pode ter ficado ligado ao ISP.</Notice>
        )}

        <View style={estilos.row}>
          <Botao
            title={aLer ? 'A ler...' : read ? 'Ler novamente' : 'Detetar'}
            desactivado={aLer || aVerificar}
            onPress={() => void bench.readSettings()}
          />
        </View>
      </Card>

      {bench.canCheckIntegrity && catalog && (
        <Card
          title="Verificação de integridade"
          state={
            aVerificar
              ? (progress?.text ?? 'A preparar a verificação...')
              : (integrity?.message ??
                'Exercita os pinos do ATmega328P a partir do ATmega2560. Requer transferir uma aplicação verificadora para a Flash do chip-alvo.')
          }
          severity={aVerificar ? 'warning' : integrity?.severity}
        >
          <Notice>{catalog.warning}</Notice>

          <View style={estilos.subcard}>
            <PinMap catalog={catalog} />
          </View>

          <View style={{ gap: 6 }}>
            <View style={[estilos.row, { justifyContent: 'space-between' }]}>
              <Text style={estilos.label}>Progress</Text>
              <Text style={[estilos.text, estilos.mono]}>
                {integrity?.progressPct ?? 0}%
              </Text>
            </View>
            <View style={estilos.progress}>
              <View
                style={[estilos.progressBar, { width: `${integrity?.progressPct ?? 0}%` }]}
              />
            </View>
          </View>

          <ListaTestes catalog={catalog} results={integrity?.results ?? null} />

          {integrity?.backup && <FichaCopia backup={integrity.backup} bench={bench} />}

          {integrity && !integrity.busIsolated && (
            <Notice>
              Falha ao isolar o barramento — o ATmega2560 pode ter ficado a conduzir o
              barramento.
            </Notice>
          )}

          <View style={estilos.row}>
            <Botao
              title={aVerificar ? 'A verificar...' : 'Iniciar verificação'}
              principal
              desactivado={aLer || aVerificar}
              onPress={() => setDialogo('transferencia')}
            />
          </View>
        </Card>
      )}

      <Dialog
        open={dialog === 'transferencia'}
        title="É necessário programar o microcontrolador"
        onClose={() => setDialogo('nenhum')}
        actions={
          <>
            <Botao
              title="Guardar cópia e prosseguir"
              principal
              onPress={() => {
                setDialogo('nenhum');
                void bench.runIntegrity('proceedWithBackup');
              }}
            />
            <Botao
              title="Prosseguir sem cópia"
              onPress={() => {
                setDialogo('nenhum');
                void bench.runIntegrity('proceedWithoutBackup');
              }}
            />
            <Botao title="Cancelar" onPress={() => setDialogo('nenhum')} />
          </>
        }
      >
        <Text style={estilos.text}>
          A verificação de integrity não se faz por read. Para se poder exercitar os
          pins, o ATmega328P tem de correr uma aplicação verificadora, que é transferida
          para a sua memória Flash através do USBAsp.
        </Text>
        <Text style={[estilos.text, { fontWeight: '700', color: colours.textTitle }]}>
          Essa transferência substitui o programa que o seu microcontrolador tem now e pode
          apagar os dados guardados na EEPROM. Não há como desfazer.
        </Text>
        <Text style={estilos.text}>
          Antes de prosseguir pode guardar uma cópia da Flash e da EEPROM. É a única forma de
          mais tarde repor o que lá está hoje.
        </Text>
        <Text style={estilos.smallText}>
          A cópia fica no computador da bench, em {bench.config?.backupFolder ?? '...'}.
        </Text>
      </Dialog>

      <Dialog
        open={dialog === 'altaTensao'}
        title="Restauro das configurações"
        onClose={() => setDialogo('semAltaTensao')}
        actions={
          <>
            <Botao title="Sim" principal onPress={() => setDialogo('nenhum')} />
            <Botao title="Não" onPress={() => setDialogo('semAltaTensao')} />
          </>
        }
      >
        <Text style={estilos.text}>
          Para restaurar as configurações é necessário efetuar a programação de alta tensão.
          Gostaria de efetuar essa programação now?
        </Text>
        <Text style={estilos.smallText}>
          O chip-alvo não respondeu ao ISP em {read?.maxAttempts ?? 3} attempts.
        </Text>
        <Notice>
          A programação de alta tensão está por implementar — também não existe no WPF nem
          no front end web. Dizer "Sim" apenas fecha este warning.
        </Notice>
      </Dialog>

      <Dialog
        open={dialog === 'semAltaTensao'}
        title="Sem restauro das configurações"
        onClose={() => setDialogo('nenhum')}
        actions={
          <>
            <Botao
              title="Recomeçar com outra peça"
              principal
              onPress={() => {
                setDialogo('nenhum');
                void bench.resetCycle();
              }}
            />
            <Botao title="Continuar" onPress={() => setDialogo('nenhum')} />
          </>
        }
      >
        <Text style={estilos.text}>
          Sem a programação de alta tensão não é possível restaurar as configurações deste
          microcontrolador.
        </Text>
      </Dialog>
    </>
  );
}

/** The chip did not answer ISP: what to do, and how many attempts are left. */
function PainelFalha({ read, bench }: { read: Read; bench: Bench }) {
  return (
    <View style={estilos.stack}>
      <Notice error>
        {read.instruction ??
          'Verifique se o microcontrolador está corretamente inserido no ZIF socket.'}
      </Notice>

      {read.attempts && <Text style={estilos.smallText}>{read.attempts}</Text>}

      {read.avrdudeOutput && <AvrdudeOutput text={read.avrdudeOutput} />}

      <View style={estilos.row}>
        <Botao
          title="Tentar detetar novamente"
          principal
          desactivado={read.exhausted || bench.busy !== 'none'}
          onPress={() => void bench.readSettings()}
        />
      </View>
    </View>
  );
}

/** O que a leitura identificou: o chip, os fuses, e o que eles significam. */
function ReadData({ read }: { read: Read }) {
  const { mcu, fuses, decoded, flashMap } = read;

  return (
    <View style={estilos.stack}>
      {mcu && (
        <View style={[estilos.subcard, estilos.grid]}>
          <Datum label="MCU" value={mcu.name} />
          <Datum label="Assinatura" value={read.signature ?? '—'} mono />
          <Datum label="Flash" value={mcu.flash} />
          <Datum label="EEPROM" value={mcu.eeprom} />
          <Datum label="SRAM" value={mcu.sram} />
        </View>
      )}

      {fuses && (
        <View style={estilos.grid}>
          <Fuse name="lfuse" value={fuses.low} significado={decoded?.lowDescription} />
          <Fuse name="hfuse" value={fuses.high} significado={decoded?.highDescription} />
          <Fuse name="efuse" value={fuses.extended} significado={decoded?.extendedDescription} />
          <Fuse name="lock" value={fuses.lock} significado={decoded?.lockDescription} />
        </View>
      )}

      {decoded && (
        <View>
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
        </View>
      )}

      {flashMap && (
        <View style={estilos.subcard}>
          <FlashMap mapa={flashMap} />
        </View>
      )}

      {read.avrdudeOutput && <AvrdudeOutput text={read.avrdudeOutput} />}

      <Text style={estilos.smallText}>
        Read apenas — esta aplicação nunca grava fuses.
      </Text>
    </View>
  );
}

/**
 * The saved backup. The files are downloaded through the phone's browser: the backup is on
 * the bench computer, and opening the URL lets Android's download manager handle the rest.
 */
function FichaCopia({ backup, bench }: { backup: Backup; bench: Bench }) {
  return (
    <View style={estilos.subcard}>
      <Text style={estilos.label}>Cópia de segurança</Text>
      <Text style={estilos.text}>{backup.message}</Text>

      {backup.files.map((f) => (
        <Botao
          key={f.name}
          title={`Descarregar ${f.name}`}
          onPress={() => void Linking.openURL(bench.fileUrl(f.url))}
        />
      ))}
    </View>
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
    <View style={estilos.fuse}>
      <Text style={estilos.label}>{name}</Text>
      <Text style={estilos.fuseValue}>{value ?? 'n/d'}</Text>
      {significado && <Text style={estilos.smallText}>{significado}</Text>}
    </View>
  );
}

/** avrdude's raw output. At the bench it is noise, in diagnosis it is the clue. */
function AvrdudeOutput({ text }: { text: string }) {
  const [open, setOpen] = useState(false);

  return (
    <View style={{ gap: 6 }}>
      <Botao
        title={open ? 'Esconder saída do avrdude' : 'Ver saída do avrdude'}
        onPress={() => setOpen((v) => !v)}
      />
      {open && (
        <View style={estilos.output}>
          <Text style={estilos.outputText}>{text}</Text>
        </View>
      )}
    </View>
  );
}
