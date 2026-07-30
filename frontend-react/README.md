# Front end React — ATMegaPesta V1

Segundo front end da bancada, a par da aplicação WPF. Existe para preparar o React Native:
a lógica que o nativo vai precisar está toda em `src/partilhado`, sem uma linha de DOM.

## Porque é que há uma API

O front end React corre num browser e não chega ao hardware: a bancada usa WMI para
enumerar o USB, `System.IO.Ports` para falar com o master e `avrdude.exe` para chegar ao
chip-alvo. Nada disso existe no browser.

Por isso os serviços saíram do projecto WPF para `ATMegaPestaV1.Core`, e
`ATMegaPestaV1.Api` expõe-nos por HTTP. Os dois front ends usam exactamente o mesmo código
de acesso ao hardware.

```
ATMegaPestaV1.Core/     serviços da bancada (WMI, porta série, avrdude) — sem UI
ATMegaPestaV1/          front end WPF          ─┐
ATMegaPestaV1.Api/      HTTP + SignalR          ├─ os dois usam o Core
frontend-react/         front end React        ─┘  (o React através da API)
```

## Estrutura, e o que o React Native vai reutilizar

```
src/
├─ partilhado/          sem DOM — copia-se para o React Native como está
│  ├─ contratos.ts      os tipos, espelhando Api/Bancada/Contratos.cs
│  ├─ clienteApi.ts     fetch + SignalR
│  ├─ tokens.ts         paleta e medidas, em valores (não em CSS)
│  └─ usarBancada.ts    o fluxo todo: estado, transições, chamadas
└─ web/                 só web — componentes DOM e a folha de estilos
```

No React Native muda-se a camada `web/` por ecrãs nativos e passa-se o endereço da bancada
ao hook:

```ts
const bancada = usarBancada('http://192.168.1.50:5099');
```

`fetch` e `@microsoft/signalr` funcionam nos dois, e `tokens.ts` está em valores para
alimentar um `StyleSheet` sem conversão.

## Como correr

Preciso: .NET 10 SDK, Node 20+, e a bancada ligada (CH340 + USBAsp) com o `avrdude.exe` no
PATH.

**Desenvolvimento** — duas consolas:

```bash
# 1) a API (porta 5099)
dotnet run --project ATMegaPestaV1.Api

# 2) o Vite (porta 5173, encaminha /api e /hub para a API)
cd frontend-react
npm install
npm run dev
```

Abrir <http://localhost:5173>.

**Produção** — o build do React vai para o `wwwroot` da API, que passa a servir tudo na
mesma origem:

```bash
cd frontend-react && npm run build
dotnet run --project ATMegaPestaV1.Api
```

Abrir <http://localhost:5099>.

## Configuração

`ATMegaPestaV1.Api/appsettings.json`:

| Chave | O que faz |
|---|---|
| `MaxTentativas` | Tentativas de detecção antes de desistir. |
| `BaudRate`, `SerialTimeoutMs` | Ligação série ao master. |
| `VerificarAssinatura` | `false` para firmware antigo, que não responde à opção 1. |
| `PastaCopias` | Onde ficam as cópias. Vazio → `Documentos/ATMegaPesta/Copias`. |
| `OrigensPermitidas` | Origens com CORS — o dev server do Vite, e o telefone quando o React Native chegar. |
| `Kestrel:Endpoints:Http:Url` | `http://localhost:5099`. |

### Abrir à rede local

Por omissão a API só aceita ligações do próprio PC. Para o React Native num telefone
chegar à bancada, trocar o URL do Kestrel por `http://0.0.0.0:5099` e acrescentar a origem
do telefone a `OrigensPermitidas`.

Vale a pena decidir isso de propósito: esta API comuta o barramento ISP e transfere
programas para o chip-alvo. Aberta à rede, quem estiver nela consegue fazê-lo.

## Duas diferenças em relação ao WPF

Não são omissões — são coisas que no browser não se fazem da mesma maneira.

- **Pasta da cópia.** O WPF abre um selector de pastas. O browser não escolhe pastas do
  servidor, por isso a cópia fica em `PastaCopias`, numa subpasta com o carimbo da hora, e
  descarrega-se pelos links que a resposta traz.
- **Encerrar a aplicação.** Onde o WPF fecha a janela (tentativas esgotadas, recusa da alta
  tensão), o React oferece recomeçar o ciclo — não há aplicação para fechar.

## A verificação de integridade não mede nada

Os testes de GPIO, I²C, ADC e PWM aparecem sempre **pendentes**, e o progresso a 0%. Não é
um bug nem falta de ligação.

O firmware `Prog_Tester V1.2` só expõe diagnósticos de Serial2 e de SPI; para os restantes
não há comando nenhum, e sem medição não há resultado. O WPF tomou a mesma decisão — um
PASS/FAIL sorteado é indistinguível, à bancada, de uma medição verdadeira.

O que acontece de facto quando se carrega em "Iniciar verificação": a cópia (se pedida), a
comutação do barramento para o ATmega2560, e o isolamento no fim. Só a medição falta.
