import { useState } from 'react';
import { Text, TextInput, View } from 'react-native';
import { Notice, Botao } from '../components/Basics';
import { estilos } from '../styles';

/**
 * The first screen, which the web front end does not need: in the browser the API is on
 * the same origin, on a phone it is a machine at the other end of the network and someone
 * has to say which.
 *
 * The address is saved, otherwise it would have to be typed at every startup.
 */
export function ConnectionScreen({
  enderecoInicial,
  onConnect,
}: {
  enderecoInicial: string;
  onConnect: (address: string) => void;
}) {
  const [address, setAddress] = useState(enderecoInicial);
  const limpo = address.trim().replace(/\/$/, '');
  const valido = /^https?:\/\/[^\s/]+$/.test(limpo);

  return (
    <View style={[estilos.card, { margin: 14 }]}>
      <Text style={estilos.cardTitle}>Onde está a bench?</Text>
      <Text style={estilos.text}>
        Endereço do computador onde a API está a correr. O telemóvel tem de estar na mesma
        rede.
      </Text>

      <TextInput
        style={estilos.field}
        value={address}
        onChangeText={setAddress}
        placeholder="http://192.168.1.50:5099"
        placeholderTextColor="#999999"
        autoCapitalize="none"
        autoCorrect={false}
        keyboardType="url"
        inputMode="url"
        accessibilityLabel="Endereço da bancada"
      />

      {!valido && address.trim().length > 0 && (
        <Text style={[estilos.smallText, { color: '#D9222B' }]}>
          Falta o http:// or the address has spaces in it.
        </Text>
      )}

      <Notice>
        A API tem de estar a aceitar ligações da rede, e não só do próprio computador — no
        appsettings.json, o Kestrel em http://0.0.0.0:5099 em vez de localhost.
      </Notice>

      <View style={estilos.row}>
        <Botao
          title="Ligar à bancada"
          principal
          desactivado={!valido}
          onPress={() => onConnect(limpo)}
        />
      </View>
    </View>
  );
}
