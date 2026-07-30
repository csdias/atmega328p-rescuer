import { Text, View } from 'react-native';
import type { Bancada } from '@atmegapesta/partilhado';
import { Aviso, Botao, Cartao } from '../componentes/Basicos';
import { cores, estilos } from '../estilos';

/**
 * O passo entre a bancada verificada e a leitura: o chip tem de estar no ZIF antes de o
 * ISP ir procurá-lo, senão a primeira leitura falha sempre.
 */
export function EcranInserirChip({ bancada }: { bancada: Bancada }) {
  return (
    <Cartao titulo="Inserir microcontrolador">
      <Text style={estilos.texto}>Encaixe o ATmega328P no ZIF socket antes de continuar.</Text>

      <View style={[estilos.subcartao, { alignItems: 'center', gap: 4 }]}>
        <Text style={estilos.textoPequeno}>ZIF vazio</Text>
        <Text style={{ fontSize: 18, color: cores.menuAccento }}>▼</Text>
        <Text style={[estilos.texto, { color: cores.sucesso, fontWeight: '600' }]}>
          Chip inserido ✓
        </Text>
      </View>

      <Aviso>
        Certifique-se que a alavanca do ZIF está levantada, insira o chip com o pino 1 no
        canto marcado e baixe a alavanca para fixar.
      </Aviso>

      <View style={estilos.linha}>
        <Botao titulo="Voltar" aoPremir={bancada.voltarAVerificacao} />
        <Botao
          titulo="Chip inserido — Continuar"
          principal
          aoPremir={() => void bancada.confirmarChipInserido()}
        />
      </View>
    </Cartao>
  );
}
