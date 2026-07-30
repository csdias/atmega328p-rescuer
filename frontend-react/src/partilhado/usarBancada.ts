import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { ClienteBancada } from './clienteApi';
import type {
  Catalogo,
  Config,
  Deteccao,
  EscolhaTransferencia,
  Integridade,
  Leitura,
  Progresso,
} from './contratos';

/**
 * As fases do fluxo, na ordem em que acontecem. São as mesmas do WPF: até a bancada estar
 * verificada não se pede para inserir o chip, e até o chip estar no ZIF não se lê nada.
 */
export type Fase = 'verificacao' | 'inserirChip' | 'trabalho';

/** Que operação está a correr. A bancada é uma só — nunca há duas ao mesmo tempo. */
export type Ocupacao = 'nada' | 'detectar' | 'ler' | 'integridade';

export interface Bancada {
  config: Config | null;
  catalogo: Catalogo | null;
  fase: Fase;
  deteccao: Deteccao | null;
  leitura: Leitura | null;
  integridade: Integridade | null;
  progresso: Progresso | null;
  ocupacao: Ocupacao;
  erro: string | null;

  /** Leitura completa: é ela que destranca a verificação de integridade. */
  podeVerificarIntegridade: boolean;
  /** Tentativas de ISP gastas — há que propor a programação de alta tensão. */
  escalarAltaTensao: boolean;

  detectar: () => Promise<void>;
  irParaInserirChip: () => void;
  voltarAVerificacao: () => void;
  confirmarChipInserido: () => Promise<void>;
  lerConfiguracoes: () => Promise<void>;
  executarIntegridade: (escolha: EscolhaTransferencia) => Promise<void>;
  reiniciarCiclo: () => Promise<void>;
  urlFicheiro: (url: string) => string;
}

/**
 * Todo o fluxo da bancada, sem uma linha de DOM: estado, transições e chamadas à API.
 * O React Native monta os seus próprios ecrãs por cima deste hook, passando a `baseUrl`
 * do PC da bancada.
 */
export function usarBancada(baseUrl = ''): Bancada {
  const cliente = useMemo(() => new ClienteBancada(baseUrl), [baseUrl]);

  const [config, setConfig] = useState<Config | null>(null);
  const [catalogo, setCatalogo] = useState<Catalogo | null>(null);
  const [fase, setFase] = useState<Fase>('verificacao');
  const [deteccao, setDeteccao] = useState<Deteccao | null>(null);
  const [leitura, setLeitura] = useState<Leitura | null>(null);
  const [integridade, setIntegridade] = useState<Integridade | null>(null);
  const [progresso, setProgresso] = useState<Progresso | null>(null);
  const [ocupacao, setOcupacao] = useState<Ocupacao>('nada');
  const [erro, setErro] = useState<string | null>(null);

  // A bancada é um recurso único e a API serializa os acessos. Recusar aqui o segundo
  // pedido evita deixar quem carregou duas vezes à espera numa fila invisível.
  const emCurso = useRef(false);

  useEffect(() => {
    let vivo = true;

    void (async () => {
      try {
        const [c, cat] = await Promise.all([cliente.config(), cliente.catalogo()]);
        if (!vivo) return;
        setConfig(c);
        setCatalogo(cat);
      } catch (e) {
        if (vivo) setErro(mensagemDe(e));
      }
    })();

    return () => {
      vivo = false;
    };
  }, [cliente]);

  useEffect(() => {
    let desligar: (() => void) | null = null;
    let vivo = true;

    void cliente.ligarProgresso(setProgresso).then((f) => {
      if (vivo) desligar = f;
      else f();
    });

    return () => {
      vivo = false;
      desligar?.();
    };
  }, [cliente]);

  /**
   * Corre uma operação da bancada, com a ocupação e os erros tratados num sítio só.
   * A linha de progresso é limpa no fim: o que fica é o resultado, não o último passo.
   */
  const operar = useCallback(
    async (qual: Exclude<Ocupacao, 'nada'>, acao: () => Promise<void>) => {
      if (emCurso.current) return;

      emCurso.current = true;
      setOcupacao(qual);
      setErro(null);

      try {
        await acao();
      } catch (e) {
        setErro(mensagemDe(e));
      } finally {
        emCurso.current = false;
        setOcupacao('nada');
        setProgresso(null);
      }
    },
    [],
  );

  const detectar = useCallback(
    () => operar('detectar', async () => setDeteccao(await cliente.detectar())),
    [cliente, operar],
  );

  const lerConfiguracoes = useCallback(
    () =>
      operar('ler', async () => {
        // Uma leitura nova invalida o que a anterior destrancou: quem trocou a peça no
        // ZIF não deve continuar a ver resultados de outro chip.
        setIntegridade(null);
        setLeitura(await cliente.lerConfiguracoes());
      }),
    [cliente, operar],
  );

  const executarIntegridade = useCallback(
    (escolha: EscolhaTransferencia) =>
      operar('integridade', async () => setIntegridade(await cliente.executarIntegridade(escolha))),
    [cliente, operar],
  );

  const irParaInserirChip = useCallback(() => setFase('inserirChip'), []);
  const voltarAVerificacao = useCallback(() => setFase('verificacao'), []);

  const confirmarChipInserido = useCallback(async () => {
    setFase('trabalho');
    await lerConfiguracoes();
  }, [lerConfiguracoes]);

  const reiniciarCiclo = useCallback(async () => {
    await cliente.reiniciar();
    setDeteccao(null);
    setLeitura(null);
    setIntegridade(null);
    setErro(null);
    setFase('verificacao');
  }, [cliente]);

  return {
    config,
    catalogo,
    fase,
    deteccao,
    leitura,
    integridade,
    progresso,
    ocupacao,
    erro,
    podeVerificarIntegridade: leitura?.configuracoesLidas === true,
    escalarAltaTensao: leitura?.esgotado === true,
    detectar,
    irParaInserirChip,
    voltarAVerificacao,
    confirmarChipInserido,
    lerConfiguracoes,
    executarIntegridade,
    reiniciarCiclo,
    urlFicheiro: cliente.urlFicheiro,
  };
}

function mensagemDe(e: unknown): string {
  if (e instanceof Error) return e.message;
  return 'A bancada não respondeu.';
}
