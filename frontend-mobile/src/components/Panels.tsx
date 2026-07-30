import { Text, View } from 'react-native';
import type {
  Catalog,
  TestState,
  FlashMap as Mapa,
  TestResult,
} from '@atmegapesta/shared';
import { colours, estilos } from '../styles';

/**
 * The Flash bar: how much is left for the application after the bootloader is reserved.
 * It is capacity, not occupancy — the Flash contents are not read.
 */
export function FlashMap({ mapa }: { mapa: Mapa }) {
  const pctApp = Math.round((mapa.applicationBytes / mapa.totalBytes) * 100);

  return (
    <View style={{ gap: 6 }}>
      <Text style={estilos.label}>Mapa da Flash</Text>

      <View
        style={estilos.bar}
        accessibilityRole="image"
        accessibilityLabel={`Flash de ${mapa.totalBytes} bytes: ${mapa.application} para a aplicação${
          mapa.bootloaderReserved ? `, ${mapa.bootloader} reservados ao bootloader` : ''
        }.`}
      >
        <View style={{ width: `${pctApp}%`, backgroundColor: colours.success }} />
        {mapa.bootloaderReserved && (
          <View style={{ width: `${100 - pctApp}%`, backgroundColor: colours.badge }} />
        )}
      </View>

      <View style={{ gap: 3 }}>
        <Legend colour={colours.success} text={`Disponível para a aplicação — ${mapa.application}`} />
        {mapa.bootloaderReserved && (
          <Legend colour={colours.badge} text={`Bootloader — ${mapa.bootloader}`} />
        )}
        <Text style={estilos.smallText}>Capacidade, não ocupação: a Flash não é lida.</Text>
      </View>
    </View>
  );
}

function Legend({ colour, text }: { colour: string; text: string }) {
  return (
    <View style={estilos.row}>
      <View style={{ width: 10, height: 10, borderRadius: 2, backgroundColor: colour }} />
      <Text style={estilos.smallText}>{text}</Text>
    </View>
  );
}

/**
 * The ATmega328P pins and the buses covered.
 * All idle because nothing was measured — painting them would be saying they are good.
 */
export function PinMap({ catalog }: { catalog: Catalog }) {
  const tags = [
    ...new Set(catalog.tests.map((t) => t.tag).filter((t): t is string => t !== null)),
    'ISP',
  ];

  return (
    <View style={{ gap: 10 }}>
      <View style={{ gap: 6 }}>
        <Text style={estilos.label}>Estado dos pins GPIO</Text>
        <View style={estilos.grid}>
          {catalog.pins.map((pin) => (
            <View key={pin} style={estilos.pin}>
              <Text style={estilos.pinText}>{pin}</Text>
            </View>
          ))}
        </View>
      </View>

      <View style={{ gap: 6 }}>
        <Text style={estilos.label}>Barramentos cobertos</Text>
        <View style={estilos.grid}>
          {tags.map((tag) => (
            <View key={tag} style={estilos.tag}>
              <Text style={estilos.tagText}>{tag}</Text>
            </View>
          ))}
        </View>
      </View>

      <Text style={estilos.smallText}>
        Todos os pins aparecem inactivos: nada foi medido.
      </Text>
    </View>
  );
}

/** Os testes previstos, com os pinos que cada um exercitaria. */
export function ListaTestes({
  catalog,
  results,
}: {
  catalog: Catalog;
  results: TestResult[] | null;
}) {
  const porNome = new Map(results?.map((r) => [r.name, r]) ?? []);

  return (
    <View>
      {catalog.tests.map((test) => {
        const result = porNome.get(test.name);
        const state = result?.state ?? 'pending';

        return (
          <View key={test.name} style={estilos.testRow}>
            <View style={{ flex: 1 }}>
              <Text style={[estilos.text, { fontWeight: '600', color: colours.textTitle }]}>
                {test.name}
              </Text>
              <Text style={[estilos.smallText, estilos.mono]}>{test.pins.join(' ')}</Text>
            </View>

            <Text style={[estilos.smallText, estilos.mono]}>{result?.time ?? 'n/d'}</Text>

            <View style={[estilos.badge, { backgroundColor: corBadge(state) }]}>
              <Text style={estilos.badgeText}>{palavra(state)}</Text>
            </View>
          </View>
        );
      })}
    </View>
  );
}

function corBadge(state: TestState): string {
  switch (state) {
    case 'passed':
      return colours.success;
    case 'failed':
      return colours.error;
    case 'pending':
      return colours.badge;
  }
}

function palavra(state: TestState): string {
  switch (state) {
    case 'passed':
      return 'Passou';
    case 'failed':
      return 'Falhou';
    case 'pending':
      return 'Pendente';
  }
}
