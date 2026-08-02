# Front end React — ATMegaPesta V1

A bancada no browser, a par da aplicação WPF. A lógica não vive aqui: está em
`frontend-shared`, partilhada com a app Android (`frontend-mobile`).

## Porque é que há uma API

O front end React corre num browser e não chega ao hardware: a bancada usa WMI para
enumerar o USB, `System.IO.Ports` para falar com o master e `avrdude.exe` para chegar ao
chip-alvo. Nada disso existe no browser.

Por isso os serviços saíram do projecto WPF para `ATMegaPestaV1.Core`, e
`ATMegaPestaV1.Api` expõe-nos por HTTP. Os dois front ends usam exactamente o mesmo código
de acesso ao hardware.

```
ATMegaPestaV1.Core/     serviços da bancada (WMI, porta série, avrdude) — sem UI
ATMegaPestaV1/          WPF            ─┐
ATMegaPestaV1.Api/      HTTP + SignalR  ├─ os dois usam o Core
                                        │
frontend-shared/        lógica da bancada — sem DOM
frontend-react/         web (esta pasta) ─┐
frontend-mobile/        Android           ┴─ as duas usam o frontend-shared
```

## Estrutura

Nesta pasta só existe apresentação web:

```
src/
├─ main.tsx
└─ web/     components/ DOM, screens/ e a folha de estilos (styles.css)
```

A lógica — tipos, cliente da API, tokens da paleta e o fluxo inteiro (`useBench`) —
está em `frontend-shared` e importa-se pelo nome do pacote:

```ts
import { useBench } from '@atmegapesta/shared';
```

A app Android importa exactamente o mesmo. `fetch` e `@microsoft/signalr` funcionam nos
dois, e `tokens.ts` está em valores (não em CSS) para o nativo o passar a `StyleSheet`
sem conversão.

## Como correr

Preciso: .NET 10 SDK, Node 20+, e a bancada ligada (CH340 + USBAsp) com o `avrdude.exe` no
PATH.

**Desenvolvimento** — duas consolas:

O `npm install` corre uma vez, na **raiz do repositório** — instala as três pastas de front
end de uma vez.

```bash
# 1) a API (porta 5099)
dotnet run --project ATMegaPestaV1.Api

# 2) o Vite (porta 5173, encaminha /api e /hub para a API)
npm install       # na raiz do repo, uma vez
npm run dev:web
```

Abrir <http://localhost:5173>.

**Produção** — o build do React vai para o `wwwroot` da API, que passa a servir tudo na
mesma origem:

```bash
npm run build:web       # na raiz do repo
dotnet run --project ATMegaPestaV1.Api
```

Abrir <http://localhost:5099>.

## Configuração

`ATMegaPestaV1.Api/appsettings.json`:

| Chave | O que faz |
|---|---|
| `MaxAttempts` | Tentativas de detecção antes de desistir. |
| `BaudRate`, `SerialTimeoutMs` | Ligação série ao master. |
| `VerifySignature` | `false` para firmware antigo, que não responde à opção 1. |
| `BackupFolder` | Onde ficam as cópias. Vazio → `Documentos/ATMegaPesta/Copias`. |
| `AllowedOrigins` | Origens com CORS — o dev server do Vite. A app Android não precisa: CORS é uma regra de browser. |
| `Kestrel:Endpoints:Http:Url` | `http://localhost:5099`. |

### Abrir à rede local

Por omissão a API só ouve em loopback. Para o React Native num telefone chegar à bancada,
trocar o URL do Kestrel por `http://0.0.0.0:5099` **e abrir a porta na firewall do
Windows** — é este segundo passo que costuma faltar:

```powershell
New-NetFirewallRule -DisplayName "ATMegaPesta API 5099" `
  -Direction Inbound -LocalPort 5099 -Protocol TCP -Action Allow -Profile Private
```

Não é preciso mexer em `AllowedOrigins`: como diz a tabela acima, CORS é uma regra de
browser e o `fetch` do React Native não envia cabeçalho `Origin`.

Vale a pena decidir isso de propósito: esta API comuta o barramento ISP e transfere
programas para o chip-alvo. Aberta à rede, quem estiver nela consegue fazê-lo.

## Duas diferenças em relação ao WPF

Não são omissões — são coisas que no browser não se fazem da mesma maneira.

- **Pasta da cópia.** O WPF abre um selector de pastas. O browser não escolhe pastas do
  servidor, por isso a cópia fica em `BackupFolder`, numa subpasta com o carimbo da hora, e
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
