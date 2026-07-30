import { useState } from 'react';
import { Text, TextInput, View } from 'react-native';
import { Aviso, Botao } from '../componentes/Basicos';
import { estilos } from '../estilos';

/**
 * O primeiro ecrã, que o front end web não precisa de ter: no browser a API está na mesma
 * origem, no telemóvel é uma máquina noutra ponta da rede e alguém tem de dizer qual.
 *
 * O endereço fica guardado, senão obrigava a escrevê-lo a cada arranque.
 */
export function EcranLigacao({
  enderecoInicial,
  aoLigar,
}: {
  enderecoInicial: string;
  aoLigar: (endereco: string) => void;
}) {
  const [endereco, setEndereco] = useState(enderecoInicial);
  const limpo = endereco.trim().replace(/\/$/, '');
  const valido = /^https?:\/\/[^\s/]+$/.test(limpo);

  return (
    <View style={[estilos.cartao, { margin: 14 }]}>
      <Text style={estilos.cartaoTitulo}>Onde está a bancada?</Text>
      <Text style={estilos.texto}>
        Endereço do computador onde a API está a correr. O telemóvel tem de estar na mesma
        rede.
      </Text>

      <TextInput
        style={estilos.campo}
        value={endereco}
        onChangeText={setEndereco}
        placeholder="http://192.168.1.50:5099"
        placeholderTextColor="#999999"
        autoCapitalize="none"
        autoCorrect={false}
        keyboardType="url"
        inputMode="url"
        accessibilityLabel="Endereço da bancada"
      />

      {!valido && endereco.trim().length > 0 && (
        <Text style={[estilos.textoPequeno, { color: '#D9222B' }]}>
          Falta o http:// ou o endereço tem espaços.
        </Text>
      )}

      <Aviso>
        A API tem de estar a aceitar ligações da rede, e não só do próprio computador — no
        appsettings.json, o Kestrel em http://0.0.0.0:5099 em vez de localhost.
      </Aviso>

      <View style={estilos.linha}>
        <Botao
          titulo="Ligar à bancada"
          principal
          desactivado={!valido}
          aoPremir={() => aoLigar(limpo)}
        />
      </View>
    </View>
  );
}
