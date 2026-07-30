import { useEffect, useState } from 'react';
import type { Copia, Leitura } from '../../partilhado/contratos';
import type { Bancada } from '../../partilhado/usarBancada';
import { Cartao } from '../componentes/Cartao';
import { Dialogo } from '../componentes/Dialogo';
import { MapaFlash } from '../componentes/MapaFlash';
import { MapaPinos } from '../componentes/MapaPinos';
import { TabelaTestes } from '../componentes/TabelaTestes';

/** Que diálogo está aberto. Um de cada vez — são todos decisões que travam o fluxo. */
type DialogoAberto = 'nenhum' | 'transferencia' | 'altaTensao' | 'semAltaTensao';

/**
 * O ecrã de trabalho: ler as configurações do chip-alvo e, quando essa leitura sai
 * inteira, correr a verificação de integridade.
 */
export function EcranTestes({
  bancada,
  aoAbrirAltaTensao,
}: {
  bancada: Bancada;
  aoAbrirAltaTensao: () => void;
}) {
  const { leitura, integridade, catalogo, ocupacao, progresso, erro } = bancada;
  const [dialogo, setDialogo] = useState<DialogoAberto>('nenhum');

  // Esgotadas as tentativas por ISP, a única via que resta é a alta tensão. O diálogo
  // abre-se sozinho porque a leitura já acabou e o barramento já está isolado — como no
  // WPF, que não abria o modal com o ISP ainda ligado ao alvo.
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
        acoes={
          <button
            type="button"
            className="botao"
            onClick={() => void bancada.lerConfiguracoes()}
            disabled={aLer || aVerificar}
          >
            {aLer ? 'A ler...' : leitura ? 'Ler novamente' : 'Detetar'}
          </button>
        }
      >
        {erro && <p className="aviso aviso--erro" style={{ margin: 0 }}>{erro}</p>}

        {leitura && !leitura.identificado && <PainelFalha leitura={leitura} bancada={bancada} />}

        {leitura?.identificado && <DadosLeitura leitura={leitura} />}

        {leitura && !leitura.barramentoIsolado && (
          <p className="aviso" style={{ margin: 0 }}>
            <span aria-hidden="true">⚠ </span>
            Falha ao isolar o barramento — o chip-alvo pode ter ficado ligado ao ISP.
          </p>
        )}
      </Cartao>

      {bancada.podeVerificarIntegridade && (
        <Cartao
          titulo="Verificação de integridade"
          estado={
            aVerificar
              ? (progresso?.texto ?? 'A preparar a verificação...')
              : (integridade?.mensagem ??
                'Exercita os pinos do ATmega328P a partir do ATmega2560. Requer transferir uma aplicação verificadora para a Flash do chip-alvo.')
          }
          severidade={aVerificar ? 'aviso' : integridade?.severidade}
          acoes={
            <button
              type="button"
              className="botao botao--principal"
              onClick={() => setDialogo('transferencia')}
              disabled={aLer || aVerificar}
            >
              {aVerificar ? 'A verificar...' : 'Iniciar verificação'}
            </button>
          }
        >
          {catalogo && (
            <>
              <p className="aviso" style={{ margin: 0 }}>
                <span aria-hidden="true">⚠ </span>
                {catalogo.aviso}
              </p>

              <div className="subcartao">
                <MapaPinos catalogo={catalogo} />
              </div>

              <div>
                <div className="linha" style={{ justifyContent: 'space-between', marginBottom: 6 }}>
                  <span className="dado__rotulo">Progresso</span>
                  <span className="mono">{integridade?.progressoPct ?? 0}%</span>
                </div>
                <div className="progresso">
                  <div
                    className="progresso__barra"
                    style={{ width: `${integridade?.progressoPct ?? 0}%` }}
                  />
                </div>
              </div>

              <TabelaTestes catalogo={catalogo} resultados={integridade?.resultados ?? null} />
            </>
          )}

          {integridade?.copia && <FichaCopia copia={integridade.copia} bancada={bancada} />}

          {integridade && !integridade.barramentoIsolado && (
            <p className="aviso" style={{ margin: 0 }}>
              <span aria-hidden="true">⚠ </span>
              Falha ao isolar o barramento — o ATmega2560 pode ter ficado a conduzir o
              barramento.
            </p>
          )}
        </Cartao>
      )}

      <Dialogo
        aberto={dialogo === 'transferencia'}
        titulo="É necessário programar o microcontrolador"
        icone="⚠"
        aoFechar={() => setDialogo('nenhum')}
        acoes={
          <>
            <button
              type="button"
              className="botao botao--principal"
              onClick={() => {
                setDialogo('nenhum');
                void bancada.executarIntegridade('prosseguirComCopia');
              }}
            >
              Guardar cópia e prosseguir
            </button>
            <button
              type="button"
              className="botao"
              onClick={() => {
                setDialogo('nenhum');
                void bancada.executarIntegridade('prosseguirSemCopia');
              }}
            >
              Prosseguir sem cópia
            </button>
            <button type="button" className="botao" onClick={() => setDialogo('nenhum')}>
              Cancelar
            </button>
          </>
        }
      >
        <p style={{ margin: 0 }}>
          A verificação de integridade não se faz por leitura. Para se poder exercitar os
          pinos, o ATmega328P tem de correr uma aplicação verificadora, que é transferida
          para a sua memória Flash através do USBAsp.
        </p>
        <p style={{ margin: 0 }}>
          <strong>
            Essa transferência substitui o programa que o seu microcontrolador tem agora
          </strong>{' '}
          e pode apagar os dados guardados na EEPROM. Não há como desfazer.
        </p>
        <p style={{ margin: 0 }}>
          Antes de prosseguir pode guardar uma cópia da Flash e da EEPROM do seu
          microcontrolador. É a única forma de mais tarde repor o que lá está hoje.
        </p>
        <p className="subtil" style={{ margin: 0 }}>
          A cópia fica no computador da bancada, em{' '}
          <span className="mono">{bancada.config?.pastaCopias ?? '...'}</span>, e pode ser
          descarregada a seguir.
        </p>
      </Dialogo>

      <Dialogo
        aberto={dialogo === 'altaTensao'}
        titulo="Restauro das configurações"
        icone="⚡"
        aoFechar={() => setDialogo('semAltaTensao')}
        acoes={
          <>
            <button
              type="button"
              className="botao botao--principal"
              onClick={() => {
                setDialogo('nenhum');
                aoAbrirAltaTensao();
              }}
            >
              Sim
            </button>
            <button type="button" className="botao" onClick={() => setDialogo('semAltaTensao')}>
              Não
            </button>
          </>
        }
      >
        <p style={{ margin: 0 }}>
          Para restaurar as configurações é necessário efetuar a programação de alta tensão.
          Gostaria de efetuar essa programação agora?
        </p>
        <p className="subtil" style={{ margin: 0 }}>
          O chip-alvo não respondeu ao ISP em {leitura?.maxTentativas ?? 3} tentativas.
        </p>
      </Dialogo>

      <Dialogo
        aberto={dialogo === 'semAltaTensao'}
        titulo="Sem restauro das configurações"
        aoFechar={() => setDialogo('nenhum')}
        acoes={
          <>
            <button
              type="button"
              className="botao botao--principal"
              onClick={() => {
                setDialogo('nenhum');
                void bancada.reiniciarCiclo();
              }}
            >
              Recomeçar com outra peça
            </button>
            <button type="button" className="botao" onClick={() => setDialogo('nenhum')}>
              Continuar a navegar
            </button>
          </>
        }
      >
        <p style={{ margin: 0 }}>
          Sem a programação de alta tensão não é possível restaurar as configurações deste
          microcontrolador.
        </p>
        <p className="subtil" style={{ margin: 0 }}>
          Recomeçar devolve as tentativas de leitura e volta à verificação de dispositivos.
        </p>
      </Dialogo>
    </>
  );
}

/** O chip não respondeu ao ISP: o que fazer, e quantas tentativas restam. */
function PainelFalha({ leitura, bancada }: { leitura: Leitura; bancada: Bancada }) {
  return (
    <div className="pilha">
      <p className="aviso aviso--erro" style={{ margin: 0 }}>
        {leitura.instrucao ??
          'Verifique se o microcontrolador está corretamente inserido no ZIF socket.'}
      </p>

      {leitura.tentativas && (
        <p className="subtil" style={{ margin: 0 }}>
          {leitura.tentativas}
        </p>
      )}

      <div className="linha">
        <button
          type="button"
          className="botao botao--principal"
          onClick={() => void bancada.lerConfiguracoes()}
          disabled={leitura.esgotado || bancada.ocupacao !== 'nada'}
        >
          Tentar detetar novamente
        </button>
      </div>

      {leitura.saidaAvrdude && <SaidaAvrdude texto={leitura.saidaAvrdude} />}
    </div>
  );
}

/** O que a leitura identificou: o chip, os fuses, e o que eles significam. */
function DadosLeitura({ leitura }: { leitura: Leitura }) {
  const { mcu, fuses, descodificado, mapaFlash } = leitura;

  return (
    <div className="pilha">
      {mcu && (
        <div className="subcartao">
          <div className="dados">
            <Dado rotulo="Microcontrolador" valor={mcu.nome} />
            <Dado rotulo="Assinatura" valor={leitura.assinatura ?? '—'} mono />
            <Dado rotulo="Flash" valor={mcu.flash} />
            <Dado rotulo="EEPROM" valor={mcu.eeprom} />
            <Dado rotulo="SRAM" valor={mcu.sram} />
          </div>
        </div>
      )}

      {fuses && (
        <div className="fuses">
          <Fuse nome="lfuse" valor={fuses.low} significado={descodificado?.descricaoLow} />
          <Fuse nome="hfuse" valor={fuses.high} significado={descodificado?.descricaoHigh} />
          <Fuse nome="efuse" valor={fuses.extended} significado={descodificado?.descricaoExtended} />
          <Fuse nome="lock" valor={fuses.lock} significado={descodificado?.descricaoLock} />
        </div>
      )}

      {descodificado && (
        <div className="propriedades">
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
        </div>
      )}

      {mapaFlash && (
        <div className="subcartao">
          <MapaFlash mapa={mapaFlash} />
        </div>
      )}

      {leitura.saidaAvrdude && <SaidaAvrdude texto={leitura.saidaAvrdude} />}

      <p className="subtil" style={{ margin: 0 }}>
        Leitura apenas — esta aplicação nunca grava fuses.
      </p>
    </div>
  );
}

/** A cópia que ficou guardada e por onde se descarrega. */
function FichaCopia({ copia, bancada }: { copia: Copia; bancada: Bancada }) {
  return (
    <div className="subcartao">
      <div className="dado__rotulo">Cópia de segurança</div>
      <p style={{ margin: '4px 0 0', fontSize: 12 }}>{copia.mensagem}</p>

      {copia.ficheiros.length > 0 && (
        <ul className="ficheiros">
          {copia.ficheiros.map((f) => (
            <li key={f.nome}>
              <a href={bancada.urlFicheiro(f.url)} download>
                {f.nome}
              </a>{' '}
              <span className="subtil">{Math.max(1, Math.round(f.bytes / 1024))} KB</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

function Dado({ rotulo, valor, mono }: { rotulo: string; valor: string; mono?: boolean }) {
  return (
    <div>
      <div className="dado__rotulo">{rotulo}</div>
      <div className={`dado__valor${mono ? ' mono' : ''}`}>{valor}</div>
    </div>
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
    <div className="fuse">
      <div className="fuse__nome">{nome}</div>
      <div className="fuse__valor">{valor ?? 'n/d'}</div>
      {significado && <div className="fuse__significado">{significado}</div>}
    </div>
  );
}

function Propriedade({ nome, valor }: { nome: string; valor: string }) {
  return (
    <div className="propriedade">
      <span className="propriedade__nome">{nome}</span>
      <span className="propriedade__valor">{valor}</span>
    </div>
  );
}

/** A saída crua do avrdude. Fechada por omissão: à bancada é ruído, no diagnóstico é a pista. */
function SaidaAvrdude({ texto }: { texto: string }) {
  return (
    <details>
      <summary>Saída do avrdude</summary>
      <pre className="saida" style={{ marginTop: 8 }}>
        {texto}
      </pre>
    </details>
  );
}
