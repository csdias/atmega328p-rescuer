import type { ReactNode } from 'react';
import { Pressable, Text, View } from 'react-native';
import type { EstadoLed, Indicador } from '@atmegapesta/partilhado';
import { cores, coresLed, estilos } from '../estilos';

/** O ponto colorido dos LEDs do WPF. */
export function Led({ estado }: { estado: EstadoLed }) {
  return <View style={[estilos.led, { backgroundColor: coresLed[estado] }]} />;
}

/**
 * Uma linha de indicador: LED, nome e detalhe.
 *
 * O estado vai também em palavra — no telemóvel, ao sol, a cor de um ponto de 11px não
 * pode ser a única forma de saber se o CH340 apareceu.
 */
export function LinhaIndicador({ nome, indicador }: { nome: string; indicador: Indicador }) {
  return (
    <View style={estilos.indicador}>
      <View style={{ paddingTop: 4 }}>
        <Led estado={indicador.estado} />
      </View>
      <Text style={estilos.indicadorNome}>{nome}</Text>
      <View style={{ flex: 1 }}>
        <Text style={estilos.textoPequeno}>{palavraEstado(indicador.estado)}</Text>
        <Text style={estilos.texto}>{indicador.detalhe}</Text>
      </View>
    </View>
  );
}

function palavraEstado(estado: EstadoLed): string {
  switch (estado) {
    case 'ok':
      return 'OK';
    case 'aviso':
      return 'Aviso';
    case 'erro':
      return 'Falha';
    case 'inactivo':
      return 'Inactivo';
  }
}

export function Botao({
  titulo,
  aoPremir,
  principal,
  desactivado,
}: {
  titulo: string;
  aoPremir: () => void;
  principal?: boolean;
  desactivado?: boolean;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ disabled: desactivado === true }}
      disabled={desactivado === true}
      onPress={aoPremir}
      style={({ pressed }) => [
        estilos.botao,
        principal === true && estilos.botaoPrincipal,
        desactivado === true && estilos.botaoDesactivado,
        pressed && { opacity: 0.75 },
        { flexGrow: 1, flexBasis: '45%' },
      ]}
    >
      <Text
        style={[
          estilos.botaoTexto,
          principal === true && estilos.botaoTextoPrincipal,
          desactivado === true && estilos.botaoTextoDesactivado,
        ]}
      >
        {titulo}
      </Text>
    </Pressable>
  );
}

/** Um passo da bancada. O LED e o estado no cabeçalho são o resumo denso. */
export function Cartao({
  titulo,
  estado,
  severidade,
  children,
}: {
  titulo: string;
  estado?: string | undefined;
  severidade?: EstadoLed | undefined;
  children?: ReactNode | undefined;
}) {
  return (
    <View style={estilos.cartao}>
      <View style={estilos.cartaoCabeca}>
        {severidade && <Led estado={severidade} />}
        <Text style={estilos.cartaoTitulo}>{titulo}</Text>
      </View>

      {estado && (
        <Text style={[estilos.texto, severidade ? { color: coresLed[severidade] } : null]}>
          {estado}
        </Text>
      )}

      {children}
    </View>
  );
}

export function Aviso({ children, erro }: { children: ReactNode; erro?: boolean }) {
  return (
    <View style={[estilos.aviso, erro === true && estilos.avisoErro]}>
      <Text style={estilos.texto}>{children}</Text>
    </View>
  );
}

export function Dado({ rotulo, valor, mono }: { rotulo: string; valor: string; mono?: boolean }) {
  return (
    <View style={{ flexGrow: 1, flexBasis: '30%' }}>
      <Text style={estilos.rotulo}>{rotulo}</Text>
      <Text style={[estilos.valor, mono === true && estilos.mono]}>{valor}</Text>
    </View>
  );
}

export function Propriedade({ nome, valor }: { nome: string; valor: string }) {
  return (
    <View style={estilos.propriedade}>
      <Text style={[estilos.texto, { flexShrink: 1 }]}>{nome}</Text>
      <Text style={[estilos.texto, { fontWeight: '600', color: cores.textoTitulo }]}>{valor}</Text>
    </View>
  );
}
