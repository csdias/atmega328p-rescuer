import {
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType,
  type HubConnection,
} from '@microsoft/signalr';
import type {
  Catalogo,
  Config,
  Copia,
  Deteccao,
  EscolhaTransferencia,
  Integridade,
  Leitura,
  Progresso,
} from './contratos';

/**
 * O cliente da API da bancada. Não toca no DOM — o React Native usa-o tal como está,
 * passando a `baseUrl` do PC da bancada em vez de a deixar vazia.
 *
 * A `baseUrl` vazia é o caso do browser: em desenvolvimento o Vite encaminha /api para o
 * Kestrel, em produção a API serve o próprio React. Em qualquer dos dois a origem é a
 * mesma e não há endereço para configurar.
 */
export class ClienteBancada {
  private readonly baseUrl: string;
  private hub: HubConnection | null = null;

  constructor(baseUrl = '') {
    // Sem barra final: os caminhos abaixo já começam por uma.
    this.baseUrl = baseUrl.replace(/\/$/, '');
  }

  config = () => this.pedir<Config>('GET', '/api/bancada/config');

  /**
   * Varre o USB e pede a assinatura ao equipamento. Cada chamada é uma tentativa,
   * enquanto a bancada não estiver completa.
   */
  detectar = () => this.pedir<Deteccao>('POST', '/api/bancada/detectar');

  /** Devolve o ciclo ao início. É o que se faz ao trocar de peça no ZIF. */
  reiniciar = () => this.pedir<void>('POST', '/api/bancada/reiniciar');

  /** Lê a identificação e os fuses do chip-alvo. Estritamente leitura. */
  lerConfiguracoes = () => this.pedir<Leitura>('POST', '/api/alvo/ler-configuracoes');

  guardarCopia = () => this.pedir<Copia>('POST', '/api/alvo/copia');

  catalogo = () => this.pedir<Catalogo>('GET', '/api/integridade/catalogo');

  executarIntegridade = (escolha: EscolhaTransferencia) =>
    this.pedir<Integridade>('POST', '/api/integridade/executar', { escolha });

  /** URL de descarga de um ficheiro da cópia, absoluto quando há `baseUrl`. */
  urlFicheiro = (url: string) => `${this.baseUrl}${url}`;

  /**
   * Liga-se ao canal de progresso e chama `aoProgresso` a cada linha. Devolve a função
   * que desliga.
   *
   * Um acesso ISP demora segundos; sem isto o ecrã fica parado à espera da resposta HTTP.
   * Se a ligação não subir, o front end continua a funcionar — perde-se o progresso ao
   * vivo, não a operação.
   */
  async ligarProgresso(aoProgresso: (p: Progresso) => void): Promise<() => void> {
    const hub = new HubConnectionBuilder()
      // WebSockets explícito: é o único transporte que funciona igual no browser e no
      // React Native. Deixado ao critério do SignalR, ele pode cair para Server-Sent
      // Events ou long polling, que no nativo dependem de APIs de browser que não existem.
      .withUrl(`${this.baseUrl}/hub/bancada`, { transport: HttpTransportType.WebSockets })
      .withAutomaticReconnect()
      .build();

    hub.on('progresso', aoProgresso);
    this.hub = hub;

    try {
      await hub.start();
    } catch {
      // Sem canal de progresso a bancada continua a responder por HTTP.
    }

    return () => {
      hub.off('progresso', aoProgresso);
      if (hub.state !== HubConnectionState.Disconnected) void hub.stop();
      if (this.hub === hub) this.hub = null;
    };
  }

  private async pedir<T>(metodo: 'GET' | 'POST', caminho: string, corpo?: unknown): Promise<T> {
    const resposta = await fetch(`${this.baseUrl}${caminho}`, {
      method: metodo,
      headers: corpo === undefined ? {} : { 'Content-Type': 'application/json' },
      ...(corpo === undefined ? {} : { body: JSON.stringify(corpo) }),
    });

    if (!resposta.ok)
      throw new ErroApi(
        `A bancada respondeu ${resposta.status} a ${metodo} ${caminho}.`,
        resposta.status,
      );

    // 204 das operações que não devolvem nada (reiniciar).
    if (resposta.status === 204) return undefined as T;

    return (await resposta.json()) as T;
  }
}

/** Falha da API, com o código para quem quiser distinguir "não respondeu" de "recusou". */
export class ErroApi extends Error {
  constructor(
    mensagem: string,
    readonly status: number,
  ) {
    super(mensagem);
    this.name = 'ErroApi';
  }
}
