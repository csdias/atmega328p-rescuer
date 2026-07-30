# ATMegaPesta V1 — Copilot Instructions

## Projecto
Aplicação WPF (.NET 10) para recuperação e teste funcional de microcontroladores AVR (ATmega328P, ATmega2560).  
Comunica via **CH340** (UART/USB) com um firmware master (ATmega2560 Pro Mini) que actua como **BusManager**,  
e via **USBAsp** (programador ISP) com o chip-alvo, controlado pelo **avrdude**.

## Arquitectura de serviços
| Interface | Implementação Real | Implementação Simulada |
|---|---|---|
| `IBusManager` | `BusManager` (porta série CH340) | `SimulatedBusManager` |
| `IUsbAspService` | `UsbAspService` (avrdude.exe) | `SimulatedUsbAspService` |

- `SimulationMode: true` em `appsettings.json` → usa implementações simuladas (sem hardware).
- `SimulationMode: false` → usa hardware real.

## Diagrama de sequências — Recover Microcontroller
```
Student → PC               : Recover Microcontroller
PC      → BusManager       : Select USBasp            (comando série "1")
PC      → USBasp           : Attempt recovery (fuses/flash)  (avrdude -c usbasp -p m328p)

alt [Recovery successful]
  PC ← - - - - - - - - - -: Success
  PC → Student             : Recovery complete

alt [Needs deeper recovery]
  PC      → BusManager     : Switch to Mega            (comando série "2")
  PC      → ATmega2560     : Inject clock / assist recovery  (comando série "3")
  ATmega2560 → Chip        : Provide clock
  PC      → USBasp         : Retry ISP                 (avrdude -c usbasp -p m328p)
  USBasp → Chip            : Access restored
  PC ← - - - - - - - - - -: Recovery complete
  PC → Student             : Recovery complete
```

## Comandos série para o BusManager (CH340)
| Comando | Acção |
|---|---|
| `"1"` | Seleccionar USBAsp no barramento ISP |
| `"2"` | Comutar barramento para ATmega2560 (Mega) |
| `"3"` | Injectar clock externo no chip-alvo |

## Convenções de código
- Língua: Português (PT) para UI, mensagens e comentários.
- Estilo: C# idiomático, `record` para DTOs, `async/await`, sem comentários desnecessários.
- Brushes de estado: Verde `#A6E3A1`, Vermelho `#F38BA8`, Amarelo `#F9E2AF`, Cinza `#6C7086`.
- Tema visual: Catppuccin Mocha.
