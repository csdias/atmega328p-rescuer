import { useCallback, useEffect, useState } from 'react';
import { SafeAreaView, ScrollView, Text, View } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { useBench } from '@atmegapesta/shared';
import { InsertChipScreen } from './src/screens/InsertChipScreen';
import { ConnectionScreen } from './src/screens/ConnectionScreen';
import { WorkScreen } from './src/screens/WorkScreen';
import { VerificationScreen } from './src/screens/VerificationScreen';
import { estilos } from './src/styles';

const CHAVE_ENDERECO = 'atmegapesta.address';

export default function App() {
  const [address, setAddress] = useState<string | null>(null);
  const [savedLoaded, setSavedLoaded] = useState(false);
  const [savedAddress, setSavedAddress] = useState('http://192.168.1.50:5099');

  // The bench address does not change from one day to the next — retyping it at every
  // trabalho para nada.
  useEffect(() => {
    let vivo = true;

    void AsyncStorage.getItem(CHAVE_ENDERECO)
      .then((value) => {
        if (!vivo) return;
        if (value) {
          setSavedAddress(value);
          setAddress(value);
        }
      })
      .finally(() => {
        if (vivo) setSavedLoaded(true);
      });

    return () => {
      vivo = false;
    };
  }, []);

  const ligar = useCallback((novo: string) => {
    setAddress(novo);
    setSavedAddress(novo);
    void AsyncStorage.setItem(CHAVE_ENDERECO, novo);
  }, []);

  const trocar = useCallback(() => setAddress(null), []);

  if (!savedLoaded)
    return (
      <SafeAreaView style={estilos.screen}>
        <StatusBar style="dark" />
      </SafeAreaView>
    );

  if (address === null)
    return (
      <SafeAreaView style={estilos.screen}>
        <StatusBar style="dark" />
        <Cabecalho address={null} />
        <ConnectionScreen enderecoInicial={savedAddress} onConnect={ligar} />
      </SafeAreaView>
    );

  // The key forces a fresh mount when switching bench: the hook keeps state from the
  // previous bench (port, attempts) that does not hold for another one.
  return <Bench key={address} address={address} onSwitchBench={trocar} />;
}

function Bench({
  address,
  onSwitchBench,
}: {
  address: string;
  onSwitchBench: () => void;
}) {
  const bench = useBench(address);

  return (
    <SafeAreaView style={estilos.screen}>
      <StatusBar style="dark" />
      <Cabecalho address={address} porta={bench.detection?.comPort ?? null} />

      <ScrollView contentContainerStyle={estilos.content}>
        {bench.phase === 'verification' && (
          <VerificationScreen bench={bench} onSwitchBench={onSwitchBench} />
        )}
        {bench.phase === 'insertChip' && <InsertChipScreen bench={bench} />}
        {bench.phase === 'work' && <WorkScreen bench={bench} />}
      </ScrollView>
    </SafeAreaView>
  );
}

function Cabecalho({ address, porta }: { address: string | null; porta?: string | null }) {
  return (
    <View style={estilos.header}>
      <Text style={estilos.headerTitle}>ATMegaPesta — Banca de recuperação</Text>
      <Text style={estilos.headerSub}>
        {address === null
          ? 'Sem bancada escolhida'
          : `${address}${porta ? ` · master em ${porta}` : ''}`}
      </Text>
    </View>
  );
}
