import type { ReactNode } from 'react';
import { Modal, ScrollView, Text, View } from 'react-native';
import { estilos } from '../styles';

/**
 * Native modal dialog. `onClose` also runs on Android's physical back button — whoever
 * goes back has made the same decision as whoever presses Cancel, and one of these
 * dialogs closed without an answer would leave the bench waiting for a choice that never
 * arrives.
 */
export function Dialog({
  open,
  title,
  onClose,
  actions,
  children,
}: {
  open: boolean;
  title: string;
  onClose: () => void;
  actions: ReactNode;
  children: ReactNode;
}) {
  return (
    <Modal visible={open} transparent animationType="fade" onRequestClose={onClose}>
      <View style={estilos.dialogBackdrop}>
        <View style={estilos.dialog}>
          <Text style={estilos.dialogTitle}>{title}</Text>

          <ScrollView contentContainerStyle={estilos.stack}>{children}</ScrollView>

          <View style={estilos.row}>{actions}</View>
        </View>
      </View>
    </Modal>
  );
}
