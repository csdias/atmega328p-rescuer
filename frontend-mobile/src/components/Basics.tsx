import type { ReactNode } from 'react';
import { Pressable, Text, View } from 'react-native';
import type { LedState, Indicator } from '@atmegapesta/shared';
import { colours, ledColours, estilos } from '../styles';

/** O ponto colorido dos LEDs do WPF. */
export function Led({ state }: { state: LedState }) {
  return <View style={[estilos.led, { backgroundColor: ledColours[state] }]} />;
}

/**
 * One indicator row: LED, name and detail.
 *
 * The state also travels as a word — on a phone, in the sun, the colour of an 11px dot
 * cannot be the only way to know whether the CH340 showed up.
 */
export function IndicatorRow({ name, indicator }: { name: string; indicator: Indicator }) {
  return (
    <View style={estilos.indicator}>
      <View style={{ paddingTop: 4 }}>
        <Led state={indicator.state} />
      </View>
      <Text style={estilos.indicatorName}>{name}</Text>
      <View style={{ flex: 1 }}>
        <Text style={estilos.smallText}>{palavraEstado(indicator.state)}</Text>
        <Text style={estilos.text}>{indicator.detail}</Text>
      </View>
    </View>
  );
}

function palavraEstado(state: LedState): string {
  switch (state) {
    case 'ok':
      return 'OK';
    case 'warning':
      return 'Aviso';
    case 'error':
      return 'Falha';
    case 'idle':
      return 'Inactivo';
  }
}

export function Botao({
  title,
  onPress,
  principal,
  desactivado,
}: {
  title: string;
  onPress: () => void;
  principal?: boolean;
  desactivado?: boolean;
}) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ disabled: desactivado === true }}
      disabled={desactivado === true}
      onPress={onPress}
      style={({ pressed }) => [
        estilos.button,
        principal === true && estilos.buttonPrimary,
        desactivado === true && estilos.buttonDisabled,
        pressed && { opacity: 0.75 },
        { flexGrow: 1, flexBasis: '45%' },
      ]}
    >
      <Text
        style={[
          estilos.buttonText,
          principal === true && estilos.buttonTextPrimary,
          desactivado === true && estilos.buttonTextDisabled,
        ]}
      >
        {title}
      </Text>
    </Pressable>
  );
}

/** One bench step. The LED and the state in the header are the dense summary. */
export function Card({
  title,
  state,
  severity,
  children,
}: {
  title: string;
  state?: string | undefined;
  severity?: LedState | undefined;
  children?: ReactNode | undefined;
}) {
  return (
    <View style={estilos.card}>
      <View style={estilos.cardHead}>
        {severity && <Led state={severity} />}
        <Text style={estilos.cardTitle}>{title}</Text>
      </View>

      {state && (
        <Text style={[estilos.text, severity ? { color: ledColours[severity] } : null]}>
          {state}
        </Text>
      )}

      {children}
    </View>
  );
}

export function Notice({ children, error }: { children: ReactNode; error?: boolean }) {
  return (
    <View style={[estilos.warning, error === true && estilos.noticeError]}>
      <Text style={estilos.text}>{children}</Text>
    </View>
  );
}

export function Datum({ label, value, mono }: { label: string; value: string; mono?: boolean }) {
  return (
    <View style={{ flexGrow: 1, flexBasis: '30%' }}>
      <Text style={estilos.label}>{label}</Text>
      <Text style={[estilos.value, mono === true && estilos.mono]}>{value}</Text>
    </View>
  );
}

export function Propriedade({ name, value }: { name: string; value: string }) {
  return (
    <View style={estilos.property}>
      <Text style={[estilos.text, { flexShrink: 1 }]}>{name}</Text>
      <Text style={[estilos.text, { fontWeight: '600', color: colours.textTitle }]}>{value}</Text>
    </View>
  );
}
