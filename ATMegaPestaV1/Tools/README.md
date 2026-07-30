# PestaBench — bancada por linha de comandos

Módulo PowerShell que conduz o tester ATMega2560 Pro Mini e o USBAsp por script,
em vez de à mão no Serial Monitor.

```powershell
Import-Module C:\Abc\ATMegaPestaV1\ATMegaPestaV1\Tools\PestaBench.psm1 -Force
Read-TargetInfo
```

---

## O que muda em relação ao método manual

O método habitual era: abrir o Serial Monitor, ver o menu, carregar numa tecla.
Isso continua válido — e o menu é exactamente o mesmo. A diferença está em duas coisas.

**1. O menu passa a ser conduzível por script.** `Send-PestaCommand -Option 2` faz
o que fazia a tecla `2`. Ganha-se repetibilidade, e o resultado volta como texto que
se pode testar (`if ($r.Valida) { ... }`) em vez de ser lido com os olhos.

**2. O avrdude é um canal separado, não passa pelo menu.** Este é o ponto que
costuma confundir. São dois caminhos físicos independentes:

```
PC ──USB──> CH340 ──> ATMega2560         menu: decide QUEM manda no barramento
PC ──USB──> USBAsp ──ISP──> ATmega328P   dados: lê/escreve mesmo o chip
```

O Mega **nunca vê** o tráfego do avrdude. A única função do menu, no que toca à
programação, é encaminhar o barramento ISP. Daí a regra de ouro:

> Encaminhar primeiro (opção 2), correr o avrdude depois.

Se correr o avrdude com o barramento isolado, dá `target doesn't answer` mesmo com
um chip perfeito no soquete — e o erro não distingue "chip morto" de "barramento
fechado". Já perdemos tempo com isto.

---

## Comandos

### Diagnóstico completo (o que se usa 90% das vezes)

| Comando | Faz |
|---|---|
| `Test-PestaHardware` | Confirma CH340, USBAsp e avrdude. **Corra isto primeiro.** |
| `Read-TargetInfo` | Sequência inteira: valida tester → encaminha → lê assinatura e fuses → volta a isolar |
| `Read-TargetInfo -KeepBusConnected` | Igual, mas deixa o barramento ligado para continuar a trabalhar |

`Read-TargetInfo` isola o barramento num bloco `finally` — mesmo que a leitura
rebente a meio, a bancada não fica com o ISP energizado.

### Menu do firmware (canal CH340)

| Comando | Opção |
|---|---|
| `Get-PestaSignature` | 1 — assinatura do **tester**, valida contra `ATmega2560_Pro_ON` |
| `Set-PestaBus -Mode UsbAsp` | 2 |
| `Set-PestaBus -Mode Mega` | 3 |
| `Set-PestaBus -Mode Isolate` | 4 |
| `Test-PestaSerial2` | 5 |
| `Test-PestaSpi` | 6 |
| `Send-PestaCommand -Option N` | qualquer opção, em cru |

### Chip-alvo (canal USBAsp)

| Comando | Faz |
|---|---|
| `Get-TargetSignature` | Assinatura do chip no ZIF |
| `Get-TargetFuses` | lfuse/hfuse/efuse/lock **já descodificados** |

Ambos exigem o barramento em `UsbAsp`. Aceitam `-Part` (por omissão `m328p`).

**Nada neste módulo escreve no chip.** É tudo leitura.

---

## Detalhes que custaram a descobrir

**DTR e RTS têm de ficar desligados.** Se forem activados, o CH340 reinicia o Mega
ao abrir a porta série — e o barramento volta a Hi-Z sem aviso. É por isso que
`Send-PestaCommand` os força a `$false`, tal como o `BusManager.cs` da app.

**A moldura do menu marca o fim da resposta.** Depois de cada comando o firmware
reimprime o menu inteiro. O parser pára na primeira linha `====` ou `----`, e
descarta o eco do carácter. Sem isto, ficava-se à espera do timeout em cada comando.

**A opção 6 precisa de janela larga.** O firmware espera até 3 s pelo `COM_SET` do
Nano antes de sequer começar a transferência SPI. `Test-PestaSpi` usa 8 s. Nota
ainda que essa opção deixa o barramento com o **Mega como master** — chama
`ativarMegaMaster()` internamente. Se for correr o avrdude a seguir, reencaminhe.

**O ficheiro `.psm1` tem de ter BOM UTF-8.** O Windows PowerShell 5.1 lê ficheiros
sem BOM como ANSI e os acentos saem trocados. Se editar o módulo, grave com BOM.

**O Serial Monitor não pode estar aberto.** Só um processo pode ter a COM6. Se
aparecer "Access to the port is denied", feche o Arduino IDE.

---

## Interpretar os fuses

`Get-TargetFuses` devolve `FlashApp`, que é o espaço realmente disponível para a
aplicação. Depende de `BOOTSZ` e `BOOTRST`:

| hfuse típico | Bootloader | FlashApp |
|---|---|---|
| `0xDE` | 512 B (Optiboot) | 32256 |
| `0xDA` | 2048 B (estilo Duemilanove) | 30720 |
| sem BOOTRST | nenhum | 32768 |

A Flash total é sempre 32768 bytes — o bootloader não a reduz, apenas reserva o
topo. Se programar por ISP com chip erase, recupera os 32 KB inteiros (e perde o
bootloader e os lock bits).
