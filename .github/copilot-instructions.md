# ATMegaPesta V1 — instruções para assistentes

Bancada de recuperação de microcontroladores AVR para laboratório de ensino. Ver o
[`README.md`](../README.md) na raiz para o panorama geral, o hardware e como correr.

Este ficheiro é só o que um assistente precisa de saber antes de mexer no código.

---

## Convenção de língua — leia isto primeiro

| | Língua |
|---|---|
| Identificadores, tipos, ficheiros, pastas | **Inglês** |
| URLs, chaves de configuração, nomes JSON na rede | **Inglês** |
| Comentários e documentação XML | **Inglês** |
| Tudo o que o utilizador lê no ecrã | **Português** |
| Texto da ficha de fuses, nomes dos testes | **Português** |

O critério é: código em inglês, produto na língua de quem o usa.
`"Não detectado — Ligue o conversor USB-Serial CH340"` é conteúdo, não código.

**Não traduza strings de UI para inglês.** A bancada é usada por alunos portugueses.

**O prefixo `use` dos hooks React é API, não estilo.** O `eslint-plugin-react-hooks`
identifica hooks por esse prefixo; um hook chamado `usarBancada` fica fora das Rules of
Hooks sem aviso nenhum. Nunca traduza `use*` para `usar*`.

---

## Projectos

```
ATMegaPestaV1.Core     serviços da bancada, sem UI. Quem fala com o hardware é este, e só este
ATMegaPestaV1          aplicação WPF (net10.0-windows)
ATMegaPestaV1.Api      API HTTP + SignalR sobre o mesmo Core
frontend-shared        contratos, cliente da API, hook useBench — sem DOM
frontend-react         app web (Vite), sobre o frontend-shared
frontend-mobile        app Android (Expo/React Native), sobre o mesmo frontend-shared
```

`Core` é partilhado pelo WPF e pela API. `frontend-shared` é partilhado pelas duas apps.
Antes de acrescentar lógica a um front end, pergunte se ela não pertence ao partilhado.

## Serviços do Core

| Interface | Implementação |
|---|---|
| `IBusManager` | `BusManager` — porta série CH340 |
| `IUsbAspService` | `UsbAspService` — `avrdude.exe` |
| `IDeviceDetector` | `WmiDeviceDetector` — WMI |
| `IServiceFactory` | `RealServiceFactory` |

**Há só uma implementação de cada.** Não existe modo de simulação, nem classes `Simulated*`,
nem chave `SimulationMode` — se encontrar referências a isso, estão erradas.

---

## Invariantes que não se quebram

1. **A aplicação nunca grava fuses.** Lê-os, descodifica-os e escreve o comando de reposição
   numa ficha de texto. Um valor errado fecha o ISP e o chip só volta por alta tensão.
2. **Depois de qualquer acesso ISP, o barramento volta a Hi-Z** (opção `4` do menu). Corre
   em `finally`, mesmo quando a operação falha a meio.
3. **Nada inventa resultados de medição.** A verificação de integridade devolve tudo
   pendente porque o firmware não expõe esses testes — ver `IntegrityState.NotImplemented`.
   Um PASS/FAIL sorteado seria indistinguível de uma medição verdadeira.
4. **A API é singleton e serializa tudo num semáforo.** A bancada é um recurso físico único:
   dois pedidos a comutar o barramento ao mesmo tempo deixariam o alvo ligado a dois mestres.

---

## Menu série do firmware (Prog_Tester V1.2)

O ATmega2560 Pro Mini lê **um único carácter** e responde. Fonte: `Prog_Tester_V1.2.txt`.

| Char | Acção | Constante |
|:---:|---|---|
| `1` | Assinatura → `ATmega2560_Pro_ON` | `MenuTester.Signature` |
| `2` | USBAsp no barramento ISP | `MenuTester.EnableUsbAsp` |
| `3` | Barramento para o ATmega2560 | `MenuTester.EnableMegaMaster` |
| `4` | Isolar tudo em Hi-Z | `MenuTester.IsolateBus` |
| `5` | Testar Serial2 ao Nano | `MenuTester.TestSerial2` |
| `6` | Sequência de teste SPI | `MenuTester.SpiTest` |

Erros do `BusManager` vêm como string começada por `"Erro:"` — é sentinela **e** texto de
UI ao mesmo tempo. Não a traduza nem a mude sem mudar quem a testa (`IsBusError`,
`StartsWith("Erro:")`).

---

## Fronteiras que só falham em runtime

Estas quatro atravessam ficheiros e o compilador não as verifica. Mude sempre os dois lados.

| Fronteira | Ficheiros |
|---|---|
| Nomes JSON | `Api/Bench/Contracts.cs` ↔ `frontend-shared/src/contracts.ts` |
| URLs | `Api/Program.cs` ↔ `frontend-shared/src/apiClient.ts` |
| Evento SignalR (`"progress"`) | `Api/Bench/BenchHub.cs` ↔ `apiClient.ts` |
| Chaves de configuração | `appsettings.json` ↔ `GetValue<>("...")` |

Mais duas do mesmo tipo:

- **Chaves de tema do WPF** são lidas por string a partir do C#: `ThemeBrush("BrushTextNormal")`.
  Renomear no `LightTheme.xaml` sem renomear a string devolve `Transparent` em silêncio.
- **Classes CSS**: `frontend-react/src/web/styles.css` ↔ os `className` do JSX.

Renomear uma chave de configuração só de um lado não dá erro — dá o valor por omissão.

---

## Estilo

- C# idiomático: `record` para DTOs, `async/await`, ficheiros com namespace de nível de topo.
- Comentários explicam **porquê**, não o quê. O código já diz o quê.
- Sem comentários decorativos.

### Cores

**Nunca escreva uma cor literal no code-behind.** Peça-a ao tema: `ThemeBrush("BrushPinSuccess")`
no WPF, `colours.*` de `tokens.ts` nos front ends. Só existe um tema (`LightTheme.xaml`,
fundo `#F5F5F5`), e uma cor fixa escapa-lhe.

Isto já correu mal: o `MainWindow.xaml.cs` tinha sete valores da paleta Catppuccin Mocha
(um tema **escuro**) sobre o fundo claro — o texto das mensagens ficava a 1.33:1 de
contraste, praticamente invisível.

Ao escolher ou mudar uma cor, meça-a contra `#F5F5F5`:

| Uso | Mínimo |
|---|---|
| Texto | 4.5:1 |
| LEDs, barras, molduras (não-texto) | 3:1 |
| Estados inactivos | isentos |

## Verificar antes de dar por feito

```bash
dotnet build ATMegaPestaV1/ATMegaPestaV1.slnx
npm run typecheck
npm run lint
```

---

## Problemas conhecidos — não são para "arrumar" de passagem

- **`--radius-pill` é usado e nunca declarado** em `styles.css`.
- **`BtnHelp_Click`, `HelpTopics` e `HelpDialog` são código morto** no WPF: o estilo
  `HelpButton` existe mas nenhum botão o usa, e nenhum `Tag=` liga ao dicionário.
- **Só existe `LightTheme.xaml`.** Os comentários do XAML falam de temas vintage e escuro —
  são de uma intenção que não chegou a existir.
- **A verificação de integridade não mede nada** (ver invariante 3). Não é um bug para
  corrigir; é o firmware que não expõe os comandos.
