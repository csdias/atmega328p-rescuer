import type { ReactNode } from 'react';
import { Modal, ScrollView, Text, View } from 'react-native';
import { estilos } from '../estilos';

/**
 * Diálogo modal nativo. `aoFechar` corre também no botão físico de voltar do Android —
 * quem volta atrás tomou a mesma decisão que quem carrega em Cancelar, e um destes
 * diálogos fechado sem resposta deixaria a bancada à espera de uma escolha que não chega.
 */
export function Dialogo({
  aberto,
  titulo,
  aoFechar,
  acoes,
  children,
}: {
  aberto: boolean;
  titulo: string;
  aoFechar: () => void;
  acoes: ReactNode;
  children: ReactNode;
}) {
  return (
    <Modal visible={aberto} transparent animationType="fade" onRequestClose={aoFechar}>
      <View style={estilos.fundoDialogo}>
        <View style={estilos.dialogo}>
          <Text style={estilos.dialogoTitulo}>{titulo}</Text>

          <ScrollView contentContainerStyle={estilos.pilha}>{children}</ScrollView>

          <View style={estilos.linha}>{acoes}</View>
        </View>
      </View>
    </Modal>
  );
}
