import { useCallback, useEffect, useState } from 'react';
import { SafeAreaView, ScrollView, Text, View } from 'react-native';
import { StatusBar } from 'expo-status-bar';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { usarBancada } from '@atmegapesta/partilhado';
import { EcranInserirChip } from './src/ecrans/EcranInserirChip';
import { EcranLigacao } from './src/ecrans/EcranLigacao';
import { EcranTrabalho } from './src/ecrans/EcranTrabalho';
import { EcranVerificacao } from './src/ecrans/EcranVerificacao';
import { estilos } from './src/estilos';

const CHAVE_ENDERECO = 'atmegapesta.endereco';

export default function App() {
  const [endereco, setEndereco] = useState<string | null>(null);
  const [guardadoLido, setGuardadoLido] = useState(false);
  const [enderecoGuardado, setEnderecoGuardado] = useState('http://192.168.1.50:5099');

  // O endereço da bancada não muda de dia para dia — reescrevê-lo a cada arranque seria
  // trabalho para nada.
  useEffect(() => {
    let vivo = true;

    void AsyncStorage.getItem(CHAVE_ENDERECO)
      .then((valor) => {
        if (!vivo) return;
        if (valor) {
          setEnderecoGuardado(valor);
          setEndereco(valor);
        }
      })
      .finally(() => {
        if (vivo) setGuardadoLido(true);
      });

    return () => {
      vivo = false;
    };
  }, []);

  const ligar = useCallback((novo: string) => {
    setEndereco(novo);
    setEnderecoGuardado(novo);
    void AsyncStorage.setItem(CHAVE_ENDERECO, novo);
  }, []);

  const trocar = useCallback(() => setEndereco(null), []);

  if (!guardadoLido)
    return (
      <SafeAreaView style={estilos.ecra}>
        <StatusBar style="dark" />
      </SafeAreaView>
    );

  if (endereco === null)
    return (
      <SafeAreaView style={estilos.ecra}>
        <StatusBar style="dark" />
        <Cabecalho endereco={null} />
        <EcranLigacao enderecoInicial={enderecoGuardado} aoLigar={ligar} />
      </SafeAreaView>
    );

  // A chave força uma montagem nova ao trocar de bancada: o hook guarda estado do
  // equipamento anterior (porta, tentativas) que não vale para outro.
  return <Bancada key={endereco} endereco={endereco} aoTrocarBancada={trocar} />;
}

function Bancada({
  endereco,
  aoTrocarBancada,
}: {
  endereco: string;
  aoTrocarBancada: () => void;
}) {
  const bancada = usarBancada(endereco);

  return (
    <SafeAreaView style={estilos.ecra}>
      <StatusBar style="dark" />
      <Cabecalho endereco={endereco} porta={bancada.deteccao?.portaCom ?? null} />

      <ScrollView contentContainerStyle={estilos.conteudo}>
        {bancada.fase === 'verificacao' && (
          <EcranVerificacao bancada={bancada} aoTrocarBancada={aoTrocarBancada} />
        )}
        {bancada.fase === 'inserirChip' && <EcranInserirChip bancada={bancada} />}
        {bancada.fase === 'trabalho' && <EcranTrabalho bancada={bancada} />}
      </ScrollView>
    </SafeAreaView>
  );
}

function Cabecalho({ endereco, porta }: { endereco: string | null; porta?: string | null }) {
  return (
    <View style={estilos.cabecalho}>
      <Text style={estilos.cabecalhoTitulo}>ATMegaPesta — Banca de recuperação</Text>
      <Text style={estilos.cabecalhoSub}>
        {endereco === null
          ? 'Sem bancada escolhida'
          : `${endereco}${porta ? ` · master em ${porta}` : ''}`}
      </Text>
    </View>
  );
}
