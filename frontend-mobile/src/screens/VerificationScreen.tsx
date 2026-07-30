import { Text, View } from 'react-native';
import type { Bench } from '@atmegapesta/shared';
import { Notice, Botao, Card, IndicatorRow } from '../components/Basics';
import { ledColours, estilos } from '../styles';

/** The startup: until the bench is complete and the rig identified, nothing moves on. */
export function VerificationScreen({
  bench,
  onSwitchBench,
}: {
  bench: Bench;
  onSwitchBench: () => void;
}) {
  const { detection, busy, progress, error, config } = bench;
  const aVerificar = busy === 'detect';
  const exhausted = detection?.exhausted === true;
  const restantes = detection ? detection.maxAttempts - detection.attempt : null;

  return (
    <Card title="Verificação de dispositivos">
      <Text style={estilos.text}>
        A bench precisa do conversor CH340 e do programador USBAsp ligados ao computador.
      </Text>

      <View style={estilos.subcard}>
        <IndicatorRow
          name="CH340"
          indicator={detection?.ch340 ?? { state: 'idle', detail: 'Ainda não verificado' }}
        />
        <IndicatorRow
          name="USBAsp"
          indicator={detection?.usbAsp ?? { state: 'idle', detail: 'Ainda não verificado' }}
        />
        <IndicatorRow
          name="Assinatura"
          indicator={
            detection?.signature ?? {
              state: 'idle',
              detail:
                config?.verifySignature === false
                  ? 'Verificação de assinatura desligada na configuração'
                  : 'A aguardar detecção do CH340...',
            }
          }
        />
      </View>

      {aVerificar && (
        <Text style={estilos.text} accessibilityLiveRegion="polite">
          {progress?.text ?? 'A verificar dispositivos...'}
        </Text>
      )}

      {!aVerificar && detection && (
        <Text
          style={[estilos.text, { color: ledColours[detection.severity] }]}
          accessibilityLiveRegion="polite"
        >
          {detection.message}
        </Text>
      )}

      {error && (
        <Notice error>
          {error}
          {'\n'}Confirma o endereço da bench e se estás na mesma rede.
        </Notice>
      )}

      <View style={estilos.row}>
        {exhausted ? (
          <Botao
            title="Recomeçar a verificação"
            principal
            onPress={() => void bench.resetCycle()}
          />
        ) : (
          <>
            <Botao
              title={
                aVerificar
                  ? 'A verificar...'
                  : detection
                    ? `Tentar novamente${restantes !== null ? ` (${restantes})` : ''}`
                    : 'Verificar dispositivos'
              }
              principal
              desactivado={aVerificar}
              onPress={() => void bench.detect()}
            />
            {detection?.canProceed && (
              <Botao title="Continuar" onPress={bench.goToInsertChip} />
            )}
          </>
        )}
        <Botao title="Trocar de bancada" onPress={onSwitchBench} />
      </View>

      {detection && !exhausted && !detection.canProceed && (
        <Text style={estilos.smallText}>
          Tentativa {detection.attempt} de {detection.maxAttempts}
        </Text>
      )}
    </Card>
  );
}
