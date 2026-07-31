# ATMegaPesta V1

Bancada de recuperação e teste de microcontroladores AVR, para uso em laboratório de
ensino. O aluno traz um ATmega328P que deixou de responder — tipicamente com os fuses mal
gravados — e a bancada tenta chegar-lhe, ler o que lá está e guardar uma cópia antes de se
mexer em fusível nenhum.

**Esta aplicação nunca grava fuses.** Lê-os, descodifica-os e escreve o comando de reposição
numa ficha de texto, para quem quiser repô-los o fazer por sua conta e risco. Um valor
errado nessa linha fecha o ISP e o chip só volta por programação de alta tensão.

---

## O hardware

```
        PC (Windows)
         │        │
   CH340 │        │ USBAsp
   (série)        │ (ISP, via avrdude.exe)
         │        │
         ▼        ▼
  ATmega2560 ─── barramento SPI ─── ATmega328P
  Pro Mini        (comutado)         (chip-alvo,
  «master»                            no socket ZIF)
```

O **ATmega2560 Pro Mini** corre o firmware `Prog_Tester V1.2` e faz de *BusManager*: é ele
que decide quem está ligado ao barramento ISP do chip-alvo — o USBAsp, ele próprio, ou
ninguém (Hi-Z). O PC fala com ele por um menu numérico na porta série.

| Opção | Acção | No código |
|:---:|---|---|
| `1` | Devolve a assinatura `ATmega2560_Pro_ON` | `MenuTester.Signature` |
| `2` | Põe o USBAsp no barramento | `MenuTester.EnableUsbAsp` |
| `3` | Comuta o barramento para o Mega | `MenuTester.EnableMegaMaster` |
| `4` | Isola tudo em Hi-Z | `MenuTester.IsolateBus` |
| `5` | Testa a ligação Serial2 ao Nano | `MenuTester.TestSerial2` |
| `6` | Corre a sequência de teste SPI | `MenuTester.SpiTest` |

O código-fonte do firmware está em `Prog_Tester_V1.2.txt`; os sketches do Uno e do Nano em
`Uno_Master_code/` e `Nano_slave_code/`.

**Regra que atravessa o código todo:** depois de qualquer acesso ISP, o barramento volta a
Hi-Z (opção `4`). Corre mesmo quando a operação falha a meio — não se deixa o programador
ligado ao alvo.

---

## Os cinco projectos

```
ATMegaPestaV1.Core     serviços da bancada, sem UI (fala com o master, o USBAsp e o WMI)
ATMegaPestaV1          aplicação WPF — a bancada em si
ATMegaPestaV1.Api      API HTTP + SignalR sobre o mesmo Core
frontend-shared        lógica dos front ends: contratos, cliente da API, o hook useBench
frontend-react         app web (browser), sobre o frontend-shared
frontend-mobile        app Android (React Native/Expo), sobre o mesmo frontend-shared
```

O ponto da divisão é não haver duas cópias da mesma coisa:

- **`Core`** é partilhado pelo WPF e pela API. Quem fala com o hardware é ele, e só ele.
- **`frontend-shared`** é partilhado pela app web e pela app Android. Tem o fluxo inteiro —
  fases, chamadas, estado, erros — e nem uma linha de DOM. As duas apps são só apresentação
  por cima disto.

**Há duas apps de front end porque o telemóvel não tem DOM.** São as duas React, mas a web
escreve `<div>` e CSS, e a nativa escreve `<View>` e `StyleSheet`. O que dava para partilhar
está partilhado.

Detalhes de cada uma: [`frontend-react/README.md`](frontend-react/README.md) e
[`frontend-mobile/README.md`](frontend-mobile/README.md).

---

## Como correr

**Precisa de:** .NET 10 SDK, Node 20+, `avrdude.exe` no `PATH`, e a bancada ligada
(CH340 + USBAsp).

### A aplicação WPF

```bash
dotnet run --project ATMegaPestaV1
```

É a aplicação de referência — não precisa de Node nem da API.

### A app web

Dois terminais, porque o Vite não serve a API: encaminha-lhe os pedidos.

```bash
# 1) a API — fica em http://localhost:5099
dotnet run --project ATMegaPestaV1.Api

# 2) o Vite — abre em http://localhost:5173
npm run dev:web
```

Se abrir o `5173` sem a API a correr, a página carrega mas fica em erro: o `useBench` pede
a configuração e o catálogo logo no arranque.

**Em produção não são dois.** `npm run build:web` compila o React para
`ATMegaPestaV1.Api/wwwroot/`, e a partir daí é a própria API que serve a página — mesma
origem, mesma porta, sem CORS. Só precisa do `dotnet run`.

### A app Android

```bash
npm run android        # telemóvel por USB, ou emulador
npm run start:mobile   # QR code para o Expo Go
```

No primeiro ecrã escreve-se o endereço do PC da bancada (ex. `http://192.168.1.50:5099`).
Esse endereço tem de constar em `AllowedOrigins` no `appsettings.json` da API, senão o CORS
recusa.

### Verificações

```bash
dotnet build ATMegaPestaV1/ATMegaPestaV1.slnx
npm run typecheck      # tsc nos três workspaces
npm run lint           # eslint, com as Rules of Hooks
```

---

## Configuração

`ATMegaPestaV1/appsettings.json` (WPF) e `ATMegaPestaV1.Api/appsettings.json` (API):

| Chave | O que faz |
|---|---|
| `MaxAttempts` | Tentativas de detecção dos dispositivos antes de desistir |
| `BaudRate` / `SerialTimeoutMs` | Ligação série ao master |
| `VerifySignature` | Pedir a assinatura ao equipamento (`false` para firmware antigo) |
| `BackupFolder` | *(só API)* Onde ficam as cópias. Vazio → `Documentos/ATMegaPesta/Copias` |
| `AllowedOrigins` | *(só API)* Origens autorizadas pelo CORS |

As chaves ligam-se por nome ao binder de configuração. **Renomear a propriedade em C# sem
renomear a chave aqui não dá erro nenhum** — dá silenciosamente o valor por omissão.

---

## Convenção de língua

| | Língua |
|---|---|
| Identificadores, tipos, ficheiros, pastas | Inglês |
| URLs, chaves de configuração, nomes JSON | Inglês |
| Comentários e documentação XML | Inglês |
| **Tudo o que o aluno lê no ecrã** | **Português** |
| Texto da ficha de fuses, nomes dos testes | **Português** |

O critério é: código em inglês, produto na língua de quem o usa. Uma mensagem como
`"Não detectado — Ligue o conversor USB-Serial CH340"` é conteúdo, não código.

**Uma excepção que não é estilo:** o prefixo `use` dos hooks React é API, não convenção. O
`eslint-plugin-react-hooks` identifica hooks por esse prefixo — um hook chamado
`usarBancada` fica fora das Rules of Hooks sem aviso nenhum. Daí `useBench` e `useClock`, e
daí existir um `eslint.config.mjs`.

---

## O que não está implementado

**A verificação de integridade não mede nada.** Comuta o barramento para o Mega e isola-o no
fim — isso acontece de facto — mas devolve a lista de testes toda em *pendente*.

O firmware `Prog_Tester V1.2` só expõe diagnósticos de Serial2 e de SPI. Para GPIO, I²C, ADC
e PWM não há comando nenhum, e sem medição não há resultado. Um PASS/FAIL sorteado seria
indistinguível, à bancada, de uma medição verdadeira — por isso o estado existe em vez de se
inventarem resultados. Ver `IntegrityState.NotImplemented`.

Os módulos **Programação de Alta Tensão** e **Configurações** existem no menu e mostram um
ecrã "por implementar".

---

## Fluxo, do princípio ao fim

1. **Verificação** — varre o USB à procura do CH340 e do USBAsp, e pede a assinatura ao
   master. Três tentativas; esgotadas, propõe encerrar.
2. **Inserir o chip** — o ATmega328P tem de estar no ZIF antes de o ISP ir procurá-lo.
3. **Leitura das configurações** — comuta para o USBAsp, identifica o chip, lê e descodifica
   os fuses, isola o barramento. Três falhas seguidas de ISP e propõe-se a alta tensão.
4. **Cópia de segurança** — Flash e EEPROM em Intel HEX, mais a ficha dos fuses, numa
   subpasta com o carimbo da hora.
5. **Verificação de integridade** — ver acima.

O passo 5 só destranca com a leitura do passo 3 **inteira** (chip identificado *e* fuses
descodificados): verificar pinos sem saber o estado dos fuses é medir sem saber contra quê.
