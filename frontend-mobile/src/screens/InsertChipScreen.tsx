import { Text, View } from 'react-native';
import type { Bench } from '@atmegapesta/shared';
import { Notice, Botao, Card } from '../components/Basics';
import { colours, estilos } from '../styles';

/**
 * The step between the verified bench and the read: the chip has to be in the ZIF before
 * ISP goes looking for it, or the first read always fails.
 */
export function InsertChipScreen({ bench }: { bench: Bench }) {
  return (
    <Card title="Inserir microcontrolador">
      <Text style={estilos.text}>Encaixe o ATmega328P no ZIF socket antes de continuar.</Text>

      <View style={[estilos.subcard, { alignItems: 'center', gap: 4 }]}>
        <Text style={estilos.smallText}>ZIF vazio</Text>
        <Text style={{ fontSize: 18, color: colours.menuAccent }}>▼</Text>
        <Text style={[estilos.text, { color: colours.success, fontWeight: '600' }]}>
          Chip inserido ✓
        </Text>
      </View>

      <Notice>
        Certifique-se que a alavanca do ZIF está levantada, insira o chip com o pin 1 no
        canto marcado e baixe a alavanca para fixar.
      </Notice>

      <View style={estilos.row}>
        <Botao title="Voltar" onPress={bench.backToVerification} />
        <Botao
          title="Chip inserido — Continuar"
          principal
          onPress={() => void bench.confirmChipInserted()}
        />
      </View>
    </Card>
  );
}
