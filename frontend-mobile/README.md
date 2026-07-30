# App Android — ATMegaPesta V1

A bancada num telemóvel, em React Native. Só Android para já.

## Como está organizado

Há três pastas de front end no repositório e uma delas não é uma app:

```
frontend-shared/     a lógica da bancada — usada pelas duas apps, sem cópias
frontend-react/      a app web (browser)
frontend-mobile/     esta app (Android)
```

`frontend-shared` tem os tipos, o cliente da API e o fluxo todo (`usarBancada`). Esta app
não reimplementa nada disso — só desenha ecrãs nativos por cima. Se a API mudar, muda-se
num sítio e as duas apps acompanham.

Nesta pasta só existe apresentação:

```
App.tsx              junta tudo; guarda o endereço da bancada entre arranques
src/estilos.ts       StyleSheet a partir dos tokens partilhados
src/componentes/     Led, Cartão, Botão, mapas, diálogo
src/ecrans/          Ligação, Verificação, Inserir chip, Trabalho
```

## O que é o Expo

É o kit com que se fazem apps React Native hoje — o caminho recomendado na documentação do
próprio React Native. Trata da parte de compilar para Android, para não teres de configurar
o Android Studio e o Gradle à mão. O código dos ecrãs é React Native normal: `View`,
`Text`, `StyleSheet`.

## Correr em desenvolvimento

Precisas de: Node 20+, um telemóvel Android com a app **Expo Go** (grátis, na Play Store),
e o telemóvel na mesma rede Wi-Fi que o computador da bancada.

**1. Abrir a API à rede.** Por omissão ela só aceita ligações do próprio computador. Em
`ATMegaPestaV1.Api/appsettings.json`:

```json
"Kestrel": { "Endpoints": { "Http": { "Url": "http://0.0.0.0:5099" } } }
```

E descobrir o IP do computador (`ipconfig`, procurar o IPv4 da rede Wi-Fi).

**2. Arrancar as duas coisas:**

```bash
dotnet run --project ATMegaPestaV1.Api    # numa consola
npm run start:mobile                       # noutra, a partir da raiz do repo
```

**3.** Ler o código QR que aparece na consola, com a Expo Go. No primeiro ecrã da app,
escrever o endereço da bancada (ex.: `http://192.168.1.50:5099`) e ligar. Fica guardado.

## Fazer o APK instalável

O Expo Go serve para desenvolver. Para teres um ficheiro `.apk` que instalas e fica lá,
precisas de um build — por uma de duas vias.

**Via EAS Build (na nuvem, sem instalar nada):** precisa de conta Expo, grátis para builds
ocasionais.

```bash
npm install -g eas-cli
eas login
cd frontend-mobile
eas build --platform android --profile preview
```

Devolve um link para descarregar o APK. O perfil `preview` é o que dá um APK instalável
directamente; o `production` dá um AAB, que é o formato da Play Store e não se instala à mão.

**Via build local:** precisa de Java (JDK 17) e do Android SDK instalados.

```bash
cd frontend-mobile
npx expo run:android --variant release
```

## Porque é que a app fala HTTP e não HTTPS

O Android bloqueia HTTP em claro desde a API 28. Isto está ligado de propósito em
`app.json`:

```json
["expo-build-properties", { "android": { "usesCleartextTraffic": true } }]
```

Sem isso a app não conseguiria falar com a bancada, que serve HTTP simples na rede local.

**Vale a pena pensar nisto:** a API comuta o barramento ISP e transfere programas para o
chip-alvo. Aberta à rede da escola, quem estiver nessa rede consegue fazê-lo. Se isso
incomodar, o passo certo é um token de acesso na API — e é mais fácil pôr agora do que
depois de haver várias apps a usá-la.

## Diferenças em relação à app web

- **Escolher a bancada.** No browser a API está na mesma origem e não há nada a configurar.
  Aqui há um primeiro ecrã para o endereço, guardado entre arranques.
- **Descarregar a cópia.** Os ficheiros da cópia estão no computador da bancada. Os botões
  abrem o URL no browser do telemóvel e o gestor de downloads do Android trata do resto.
- **Programação de alta tensão.** Continua por implementar, como no WPF e na web. O diálogo
  diz isso em vez de fingir que faz algo.

## A verificação de integridade não mede nada

Os testes de GPIO, I²C, ADC e PWM aparecem sempre **pendentes** e o progresso a 0%. Não é
falta de ligação: o firmware `Prog_Tester V1.2` só expõe diagnósticos de Serial2 e de SPI, e
para os restantes não há comando. Um PASS/FAIL sorteado seria indistinguível, à bancada, de
uma medição verdadeira.

O que acontece de facto ao carregar em "Iniciar verificação": a cópia (se pedida), a
comutação do barramento para o ATmega2560, e o isolamento no fim.

## Por testar

Esta app foi escrita e empacotada, mas **nunca correu num telemóvel** — a máquina onde foi
feita não tem Java nem o Android SDK, nem havia bancada ligada. O que está verificado é que
o TypeScript compila e que o Metro produz o pacote Android com a lógica partilhada dentro.

Fica em particular por confirmar num aparelho real:

- O canal de progresso (SignalR) sobre WebSockets no Android. Está forçado a WebSockets
  precisamente para evitar os transportes que dependem de APIs de browser, mas é o ponto
  mais provável de precisar de um ajuste.
- O `Linking.openURL` a descarregar os ficheiros da cópia.
