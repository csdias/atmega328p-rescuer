import { Text, View } from 'react-native';
import type { Bancada } from '@atmegapesta/partilhado';
import { Aviso, Botao, Cartao, LinhaIndicador } from '../componentes/Basicos';
import { coresLed, estilos } from '../estilos';

/** O arranque: até a bancada estar completa e o equipamento identificado não se avança. */
export function EcranVerificacao({
  bancada,
  aoTrocarBancada,
}: {
  bancada: Bancada;
  aoTrocarBancada: () => void;
}) {
  const { deteccao, ocupacao, progresso, erro, config } = bancada;
  const aVerificar = ocupacao === 'detectar';
  const esgotado = deteccao?.esgotado === true;
  const restantes = deteccao ? deteccao.maxTentativas - deteccao.tentativa : null;

  return (
    <Cartao titulo="Verificação de dispositivos">
      <Text style={estilos.texto}>
        A bancada precisa do conversor CH340 e do programador USBAsp ligados ao computador.
      </Text>

      <View style={estilos.subcartao}>
        <LinhaIndicador
          nome="CH340"
          indicador={deteccao?.ch340 ?? { estado: 'inactivo', detalhe: 'Ainda não verificado' }}
        />
        <LinhaIndicador
          nome="USBAsp"
          indicador={deteccao?.usbAsp ?? { estado: 'inactivo', detalhe: 'Ainda não verificado' }}
        />
        <LinhaIndicador
          nome="Assinatura"
          indicador={
            deteccao?.assinatura ?? {
              estado: 'inactivo',
              detalhe:
                config?.verificarAssinatura === false
                  ? 'Verificação de assinatura desligada na configuração'
                  : 'A aguardar detecção do CH340...',
            }
          }
        />
      </View>

      {aVerificar && (
        <Text style={estilos.texto} accessibilityLiveRegion="polite">
          {progresso?.texto ?? 'A verificar dispositivos...'}
        </Text>
      )}

      {!aVerificar && deteccao && (
        <Text
          style={[estilos.texto, { color: coresLed[deteccao.severidade] }]}
          accessibilityLiveRegion="polite"
        >
          {deteccao.mensagem}
        </Text>
      )}

      {erro && (
        <Aviso erro>
          {erro}
          {'\n'}Confirma o endereço da bancada e se estás na mesma rede.
        </Aviso>
      )}

      <View style={estilos.linha}>
        {esgotado ? (
          <Botao
            titulo="Recomeçar a verificação"
            principal
            aoPremir={() => void bancada.reiniciarCiclo()}
          />
        ) : (
          <>
            <Botao
              titulo={
                aVerificar
                  ? 'A verificar...'
                  : deteccao
                    ? `Tentar novamente${restantes !== null ? ` (${restantes})` : ''}`
                    : 'Verificar dispositivos'
              }
              principal
              desactivado={aVerificar}
              aoPremir={() => void bancada.detectar()}
            />
            {deteccao?.podeAvancar && (
              <Botao titulo="Continuar" aoPremir={bancada.irParaInserirChip} />
            )}
          </>
        )}
        <Botao titulo="Trocar de bancada" aoPremir={aoTrocarBancada} />
      </View>

      {deteccao && !esgotado && !deteccao.podeAvancar && (
        <Text style={estilos.textoPequeno}>
          Tentativa {deteccao.tentativa} de {deteccao.maxTentativas}
        </Text>
      )}
    </Cartao>
  );
}
