import { StyleSheet } from 'react-native';
import { cores, coresLed, raios } from '@atmegapesta/partilhado';

/**
 * Os estilos nativos, a partir dos mesmos tokens que o front end web usa. É por isto que
 * `tokens.ts` está em valores e não em CSS: aqui alimenta um StyleSheet sem conversão.
 */
export { cores, coresLed };

export const estilos = StyleSheet.create({
  ecra: {
    flex: 1,
    backgroundColor: cores.fundoJanela,
  },

  // ── Cabeçalho ────────────────────────────────────────────────────────────
  cabecalho: {
    backgroundColor: cores.fundoHeader,
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: cores.separador,
  },
  cabecalhoTitulo: {
    fontSize: 15,
    fontWeight: '600',
    color: cores.textoTitulo,
  },
  cabecalhoSub: {
    fontSize: 11,
    color: cores.textoSubtil,
    marginTop: 2,
  },

  conteudo: {
    padding: 14,
    paddingBottom: 32,
    gap: 12,
  },

  // ── Cartões ──────────────────────────────────────────────────────────────
  cartao: {
    backgroundColor: cores.fundoCard,
    borderRadius: raios.grande,
    padding: 16,
    gap: 10,
  },
  cartaoCabeca: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  cartaoTitulo: {
    fontSize: 14,
    fontWeight: '600',
    color: cores.textoTitulo,
    flexShrink: 1,
  },
  subcartao: {
    backgroundColor: cores.fundoSubCard,
    borderRadius: raios.medio,
    padding: 12,
    gap: 8,
  },

  // ── Texto ────────────────────────────────────────────────────────────────
  texto: {
    fontSize: 13,
    color: cores.textoNormal,
  },
  textoPequeno: {
    fontSize: 11,
    color: cores.textoSubtil,
  },
  rotulo: {
    fontSize: 10,
    letterSpacing: 0.6,
    color: cores.textoSubtil,
    textTransform: 'uppercase',
  },
  valor: {
    fontSize: 14,
    fontWeight: '600',
    color: cores.textoTitulo,
  },
  mono: {
    fontFamily: 'monospace',
  },

  // ── LED e indicadores ────────────────────────────────────────────────────
  led: {
    width: 11,
    height: 11,
    borderRadius: 6,
  },
  indicador: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: 10,
    paddingVertical: 7,
  },
  indicadorNome: {
    fontSize: 13,
    fontWeight: '600',
    color: cores.textoTitulo,
    width: 84,
  },

  // ── Botões ───────────────────────────────────────────────────────────────
  botao: {
    backgroundColor: cores.btnClaro,
    borderRadius: raios.pequeno,
    paddingVertical: 12,
    paddingHorizontal: 18,
    alignItems: 'center',
  },
  botaoPrincipal: {
    backgroundColor: cores.btnEscuro,
  },
  botaoDesactivado: {
    backgroundColor: cores.btnDesactivado,
  },
  botaoTexto: {
    fontSize: 13,
    fontWeight: '600',
    color: cores.btnClaroTexto,
  },
  botaoTextoPrincipal: {
    color: cores.btnEscuroTexto,
  },
  botaoTextoDesactivado: {
    color: cores.btnDesactivadoTexto,
  },

  // ── Avisos ───────────────────────────────────────────────────────────────
  aviso: {
    backgroundColor: cores.fundoSubCard,
    borderLeftWidth: 3,
    borderLeftColor: cores.aviso,
    borderRadius: raios.pequeno,
    padding: 12,
  },
  avisoErro: {
    borderLeftColor: cores.erro,
  },

  // ── Fuses e dados ────────────────────────────────────────────────────────
  fuse: {
    backgroundColor: cores.fundoInput,
    borderRadius: raios.pequeno,
    padding: 10,
    flexGrow: 1,
    flexBasis: '47%',
  },
  fuseValor: {
    fontSize: 15,
    fontWeight: '600',
    color: cores.textoTitulo,
    fontFamily: 'monospace',
  },
  grelha: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 8,
  },
  propriedade: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    gap: 10,
    paddingVertical: 6,
    borderBottomWidth: 1,
    borderBottomColor: cores.separador,
  },

  // ── Mapa da Flash ────────────────────────────────────────────────────────
  barra: {
    flexDirection: 'row',
    height: 22,
    borderRadius: raios.pequeno,
    overflow: 'hidden',
    backgroundColor: cores.progressoFundo,
  },
  progresso: {
    height: 8,
    borderRadius: raios.pilula,
    overflow: 'hidden',
    backgroundColor: cores.progressoFundo,
  },
  progressoBarra: {
    height: '100%',
    backgroundColor: cores.progressoBarra,
  },

  // ── Pinos e tags ─────────────────────────────────────────────────────────
  pino: {
    backgroundColor: cores.pino,
    borderRadius: raios.pequeno,
    paddingVertical: 5,
    paddingHorizontal: 7,
    minWidth: 36,
    alignItems: 'center',
  },
  pinoTexto: {
    fontSize: 10,
    fontWeight: '600',
    color: cores.pinoTexto,
    fontFamily: 'monospace',
  },
  tag: {
    backgroundColor: cores.tag,
    borderRadius: raios.pequeno,
    paddingVertical: 3,
    paddingHorizontal: 8,
  },
  tagTexto: {
    fontSize: 10,
    fontWeight: '600',
    color: cores.tagTexto,
  },
  badge: {
    backgroundColor: cores.badge,
    borderRadius: raios.pequeno,
    paddingVertical: 2,
    paddingHorizontal: 8,
  },
  badgeTexto: {
    fontSize: 10,
    fontWeight: '600',
    color: cores.badgeTexto,
  },

  // ── Lista de testes ──────────────────────────────────────────────────────
  linhaTeste: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: 10,
    paddingVertical: 8,
    borderBottomWidth: 1,
    borderBottomColor: cores.separador,
  },

  // ── Campo de texto (endereço da bancada) ─────────────────────────────────
  campo: {
    backgroundColor: cores.fundoInput,
    borderRadius: raios.pequeno,
    paddingHorizontal: 12,
    paddingVertical: 10,
    fontSize: 15,
    color: cores.textoTitulo,
    fontFamily: 'monospace',
  },

  // ── Diálogo ──────────────────────────────────────────────────────────────
  fundoDialogo: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.35)',
    justifyContent: 'center',
    padding: 20,
  },
  dialogo: {
    backgroundColor: cores.fundoCard,
    borderRadius: raios.enorme,
    padding: 20,
    gap: 10,
    maxHeight: '85%',
  },
  dialogoTitulo: {
    fontSize: 16,
    fontWeight: '600',
    color: cores.textoTitulo,
  },

  // ── Utilidades ───────────────────────────────────────────────────────────
  linha: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    flexWrap: 'wrap',
  },
  pilha: {
    gap: 10,
  },
  saida: {
    backgroundColor: cores.fundoInput,
    borderRadius: raios.pequeno,
    padding: 10,
  },
  saidaTexto: {
    fontSize: 10,
    fontFamily: 'monospace',
    color: cores.textoNormal,
  },
});
