import { Text, View } from 'react-native';
import type {
  Catalogo,
  EstadoTeste,
  MapaFlash as Mapa,
  ResultadoTeste,
} from '@atmegapesta/partilhado';
import { cores, estilos } from '../estilos';

/**
 * A barra da Flash: quanto sobra para a aplicação depois de reservado o bootloader.
 * É capacidade, não ocupação — o conteúdo da Flash não é lido.
 */
export function MapaFlash({ mapa }: { mapa: Mapa }) {
  const pctApp = Math.round((mapa.aplicacaoBytes / mapa.totalBytes) * 100);

  return (
    <View style={{ gap: 6 }}>
      <Text style={estilos.rotulo}>Mapa da Flash</Text>

      <View
        style={estilos.barra}
        accessibilityRole="image"
        accessibilityLabel={`Flash de ${mapa.totalBytes} bytes: ${mapa.aplicacao} para a aplicação${
          mapa.bootloaderReservado ? `, ${mapa.bootloader} reservados ao bootloader` : ''
        }.`}
      >
        <View style={{ width: `${pctApp}%`, backgroundColor: cores.sucesso }} />
        {mapa.bootloaderReservado && (
          <View style={{ width: `${100 - pctApp}%`, backgroundColor: cores.badge }} />
        )}
      </View>

      <View style={{ gap: 3 }}>
        <Legenda cor={cores.sucesso} texto={`Disponível para a aplicação — ${mapa.aplicacao}`} />
        {mapa.bootloaderReservado && (
          <Legenda cor={cores.badge} texto={`Bootloader — ${mapa.bootloader}`} />
        )}
        <Text style={estilos.textoPequeno}>Capacidade, não ocupação: a Flash não é lida.</Text>
      </View>
    </View>
  );
}

function Legenda({ cor, texto }: { cor: string; texto: string }) {
  return (
    <View style={estilos.linha}>
      <View style={{ width: 10, height: 10, borderRadius: 2, backgroundColor: cor }} />
      <Text style={estilos.textoPequeno}>{texto}</Text>
    </View>
  );
}

/**
 * Os pinos do ATmega328P e os barramentos cobertos.
 * Todos inactivos porque nada foi medido — pintá-los seria dizer que estão bons.
 */
export function MapaPinos({ catalogo }: { catalogo: Catalogo }) {
  const tags = [
    ...new Set(catalogo.testes.map((t) => t.tag).filter((t): t is string => t !== null)),
    'ISP',
  ];

  return (
    <View style={{ gap: 10 }}>
      <View style={{ gap: 6 }}>
        <Text style={estilos.rotulo}>Estado dos pinos GPIO</Text>
        <View style={estilos.grelha}>
          {catalogo.pinos.map((pino) => (
            <View key={pino} style={estilos.pino}>
              <Text style={estilos.pinoTexto}>{pino}</Text>
            </View>
          ))}
        </View>
      </View>

      <View style={{ gap: 6 }}>
        <Text style={estilos.rotulo}>Barramentos cobertos</Text>
        <View style={estilos.grelha}>
          {tags.map((tag) => (
            <View key={tag} style={estilos.tag}>
              <Text style={estilos.tagTexto}>{tag}</Text>
            </View>
          ))}
        </View>
      </View>

      <Text style={estilos.textoPequeno}>
        Todos os pinos aparecem inactivos: nada foi medido.
      </Text>
    </View>
  );
}

/** Os testes previstos, com os pinos que cada um exercitaria. */
export function ListaTestes({
  catalogo,
  resultados,
}: {
  catalogo: Catalogo;
  resultados: ResultadoTeste[] | null;
}) {
  const porNome = new Map(resultados?.map((r) => [r.nome, r]) ?? []);

  return (
    <View>
      {catalogo.testes.map((teste) => {
        const resultado = porNome.get(teste.nome);
        const estado = resultado?.estado ?? 'pendente';

        return (
          <View key={teste.nome} style={estilos.linhaTeste}>
            <View style={{ flex: 1 }}>
              <Text style={[estilos.texto, { fontWeight: '600', color: cores.textoTitulo }]}>
                {teste.nome}
              </Text>
              <Text style={[estilos.textoPequeno, estilos.mono]}>{teste.pinos.join(' ')}</Text>
            </View>

            <Text style={[estilos.textoPequeno, estilos.mono]}>{resultado?.tempo ?? 'n/d'}</Text>

            <View style={[estilos.badge, { backgroundColor: corBadge(estado) }]}>
              <Text style={estilos.badgeTexto}>{palavra(estado)}</Text>
            </View>
          </View>
        );
      })}
    </View>
  );
}

function corBadge(estado: EstadoTeste): string {
  switch (estado) {
    case 'passou':
      return cores.sucesso;
    case 'falhou':
      return cores.erro;
    case 'pendente':
      return cores.badge;
  }
}

function palavra(estado: EstadoTeste): string {
  switch (estado) {
    case 'passou':
      return 'Passou';
    case 'falhou':
      return 'Falhou';
    case 'pendente':
      return 'Pendente';
  }
}
