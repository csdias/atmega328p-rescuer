<#
.SYNOPSIS
    Bancada Pesta — controlo por script do tester ATMega2560 Pro Mini + USBAsp.

.DESCRIPTION
    Faz por linha de comandos exactamente o que se fazia à mão no Serial Monitor:
    envia as opções do menu do firmware Prog_Tester V1.2 pelo CH340, e fala com o
    chip-alvo através do avrdude/USBAsp.

    São DOIS canais físicos distintos:

      PC ──USB──> CH340 ──> ATMega2560 (menu)  ... comuta o barramento ISP
      PC ──USB──> USBAsp ──ISP──> ATmega328P   ... lê/escreve o chip-alvo

    O Mega nunca vê o tráfego do avrdude. A única coisa que o menu faz é decidir
    QUEM fica ligado ao barramento ISP. Por isso a ordem importa sempre:
    encaminhar o barramento primeiro (opção 2), só depois correr o avrdude.

.NOTES
    Importar:  Import-Module C:\Abc\ATMegaPestaV1\ATMegaPestaV1\Tools\PestaBench.psm1 -Force
    Requer Windows PowerShell 5.1+ e avrdude no PATH.
#>

# ─────────────────────────────────────────────────────────────────────────────
#  Constantes do protocolo (espelham Services/IBusManager.cs :: MenuTester)
# ─────────────────────────────────────────────────────────────────────────────

$script:OpcaoAssinatura       = '1'
$script:OpcaoAtivarUsbAsp     = '2'
$script:OpcaoAtivarMegaMaster = '3'
$script:OpcaoIsolarBarramento = '4'
$script:OpcaoTestarSerial2    = '5'
$script:OpcaoTesteSpi         = '6'

$script:AssinaturaEsperada = 'ATmega2560_Pro_ON'
$script:BaudRate           = 9600
$script:PartPorDefeito     = 'm328p'


# ─────────────────────────────────────────────────────────────────────────────
#  Descoberta de hardware
# ─────────────────────────────────────────────────────────────────────────────

function Get-PestaPort {
    <#
    .SYNOPSIS
        Devolve a porta COM do CH340 (ex.: "COM6"). $null se não estiver ligado.
    .EXAMPLE
        Get-PestaPort
    #>
    [CmdletBinding()]
    param()

    $dispositivo = Get-CimInstance Win32_PnPEntity -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match 'CH34' } |
        Select-Object -First 1

    if (-not $dispositivo) {
        Write-Verbose 'CH340 não encontrado.'
        return $null
    }

    if ($dispositivo.Name -match '\((COM\d+)\)') {
        return $Matches[1]
    }

    Write-Verbose "CH340 encontrado mas sem porta COM no nome: $($dispositivo.Name)"
    return $null
}

function Test-PestaHardware {
    <#
    .SYNOPSIS
        Verifica se CH340, USBAsp e avrdude estão disponíveis. Corra isto primeiro.
    .EXAMPLE
        Test-PestaHardware
    #>
    [CmdletBinding()]
    param()

    $porta = Get-PestaPort

    $usbasp = Get-CimInstance Win32_PnPEntity -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match 'USBasp' -or $_.DeviceID -match 'VID_16C0&PID_05DC' } |
        Select-Object -First 1

    $avrdude = Get-Command avrdude.exe -ErrorAction SilentlyContinue

    [PSCustomObject]@{
        CH340        = if ($porta) { $porta } else { 'NAO DETECTADO' }
        USBAsp       = if ($usbasp) { 'ligado' } else { 'NAO DETECTADO' }
        Avrdude      = if ($avrdude) { $avrdude.Source } else { 'NAO ENCONTRADO NO PATH' }
        Pronto       = [bool]($porta -and $usbasp -and $avrdude)
    }
}


# ─────────────────────────────────────────────────────────────────────────────
#  Canal 1 — menu do firmware, via CH340
# ─────────────────────────────────────────────────────────────────────────────

function Send-PestaCommand {
    <#
    .SYNOPSIS
        Envia uma opção do menu (1-6) e devolve a resposta do firmware, já limpa.
    .DESCRIPTION
        Replica a lógica de Services/BusManager.cs :: LerResposta — descarta o eco
        do carácter e pára quando o firmware reimprime a moldura do menu.

        DTR/RTS ficam desligados de propósito: se forem activados, o CH340 reinicia
        o Mega ao abrir a porta e perde-se o estado do barramento.
    .PARAMETER Option
        Carácter da opção: 1 a 6.
    .PARAMETER TimeoutMs
        Janela de leitura. Suba para a opção 6 (o firmware espera até 3 s pelo Nano).
    .EXAMPLE
        Send-PestaCommand -Option 2
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('1','2','3','4','5','6')]
        [string]$Option,

        [string]$Port,
        [int]$TimeoutMs = 2500
    )

    if (-not $Port) { $Port = Get-PestaPort }
    if (-not $Port) { throw 'CH340 não detectado. Ligue o cabo ou indique -Port.' }

    $sp = New-Object System.IO.Ports.SerialPort $Port, $script:BaudRate, 'None', 8, 'One'
    $sp.ReadTimeout  = $TimeoutMs
    $sp.WriteTimeout = 2000
    $sp.DtrEnable    = $false
    $sp.RtsEnable    = $false

    try {
        $sp.Open()
        Start-Sleep -Milliseconds 100
        $sp.DiscardInBuffer()
        $sp.Write($Option)

        $linhas   = New-Object System.Collections.Generic.List[string]
        $limite   = (Get-Date).AddMilliseconds($TimeoutMs * 2)
        $eco      = $true

        while ((Get-Date) -lt $limite) {
            try {
                $linha = $sp.ReadLine().Trim()
            }
            catch [TimeoutException] {
                break
            }

            # O firmware faz eco do carácter recebido antes de responder.
            if ($eco) {
                $eco = $false
                if ($linha -eq $Option) { continue }
            }

            # Moldura do menu reimpresso = fim da resposta.
            if ($linha -like '====*' -or $linha -like '----*') { break }

            if ($linha.Length -gt 0) { $linhas.Add($linha) }
        }

        return ($linhas -join "`n")
    }
    finally {
        if ($sp.IsOpen) { $sp.Close() }
        $sp.Dispose()
    }
}

function Get-PestaSignature {
    <#
    .SYNOPSIS
        Opção 1 — assinatura do TESTER (o Mega), não do chip-alvo.
    .EXAMPLE
        Get-PestaSignature
    #>
    [CmdletBinding()]
    param([string]$Port)

    $resposta = Send-PestaCommand -Option $script:OpcaoAssinatura -Port $Port

    [PSCustomObject]@{
        Valida     = ($resposta -split "`n" | ForEach-Object { $_.Trim() }) -contains $script:AssinaturaEsperada
        Assinatura = $resposta
        Esperada   = $script:AssinaturaEsperada
    }
}

function Set-PestaBus {
    <#
    .SYNOPSIS
        Encaminha o barramento ISP: opções 2 (USBAsp), 3 (Mega master) ou 4 (isolar).
    .PARAMETER Mode
        UsbAsp  — o USBAsp fica ligado ao chip-alvo (necessário antes do avrdude)
        Mega    — o ATMega2560 assume o barramento como master SPI
        Isolate — tudo em Hi-Z (estado seguro de arranque do firmware)
    .EXAMPLE
        Set-PestaBus -Mode UsbAsp
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateSet('UsbAsp','Mega','Isolate')]
        [string]$Mode,

        [string]$Port
    )

    $opcao = switch ($Mode) {
        'UsbAsp'  { $script:OpcaoAtivarUsbAsp }
        'Mega'    { $script:OpcaoAtivarMegaMaster }
        'Isolate' { $script:OpcaoIsolarBarramento }
    }

    Send-PestaCommand -Option $opcao -Port $Port
}

function Test-PestaSerial2 {
    <#
    .SYNOPSIS
        Opção 5 — testa a ligação Serial2 ao Nano.
    #>
    [CmdletBinding()]
    param([string]$Port)
    Send-PestaCommand -Option $script:OpcaoTestarSerial2 -Port $Port -TimeoutMs 3000
}

function Test-PestaSpi {
    <#
    .SYNOPSIS
        Opção 6 — sequência completa de teste SPI contra o Nano.
    .DESCRIPTION
        Precisa de janela larga: o firmware espera até 3 s pelo COM_SET antes de
        sequer começar a transferência. Nota que esta opção deixa o barramento
        com o Mega como master (chama ativarMegaMaster internamente).
    #>
    [CmdletBinding()]
    param([string]$Port)
    Send-PestaCommand -Option $script:OpcaoTesteSpi -Port $Port -TimeoutMs 8000
}


# ─────────────────────────────────────────────────────────────────────────────
#  Canal 2 — chip-alvo, via avrdude/USBAsp
# ─────────────────────────────────────────────────────────────────────────────

function Invoke-Avrdude {
    <#
    .SYNOPSIS
        Corre o avrdude e devolve saída completa + exit code.
    .DESCRIPTION
        O avrdude escreve quase tudo em stderr. Em Windows PowerShell 5.1 o
        redireccionamento 2>&1 sobre um executável nativo embrulha cada linha num
        ErrorRecord e dispara NativeCommandError — daí forçar 'Continue' e converter
        explicitamente para string.
    #>
    param([string[]]$Argumentos)

    $anterior = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $saida = & avrdude.exe @Argumentos 2>&1 |
            ForEach-Object { $_.ToString() } |
            Out-String
        return [PSCustomObject]@{
            ExitCode = $LASTEXITCODE
            Saida    = $saida.Trim()
        }
    }
    finally {
        $ErrorActionPreference = $anterior
    }
}

function Get-TargetSignature {
    <#
    .SYNOPSIS
        Lê a assinatura do chip-alvo. Exige o barramento já em UsbAsp.
    .EXAMPLE
        Set-PestaBus -Mode UsbAsp; Get-TargetSignature
    #>
    [CmdletBinding()]
    param([string]$Part = $script:PartPorDefeito)

    $r     = Invoke-Avrdude @('-c','usbasp','-p',$Part,'-v')
    $saida = $r.Saida
    $ok    = ($r.ExitCode -eq 0)

    $assinatura = $null
    if ($saida -match 'Device signature\s*=\s*([0-9A-Fa-f ]+)') {
        $assinatura = $Matches[1].Trim()
    }

    [PSCustomObject]@{
        Sucesso    = $ok
        Assinatura = $assinatura
        Saida      = $saida
    }
}

function Get-TargetFuses {
    <#
    .SYNOPSIS
        Lê lfuse/hfuse/efuse/lock do chip-alvo e descodifica-os.
    .DESCRIPTION
        Leitura pura — não escreve nada no chip. Exige barramento em UsbAsp.
    .EXAMPLE
        Set-PestaBus -Mode UsbAsp; Get-TargetFuses | Format-List
    #>
    [CmdletBinding()]
    param([string]$Part = $script:PartPorDefeito)

    $r = Invoke-Avrdude @('-c','usbasp','-p',$Part,
                          '-U','lfuse:r:-:h','-U','hfuse:r:-:h',
                          '-U','efuse:r:-:h','-U','lock:r:-:h')
    $saida = $r.Saida

    $valores = @([regex]::Matches($saida, '0x[0-9a-fA-F]{2}') | ForEach-Object { $_.Value })

    if ($r.ExitCode -ne 0 -or $valores.Count -lt 4) {
        return [PSCustomObject]@{
            Sucesso = $false
            Saida   = $saida
        }
    }

    $lfuse = [Convert]::ToInt32($valores[0], 16)
    $hfuse = [Convert]::ToInt32($valores[1], 16)
    $efuse = [Convert]::ToInt32($valores[2], 16)
    $lock  = [Convert]::ToInt32($valores[3], 16)

    $bootBytes = switch (($hfuse -shr 1) -band 0x03) {
        3 { 512 }
        2 { 1024 }
        1 { 2048 }
        0 { 4096 }
    }
    $bootrstActivo = (($hfuse -band 0x01) -eq 0)

    [PSCustomObject]@{
        Sucesso        = $true
        LFuse          = $valores[0]
        HFuse          = $valores[1]
        EFuse          = $valores[2]
        Lock           = $valores[3]

        Relogio        = Get-CkselDescricao -Lfuse $lfuse
        Ckdiv8         = if (($lfuse -band 0x80) -eq 0) { 'ACTIVO (clock /8)' } else { 'desligado' }
        BrownOut       = switch ($efuse -band 0x07) {
                             7 { 'desactivado' }
                             6 { '1.8 V' }
                             5 { '2.7 V' }
                             4 { '4.3 V' }
                             default { 'reservado' }
                         }
        SpiEnabled     = (($hfuse -band 0x20) -eq 0)
        ResetActivo    = (($hfuse -band 0x80) -ne 0)
        EepromPreservada = (($hfuse -band 0x08) -eq 0)
        BootloaderBytes = $bootBytes
        BootRst        = $bootrstActivo
        FlashApp       = if ($bootrstActivo) { 32768 - $bootBytes } else { 32768 }
        Bloqueio       = Get-LockDescricao -Lock $lock
        Saida          = $saida
    }
}

function Get-CkselDescricao {
    param([int]$Lfuse)

    $cksel = $Lfuse -band 0x0F

    switch ($cksel) {
        0  { return 'clock externo' }
        1  { return 'reservado' }
        2  { return 'RC interno 8 MHz' }
        3  { return 'RC interno 128 kHz' }
        { $_ -in 4,5 } { return 'cristal baixa frequência (32.768 kHz)' }
        { $_ -in 6,7 } { return 'cristal full swing' }
        default {
            $gama = switch (($cksel -shr 1) -band 0x07) {
                4 { '0.4-0.9 MHz' }
                5 { '0.9-3.0 MHz' }
                6 { '3.0-8.0 MHz' }
                7 { '8.0-16.0 MHz' }
                default { 'desconhecida' }
            }
            return "cristal externo baixa potência, $gama"
        }
    }
}

function Get-LockDescricao {
    param([int]$Lock)

    $lb = $Lock -band 0x03

    switch ($lb) {
        3 { 'sem bloqueio — leitura e programação livres' }
        2 { 'modo 2 — programação bloqueada, verificação permitida' }
        0 { 'modo 3 — programação E verificação bloqueadas' }
        default { "invulgar (LB=$lb)" }
    }
}


# ─────────────────────────────────────────────────────────────────────────────
#  Sequência completa
# ─────────────────────────────────────────────────────────────────────────────

function Read-TargetInfo {
    <#
    .SYNOPSIS
        Diagnóstico completo e seguro de um chip no ZIF: encaminha, lê, e volta a isolar.
    .DESCRIPTION
        É a sequência que substitui o trabalho manual no Serial Monitor:
          1. confirma hardware
          2. valida a assinatura do tester (opção 1)
          3. encaminha o barramento para o USBAsp (opção 2)
          4. lê assinatura e fuses do alvo por avrdude
          5. isola o barramento outra vez (opção 4) — mesmo se algo falhar

        Só faz leituras. Nunca escreve no chip.
    .EXAMPLE
        Read-TargetInfo
    .EXAMPLE
        Read-TargetInfo -KeepBusConnected   # deixa ligado para continuar a trabalhar
    #>
    [CmdletBinding()]
    param(
        [string]$Part = $script:PartPorDefeito,
        [switch]$KeepBusConnected
    )

    $hw = Test-PestaHardware
    if (-not $hw.Pronto) {
        Write-Warning 'Hardware incompleto:'
        $hw | Format-List | Out-String | Write-Host
        return
    }

    Write-Host "CH340 em $($hw.CH340), USBAsp ligado." -ForegroundColor DarkGray

    $tester = Get-PestaSignature
    if (-not $tester.Valida) {
        Write-Warning "Assinatura do tester inesperada. Recebido: $($tester.Assinatura)"
    }
    else {
        Write-Host "Tester identificado: $($script:AssinaturaEsperada)" -ForegroundColor DarkGray
    }

    try {
        $modo = Set-PestaBus -Mode UsbAsp
        Write-Host "Barramento: $modo" -ForegroundColor DarkGray

        $assinatura = Get-TargetSignature -Part $Part
        if (-not $assinatura.Sucesso) {
            Write-Warning 'O chip-alvo não respondeu ao ISP.'
            Write-Host $assinatura.Saida
            return
        }

        Write-Host "Assinatura do alvo: $($assinatura.Assinatura)" -ForegroundColor Green

        $fuses = Get-TargetFuses -Part $Part
        if (-not $fuses.Sucesso) {
            Write-Warning 'Falha na leitura dos fuses.'
            return
        }

        $fuses | Select-Object LFuse, HFuse, EFuse, Lock, Relogio, Ckdiv8, BrownOut,
                               BootloaderBytes, FlashApp, Bloqueio |
            Format-List | Out-String | Write-Host

        return $fuses
    }
    finally {
        if ($KeepBusConnected) {
            Write-Host 'Barramento deixado ligado ao USBAsp.' -ForegroundColor Yellow
        }
        else {
            $null = Set-PestaBus -Mode Isolate
            Write-Host 'Barramento reposto em Hi-Z.' -ForegroundColor DarkGray
        }
    }
}


Export-ModuleMember -Function Get-PestaPort, Test-PestaHardware, Send-PestaCommand,
                              Get-PestaSignature, Set-PestaBus, Test-PestaSerial2,
                              Test-PestaSpi, Get-TargetSignature, Get-TargetFuses,
                              Read-TargetInfo
