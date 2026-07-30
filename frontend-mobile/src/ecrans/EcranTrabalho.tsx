import { useEffect, useState } from 'react';
import { Linking, Text, View } from 'react-native';
import type { Bancada, Copia, Leitura } from '@atmegapesta/partilhado';
import { Aviso, Botao, Cartao, Dado, Propriedade } from '../componentes/Basicos';
import { Dialogo } from '../componentes/Dialogo';
import { ListaTestes, MapaFlash, MapaPinos } from '../componentes/Paineis';
import { cores, estilos } from '../estilos';

/** Que diálogo está aberto. Um de cada vez — são todos decisões que travam o fluxo. */
type DialogoAberto = 'nenhum' | 'transferencia' | 'altaTensao' | 'semAltaTensao';

/**
 * O ecrã de trabalho: ler as configurações do chip-alvo e, quando essa leitura sai
 * inteira, correr a verificação de integridade.
 */
export function EcranTrabalho({ bancada }: { bancada: Bancada }) {
  const { leitura, integridade, catalogo, ocupacao, progresso, erro } = bancada;
  const [dialogo, setDialogo] = useState<DialogoAberto>('nenhum');

  // Esgotadas as tentativas por ISP, a única via que resta é a alta tensão. Abre-se aqui
  // porque a leitura já acabou e o barramento já está isolado.
  useEffect(() => {
    if (bancada.escalarAltaTensao) setDialogo('altaTensao');
  }, [bancada.escalarAltaTensao]);

  const aLer = ocupacao === 'ler';
  const aVerificar = ocupacao === 'integridade';

  return (
    <>
      <Cartao
        titulo="Configurações atuais"
        estado={aLer ? (progresso?.texto ?? 'A ler o chip-alvo...') : leitura?.estado}
        severidade={aLer ? 'aviso' : leitura?.severidade}
      >
        {erro && <Aviso erro>{erro}</Aviso>}

        {leitura && !leitura.identificado && <PainelFalha leitura={leitura} bancada={bancada} />}
        {leitura?.identificado && <DadosLeitura leitura={leitura} />}

        {leitura && !leitura.barramentoIsolado && (
          <Aviso>Falha ao isolar o barramento — o chip-alvo pode ter ficado ligado ao ISP.</Aviso>
        )}

        <View style={estilos.linha}>
          <Botao
            titulo={aLer ? 'A ler...' : leitura ? 'Ler novamente' : 'Detetar'}
            desactivado={aLer || aVerificar}
            aoPremir={() => void bancada.lerConfiguracoes()}
          />
        </View>
      </Cartao>

      {bancada.podeVerificarIntegridade && catalogo && (
        <Cartao
          titulo="Verificação de integridade"
          estado={
            aVerificar
              ? (progresso?.texto ?? 'A preparar a verificação...')
              : (integridade?.mensagem ??
                'Exercita os pinos do ATmega328P a partir do ATmega2560. Requer transferir uma aplicação verificadora para a Flash do chip-alvo.')
          }
          severidade={aVerificar ? 'aviso' : integridade?.severidade}
        >
          <Aviso>{catalogo.aviso}</Aviso>

          <View style={estilos.subcartao}>
            <MapaPinos catalogo={catalogo} />
          </View>

          <View style={{ gap: 6 }}>
            <View style={[estilos.linha, { justifyContent: 'space-between' }]}>
              <Text style={estilos.rotulo}>Progresso</Text>
              <Text style={[estilos.texto, estilos.mono]}>
                {integridade?.progressoPct ?? 0}%
              </Text>
            </View>
            <View style={estilos.progresso}>
              <View
                style={[estilos.progressoBarra, { width: `${integridade?.progressoPct ?? 0}%` }]}
              />
            </View>
          </View>

          <ListaTestes catalogo={catalogo} resultados={integridade?.resultados ?? null} />

          {integridade?.copia && <FichaCopia copia={integridade.copia} bancada={bancada} />}

          {integridade && !integridade.barramentoIsolado && (
            <Aviso>
              Falha ao isolar o barramento — o ATmega2560 pode ter ficado a conduzir o
              barramento.
            </Aviso>
          )}

          <View style={estilos.linha}>
            <Botao
              titulo={aVerificar ? 'A verificar...' : 'Iniciar verificação'}
              principal
              desactivado={aLer || aVerificar}
              aoPremir={() => setDialogo('transferencia')}
            />
          </View>
        </Cartao>
      )}

      <Dialogo
        aberto={dialogo === 'transferencia'}
        titulo="É necessário programar o microcontrolador"
        aoFechar={() => setDialogo('nenhum')}
        acoes={
          <>
            <Botao
              titulo="Guardar cópia e prosseguir"
              principal
              aoPremir={() => {
                setDialogo('nenhum');
                void bancada.executarIntegridade('prosseguirComCopia');
              }}
            />
            <Botao
              titulo="Prosseguir sem cópia"
              aoPremir={() => {
                setDialogo('nenhum');
                void bancada.executarIntegridade('prosseguirSemCopia');
              }}
            />
            <Botao titulo="Cancelar" aoPremir={() => setDialogo('nenhum')} />
          </>
        }
      >
        <Text style={estilos.texto}>
          A verificação de integridade não se faz por leitura. Para se poder exercitar os
          pinos, o ATmega328P tem de correr uma aplicação verificadora, que é transferida
          para a sua memória Flash através do USBAsp.
        </Text>
        <Text style={[estilos.texto, { fontWeight: '700', color: cores.textoTitulo }]}>
          Essa transferência substitui o programa que o seu microcontrolador tem agora e pode
          apagar os dados guardados na EEPROM. Não há como desfazer.
        </Text>
        <Text style={estilos.texto}>
          Antes de prosseguir pode guardar uma cópia da Flash e da EEPROM. É a única forma de
          mais tarde repor o que lá está hoje.
        </Text>
        <Text style={estilos.textoPequeno}>
          A cópia fica no computador da bancada, em {bancada.config?.pastaCopias ?? '...'}.
        </Text>
      </Dialogo>

      <Dialogo
        aberto={dialogo === 'altaTensao'}
        titulo="Restauro das configurações"
        aoFechar={() => setDialogo('semAltaTensao')}
        acoes={
          <>
            <Botao titulo="Sim" principal aoPremir={() => setDialogo('nenhum')} />
            <Botao titulo="Não" aoPremir={() => setDialogo('semAltaTensao')} />
          </>
        }
      >
        <Text style={estilos.texto}>
          Para restaurar as configurações é necessário efetuar a programação de alta tensão.
          Gostaria de efetuar essa programação agora?
        </Text>
        <Text style={estilos.textoPequeno}>
          O chip-alvo não respondeu ao ISP em {leitura?.maxTentativas ?? 3} tentativas.
        </Text>
        <Aviso>
          A programação de alta tensão está por implementar — também não existe no WPF nem
          no front end web. Dizer "Sim" apenas fecha este aviso.
        </Aviso>
      </Dialogo>

      <Dialogo
        aberto={dialogo === 'semAltaTensao'}
        titulo="Sem restauro das configurações"
        aoFechar={() => setDialogo('nenhum')}
        acoes={
          <>
            <Botao
              titulo="Recomeçar com outra peça"
              principal
              aoPremir={() => {
                setDialogo('nenhum');
                void bancada.reiniciarCiclo();
              }}
            />
            <Botao titulo="Continuar" aoPremir={() => setDialogo('nenhum')} />
          </>
        }
      >
        <Text style={estilos.texto}>
          Sem a programação de alta tensão não é possível restaurar as configurações deste
          microcontrolador.
        </Text>
      </Dialogo>
    </>
  );
}

/** O chip não respondeu ao ISP: o que fazer, e quantas tentativas restam. */
function PainelFalha({ leitura, bancada }: { leitura: Leitura; bancada: Bancada }) {
  return (
    <View style={estilos.pilha}>
      <Aviso erro>
        {leitura.instrucao ??
          'Verifique se o microcontrolador está corretamente inserido no ZIF socket.'}
      </Aviso>

      {leitura.tentativas && <Text style={estilos.textoPequeno}>{leitura.tentativas}</Text>}

      {leitura.saidaAvrdude && <SaidaAvrdude texto={leitura.saidaAvrdude} />}

      <View style={estilos.linha}>
        <Botao
          titulo="Tentar detetar novamente"
          principal
          desactivado={leitura.esgotado || bancada.ocupacao !== 'nada'}
          aoPremir={() => void bancada.lerConfiguracoes()}
        />
      </View>
    </View>
  );
}

/** O que a leitura identificou: o chip, os fuses, e o que eles significam. */
function DadosLeitura({ leitura }: { leitura: Leitura }) {
  const { mcu, fuses, descodificado, mapaFlash } = leitura;

  return (
    <View style={estilos.pilha}>
      {mcu && (
        <View style={[estilos.subcartao, estilos.grelha]}>
          <Dado rotulo="MCU" valor={mcu.nome} />
          <Dado rotulo="Assinatura" valor={leitura.assinatura ?? '—'} mono />
          <Dado rotulo="Flash" valor={mcu.flash} />
          <Dado rotulo="EEPROM" valor={mcu.eeprom} />
          <Dado rotulo="SRAM" valor={mcu.sram} />
        </View>
      )}

      {fuses && (
        <View style={estilos.grelha}>
          <Fuse nome="lfuse" valor={fuses.low} significado={descodificado?.descricaoLow} />
          <Fuse nome="hfuse" valor={fuses.high} significado={descodificado?.descricaoHigh} />
          <Fuse nome="efuse" valor={fuses.extended} significado={descodificado?.descricaoExtended} />
          <Fuse nome="lock" valor={fuses.lock} significado={descodificado?.descricaoLock} />
        </View>
      )}

      {descodificado && (
        <View>
          <Propriedade nome="Relógio" valor={descodificado.relogio} />
          <Propriedade
            nome="CKDIV8"
            valor={descodificado.ckdiv8Activo ? 'activo — clock ÷8' : 'desligado'}
          />
          <Propriedade nome="Brown-out" valor={descodificado.brownOut} />
          <Propriedade
            nome="ISP (SPIEN)"
            valor={descodificado.spiActivo ? 'habilitado' : 'desactivado'}
          />
          <Propriedade
            nome="RESET (RSTDISBL)"
            valor={descodificado.resetActivo ? 'activo' : 'desactivado'}
          />
          <Propriedade
            nome="EEPROM em erase"
            valor={descodificado.eepromPreservada ? 'preservada' : 'apagada'}
          />
          <Propriedade nome="Bloqueio" valor={descodificado.bloqueio} />
        </View>
      )}

      {mapaFlash && (
        <View style={estilos.subcartao}>
          <MapaFlash mapa={mapaFlash} />
        </View>
      )}

      {leitura.saidaAvrdude && <SaidaAvrdude texto={leitura.saidaAvrdude} />}

      <Text style={estilos.textoPequeno}>
        Leitura apenas — esta aplicação nunca grava fuses.
      </Text>
    </View>
  );
}

/**
 * A cópia guardada. Os ficheiros descarregam-se pelo browser do telemóvel: a cópia está
 * no computador da bancada, e abrir o URL deixa o gestor de downloads do Android tratar
 * do resto.
 */
function FichaCopia({ copia, bancada }: { copia: Copia; bancada: Bancada }) {
  return (
    <View style={estilos.subcartao}>
      <Text style={estilos.rotulo}>Cópia de segurança</Text>
      <Text style={estilos.texto}>{copia.mensagem}</Text>

      {copia.ficheiros.map((f) => (
        <Botao
          key={f.nome}
          titulo={`Descarregar ${f.nome}`}
          aoPremir={() => void Linking.openURL(bancada.urlFicheiro(f.url))}
        />
      ))}
    </View>
  );
}

function Fuse({
  nome,
  valor,
  significado,
}: {
  nome: string;
  valor: string | null;
  significado?: string | undefined;
}) {
  return (
    <View style={estilos.fuse}>
      <Text style={estilos.rotulo}>{nome}</Text>
      <Text style={estilos.fuseValor}>{valor ?? 'n/d'}</Text>
      {significado && <Text style={estilos.textoPequeno}>{significado}</Text>}
    </View>
  );
}

/** A saída crua do avrdude. À bancada é ruído, no diagnóstico é a pista. */
function SaidaAvrdude({ texto }: { texto: string }) {
  const [aberto, setAberto] = useState(false);

  return (
    <View style={{ gap: 6 }}>
      <Botao
        titulo={aberto ? 'Esconder saída do avrdude' : 'Ver saída do avrdude'}
        aoPremir={() => setAberto((v) => !v)}
      />
      {aberto && (
        <View style={estilos.saida}>
          <Text style={estilos.saidaTexto}>{texto}</Text>
        </View>
      )}
    </View>
  );
}
