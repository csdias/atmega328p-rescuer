using ATMegaPestaV1.Copias;
using ATMegaPestaV1.Diagnosticos;
using ATMegaPestaV1.Services;
using Microsoft.AspNetCore.SignalR;

namespace ATMegaPestaV1.Api.Bancada;

/// <summary>
/// A bancada vista pela API. Faz o que o code-behind do WPF fazia — varre o USB, encaminha
/// o barramento, lê o chip-alvo, guarda a cópia — e reporta o mesmo que ele reportava.
///
/// É singleton e serializa tudo num semáforo porque a bancada é um recurso físico único:
/// uma porta série, um USBAsp, um barramento. Dois pedidos HTTP a comutar o barramento ao
/// mesmo tempo deixariam o alvo ligado a dois mestres, e o avrdude a correr duas vezes em
/// paralelo falha as duas. Quem chega a meio de uma operação espera pela sua vez.
///
/// O estado que guarda entre pedidos é o mesmo que a janela guardava entre cliques: a porta
/// onde o CH340 apareceu, as tentativas gastas, e o que a última leitura identificou (de que
/// a ficha da cópia precisa).
/// </summary>
public class BancadaService
{
    /// <summary>Falhas de leitura por ISP toleradas antes de propor a alta tensão.</summary>
    private const int MaxTentativasLeitura = 3;

    private readonly IServiceFactory _servicos;
    private readonly IDeviceDetector _detector;
    private readonly IHubContext<BancadaHub> _hub;
    private readonly ILogger<BancadaService> _log;

    private readonly int _maxTentativas;
    private readonly bool _verificarAssinatura;
    private readonly string _pastaCopias;

    private readonly SemaphoreSlim _acesso = new(1, 1);

    private int _tentativaDeteccao;
    private bool _deteccaoConcluida;
    private string? _portaCom;

    private int _tentativasLeitura;
    private McuInfo? _mcuLido;
    private string? _assinaturaLida;

    public BancadaService(IConfiguration config, IHubContext<BancadaHub> hub, ILogger<BancadaService> log)
    {
        _hub = hub;
        _log = log;

        _servicos = ServiceFactory.APartirDe(config);
        _detector = _servicos.CriarDetector();

        var max = config.GetValue<int>("MaxTentativas");
        _maxTentativas = max > 0 ? max : 3;

        // Equipamentos com firmware antigo não respondem à opção 1 — daí a
        // possibilidade de desligar a verificação, como no WPF.
        _verificarAssinatura = config.GetValue("VerificarAssinatura", true);

        _pastaCopias = ResolverPastaCopias(config);
    }

    public ConfigResponse Config => new(
        _maxTentativas, _verificarAssinatura, MaxTentativasLeitura, _pastaCopias);

    public CatalogoResponse Catalogo => new(
        CatalogoTestes.Pinos, CatalogoTestes.Todos, EstadoIntegridade.PorImplementar);

    /// <summary>
    /// Onde ficam as cópias. O browser não escolhe pastas do servidor, por isso a pasta é
    /// configuração da API — cada cópia leva a sua subpasta com o carimbo da hora.
    /// </summary>
    private static string ResolverPastaCopias(IConfiguration config)
    {
        var configurada = config.GetValue<string>("PastaCopias");

        if (!string.IsNullOrWhiteSpace(configurada))
            return Path.GetFullPath(configurada);

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "ATMegaPesta", "Copias");
    }

    // ── Verificação de dispositivos ─────────────────────────────────────────

    /// <summary>
    /// Varre o USB e pede a assinatura ao equipamento. Cada chamada é uma tentativa,
    /// como cada clique no botão do WPF — excepto depois de a bancada já estar completa,
    /// onde uma nova chamada só reconfirma o que está ligado (o front end pode recarregar
    /// a página) e não gasta tentativas.
    /// </summary>
    public async Task<DeteccaoResponse> DetectarAsync(CancellationToken ct)
    {
        await _acesso.WaitAsync(ct);
        try
        {
            // Tentativas gastas: o front end já mostrou o encerramento e não há nada a
            // reavaliar até alguém reiniciar o ciclo.
            if (!_deteccaoConcluida && _tentativaDeteccao >= _maxTentativas)
                return Esgotada();

            if (!_deteccaoConcluida)
                _tentativaDeteccao++;

            await ProgressoAsync("A verificar dispositivos...", EstadoLed.Inactivo);
            var dispositivos = await _detector.DetectarAsync(ct);

            var ch340 = dispositivos.PortaCH340 is { } porta
                ? new Indicador(EstadoLed.Ok, $"Conectado na porta {porta}")
                : new Indicador(EstadoLed.Erro, "Não detectado — Ligue o conversor USB-Serial CH340");

            var usbAsp = dispositivos.UsbAspLigado
                ? new Indicador(EstadoLed.Ok, "Conectado")
                : new Indicador(EstadoLed.Erro, "Não detectado — Ligue o programador USBAsp");

            var (assinatura, assinaturaOk) =
                await VerificarAssinaturaAsync(dispositivos.PortaCH340, ct);

            if (dispositivos.Completa && assinaturaOk)
            {
                _deteccaoConcluida = true;
                _portaCom = dispositivos.PortaCH340;

                return new DeteccaoResponse(ch340, usbAsp, assinatura, _portaCom,
                    PodeAvancar: true, _tentativaDeteccao, _maxTentativas, Esgotado: false,
                    "Bancada pronta.", EstadoLed.Ok);
            }

            if (_tentativaDeteccao >= _maxTentativas)
                return Esgotada(ch340, usbAsp, assinatura);

            return new DeteccaoResponse(ch340, usbAsp, assinatura, dispositivos.PortaCH340,
                PodeAvancar: false, _tentativaDeteccao, _maxTentativas, Esgotado: false,
                "Dispositivo(s) em falta. Por favor, ligue o(s) dispositivo(s) e tente novamente.",
                EstadoLed.Aviso);
        }
        finally
        {
            _acesso.Release();
        }
    }

    private DeteccaoResponse Esgotada(
        Indicador? ch340 = null, Indicador? usbAsp = null, Indicador? assinatura = null) =>
        new(ch340 ?? new Indicador(EstadoLed.Erro, "Não detectado"),
            usbAsp ?? new Indicador(EstadoLed.Erro, "Não detectado"),
            assinatura ?? new Indicador(EstadoLed.Inactivo, "A aguardar detecção do CH340..."),
            null, PodeAvancar: false, _tentativaDeteccao, _maxTentativas, Esgotado: true,
            "Não foi possível detectar todos os dispositivos após várias tentativas.\n" +
            "Por favor, contacte a assistência técnica.",
            EstadoLed.Erro);

    /// <summary>
    /// Pede a assinatura ao equipamento (opção 1 do menu do firmware). Sem CH340 não há
    /// por onde perguntar, e com a verificação desligada não se pergunta.
    /// </summary>
    private async Task<(Indicador, bool)> VerificarAssinaturaAsync(string? porta, CancellationToken ct)
    {
        if (!_verificarAssinatura)
            return (new Indicador(EstadoLed.Inactivo, "Verificação de assinatura ignorada"), true);

        if (porta is null)
            return (new Indicador(EstadoLed.Inactivo, "A aguardar detecção do CH340..."), false);

        await ProgressoAsync("A verificar a assinatura do equipamento...", EstadoLed.Inactivo);
        var resultado = await _servicos.CriarBusManager(porta).VerificarAssinaturaAsync(ct);

        if (resultado.Valida)
            return (new Indicador(EstadoLed.Ok,
                $"Equipamento identificado: {resultado.Assinatura} na porta {porta}"), true);

        var detalhe = resultado.Resposta is { Length: > 0 } resposta
            ? $"Assinatura inesperada em {porta} — recebido: {PrimeiraLinha(resposta)}"
            : $"Sem resposta do equipamento em {porta} — Verifique a ligação";

        return (new Indicador(EstadoLed.Erro, detalhe), false);
    }

    private static string PrimeiraLinha(string texto) => texto.Split('\n')[0].Trim();

    /// <summary>
    /// Devolve o ciclo ao início: tentativas a zero e leitura anterior descartada. É o que
    /// se faz ao trocar de peça no ZIF, e o que o WPF conseguia relançando a aplicação.
    /// </summary>
    public async Task ReiniciarAsync(CancellationToken ct)
    {
        await _acesso.WaitAsync(ct);
        try
        {
            _tentativaDeteccao = 0;
            _deteccaoConcluida = false;
            _portaCom = null;
            _tentativasLeitura = 0;
            _mcuLido = null;
            _assinaturaLida = null;
        }
        finally
        {
            _acesso.Release();
        }
    }

    // ── Leitura das configurações do chip-alvo ──────────────────────────────

    /// <summary>
    /// Encaminha o barramento para o USBAsp, lê a identificação e os fuses do chip-alvo, e
    /// volta a isolar o barramento. Estritamente leitura — nada nesta API grava fuses.
    ///
    /// Ao fim de <see cref="MaxTentativasLeitura"/> falhas seguidas, o ISP está esgotado
    /// como via de acesso: a resposta vem com <c>Esgotado</c> e o front end propõe a
    /// programação de alta tensão.
    /// </summary>
    public async Task<LeituraResponse> LerConfiguracoesAsync(CancellationToken ct)
    {
        await _acesso.WaitAsync(ct);
        try
        {
            // Uma leitura nova invalida a anterior: quem trocou a peça no ZIF não deve
            // continuar a ver a verificação destrancada pela leitura de outro chip.
            _mcuLido = null;
            _assinaturaLida = null;

            var leitura = await TentarLerAsync(ct);

            if (leitura.Identificado)
            {
                _tentativasLeitura = 0;
                return leitura with { Tentativa = 0, MaxTentativas = MaxTentativasLeitura };
            }

            _tentativasLeitura++;
            var esgotou = _tentativasLeitura >= MaxTentativasLeitura;

            return leitura with
            {
                Instrucao = esgotou
                    ? "O chip-alvo continuou sem responder ao ISP nas três tentativas."
                    : "Verifique se o microcontrolador está corretamente inserido no ZIF socket — " +
                      "alavanca baixada e pino 1 no canto marcado — e tente detetar novamente.",
                Tentativas = esgotou
                    ? $"{MaxTentativasLeitura} de {MaxTentativasLeitura} tentativas sem sucesso."
                    : $"Tentativa {_tentativasLeitura} de {MaxTentativasLeitura}",
                Tentativa = _tentativasLeitura,
                MaxTentativas = MaxTentativasLeitura,
                Esgotado = esgotou
            };
        }
        finally
        {
            _acesso.Release();
        }
    }

    private async Task<LeituraResponse> TentarLerAsync(CancellationToken ct)
    {
        var barramentoActivo = false;

        try
        {
            // 1a — encaminhar o barramento ISP para o USBAsp (opção 2 do menu).
            await ProgressoAsync("A activar o USBAsp no barramento...", EstadoLed.Aviso);
            var resBus = await _servicos.CriarBusManager(_portaCom ?? "").SelectUsbAspAsync(ct);

            // Sem barramento não se chegou a ligar nada ao alvo — nada para isolar.
            if (ErroDeBarramento(resBus))
                return Falha(resBus!, barramentoIsolado: true);

            barramentoActivo = true;

            // 1b — identificar o chip-alvo via ISP.
            await ProgressoAsync("Barramento no USBAsp — a ler o chip-alvo...", EstadoLed.Aviso);
            var usbAsp = _servicos.CriarUsbAspService();
            var detect = await usbAsp.DetectarAssinaturaAsync(ct);

            if (!detect.Success)
            {
                // A saída crua do avrdude não vai para a mensagem: para quem está à
                // bancada é ruído. Segue à parte, para quem quiser abrir o log.
                var isolado = await IsolarAsync(ct);
                barramentoActivo = false;

                return Falha("O chip-alvo não respondeu ao ISP.", isolado, detect.Output);
            }

            var mcu = FuseDecoder.Identificar(detect.Dispositivo);

            // 1c — ler e descodificar os fuses.
            await ProgressoAsync($"{mcu.Nome} detetado — a ler os fuse bits...", EstadoLed.Aviso);
            var fuses = await usbAsp.LerFusesAsync(ct);
            var decodificado = FuseDecoder.Descodificar(fuses, mcu);

            var isoladoFinal = await IsolarAsync(ct);
            barramentoActivo = false;

            if (decodificado is null)
            {
                // O chip respondeu — a via de acesso não está em causa, por isso isto não
                // conta como falha de leitura nem consome tentativas.
                return new LeituraResponse(
                    Identificado: true, ConfiguracoesLidas: false, Mapear(mcu),
                    detect.Assinatura, Mapear(fuses), null, null,
                    $"{mcu.Nome} detetado, mas a leitura dos fuses falhou.", EstadoLed.Aviso,
                    null, null, 0, MaxTentativasLeitura, Esgotado: false, isoladoFinal, fuses.Output);
            }

            _mcuLido = mcu;
            _assinaturaLida = detect.Assinatura;

            // Resumo denso: é a única coisa visível com o acordeão fechado.
            var resumo = $"{mcu.Nome} · {detect.Assinatura} · " +
                         $"L {fuses.Low}  H {fuses.High}  E {fuses.Extended}  LB {fuses.Lock ?? "n/d"}";

            return new LeituraResponse(
                Identificado: true, ConfiguracoesLidas: true, Mapear(mcu),
                detect.Assinatura, Mapear(fuses), Mapear(decodificado),
                MapearFlash(mcu, decodificado),
                resumo, EstadoLed.Ok, null, null,
                0, MaxTentativasLeitura, Esgotado: false, isoladoFinal, null);
        }
        finally
        {
            // Corre quando algo rebenta a meio: não se deixa o ISP ligado ao alvo.
            if (barramentoActivo)
                await IsolarAsync(ct);
        }
    }

    private static LeituraResponse Falha(string estado, bool barramentoIsolado,
                                         string? saidaAvrdude = null) =>
        new(Identificado: false, ConfiguracoesLidas: false, null, null, null, null, null,
            estado, EstadoLed.Erro, null, null, 0, MaxTentativasLeitura,
            Esgotado: false, barramentoIsolado, saidaAvrdude);

    // ── Cópia de segurança ──────────────────────────────────────────────────

    /// <summary>
    /// Guarda a Flash, a EEPROM e a ficha dos fuses do chip-alvo numa subpasta com o
    /// carimbo da hora. Encaminha o barramento para o USBAsp e volta a isolá-lo, como
    /// qualquer acesso ISP.
    /// </summary>
    public async Task<CopiaResponse> GuardarCopiaAsync(CancellationToken ct)
    {
        await _acesso.WaitAsync(ct);
        try
        {
            return await GuardarCopiaInternaAsync(ct);
        }
        finally
        {
            _acesso.Release();
        }
    }

    private async Task<CopiaResponse> GuardarCopiaInternaAsync(CancellationToken ct)
    {
        // Carimbo na hora: quem traz várias peças à bancada não fica com cópias a
        // sobrepor-se umas às outras sem se perceber qual é qual.
        var carimbo = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var pasta = Path.Combine(_pastaCopias, carimbo);
        Directory.CreateDirectory(pasta);

        var flash  = Path.Combine(pasta, $"ATmega328P_{carimbo}_flash.hex");
        var eeprom = Path.Combine(pasta, $"ATmega328P_{carimbo}_eeprom.hex");
        var ficha  = Path.Combine(pasta, $"ATmega328P_{carimbo}_fuses.txt");

        await ProgressoAsync("A guardar a Flash, a EEPROM e os fuses do chip-alvo...", EstadoLed.Aviso);

        var barramentoActivo = false;

        try
        {
            var resBus = await _servicos.CriarBusManager(_portaCom ?? "").SelectUsbAspAsync(ct);

            if (ErroDeBarramento(resBus))
                return new CopiaResponse(false, carimbo, pasta, [], null, resBus!,
                    EstadoLed.Erro, null, BarramentoIsolado: true);

            barramentoActivo = true;

            var copia = await _servicos.CriarUsbAspService().GuardarCopiaAsync(flash, eeprom, ct);

            if (!copia.Success)
            {
                var isoladoFalha = await IsolarAsync(ct);
                barramentoActivo = false;

                return new CopiaResponse(false, carimbo, pasta, [], Mapear(copia.Fuses),
                    "A cópia falhou — a verificação não avançou e o chip fica como está.",
                    EstadoLed.Erro, copia.Output, isoladoFalha);
            }

            // A ficha é escrita aqui, e não no serviço: os bytes vêm do avrdude, mas o que
            // eles significam é o FuseDecoder que sabe.
            try
            {
                var texto = FichaFuses.Construir(
                    _mcuLido ?? FuseDecoder.PorDefeito, _assinaturaLida, copia, DateTime.Now);

                await File.WriteAllTextAsync(ficha, texto, System.Text.Encoding.UTF8, ct);
            }
            catch (Exception ex)
            {
                var isoladoFicha = await IsolarAsync(ct);
                barramentoActivo = false;

                return new CopiaResponse(false, carimbo, pasta, Listar(carimbo, flash, eeprom),
                    Mapear(copia.Fuses),
                    $"As memórias foram guardadas mas a ficha dos fuses falhou: {ex.Message}",
                    EstadoLed.Erro, copia.Output, isoladoFicha);
            }

            var isolado = await IsolarAsync(ct);
            barramentoActivo = false;

            var f = copia.Fuses;
            return new CopiaResponse(true, carimbo, pasta,
                Listar(carimbo, flash, eeprom, ficha), Mapear(f),
                $"Cópia guardada em {pasta} — Flash, EEPROM e fuses " +
                $"(L {f.Low}  H {f.High}  E {f.Extended}  LB {f.Lock ?? "n/d"})",
                EstadoLed.Ok, copia.Output, isolado);
        }
        finally
        {
            if (barramentoActivo)
                await IsolarAsync(ct);
        }
    }

    private static IReadOnlyList<FicheiroCopia> Listar(string carimbo, params string[] caminhos) =>
        [.. caminhos
            .Select(c => new FileInfo(c))
            .Where(fi => fi.Exists)
            .Select(fi => new FicheiroCopia(
                fi.Name, $"/api/alvo/copias/{carimbo}/{Uri.EscapeDataString(fi.Name)}", fi.Length))];

    /// <summary>
    /// Abre um ficheiro de uma cópia para descarga. O nome é validado contra o que a cópia
    /// realmente escreveu — um caminho vindo do cliente não se junta a uma pasta sem mais.
    /// </summary>
    public FileInfo? LocalizarFicheiroCopia(string carimbo, string nome)
    {
        // Só o carimbo que esta API gera: dígitos e um underscore, nada de separadores.
        if (!System.Text.RegularExpressions.Regex.IsMatch(carimbo, @"^\d{8}_\d{6}$"))
            return null;

        if (nome != Path.GetFileName(nome))
            return null;

        var pasta = Path.Combine(_pastaCopias, carimbo);
        var ficheiro = new FileInfo(Path.Combine(pasta, nome));

        // Depois de resolvido, tem de continuar dentro da pasta das cópias.
        if (!ficheiro.FullName.StartsWith(Path.GetFullPath(_pastaCopias), StringComparison.OrdinalIgnoreCase))
            return null;

        return ficheiro.Exists ? ficheiro : null;
    }

    // ── Verificação de integridade ──────────────────────────────────────────

    /// <summary>
    /// Comuta o barramento para o ATmega2560 e devolve a lista de testes — pendentes.
    ///
    /// A comutação e o isolamento acontecem de facto; a medição é que não existe. O
    /// firmware Prog_Tester V1.2 só expõe diagnósticos de Serial2 e de SPI, e um PASS/FAIL
    /// sorteado seria indistinguível à bancada de uma medição verdadeira.
    /// </summary>
    public async Task<IntegridadeResponse> ExecutarIntegridadeAsync(
        IntegridadeRequest pedido, CancellationToken ct)
    {
        await _acesso.WaitAsync(ct);
        try
        {
            CopiaResponse? copia = null;

            // Cópia pedida e não obtida trava a verificação: prosseguir seria escrever
            // sobre o chip exactamente depois de alguém dizer que o queria guardar.
            if (pedido.Escolha == EscolhaTransferencia.ProsseguirComCopia)
            {
                copia = await GuardarCopiaInternaAsync(ct);

                if (!copia.Sucesso)
                    return new IntegridadeResponse(false, copia.BarramentoIsolado, copia,
                        Pendentes(), 0, copia.Mensagem, EstadoLed.Erro);
            }

            await ProgressoAsync("A comutar o barramento para o ATmega2560...", EstadoLed.Aviso);
            var resMega = await _servicos.CriarBusManager(_portaCom ?? "").SwitchToMegaAsync(ct);

            if (ErroDeBarramento(resMega))
                return new IntegridadeResponse(false, await IsolarAsync(ct), copia,
                    Pendentes(), 0, resMega!, EstadoLed.Erro);

            await ProgressoAsync(EstadoIntegridade.PorImplementar, EstadoLed.Aviso);

            // Nada foi medido, logo não há razão para deixar o master a conduzir o
            // barramento: devolve-se a Hi-Z, o estado de arranque do firmware.
            var isolado = await IsolarAsync(ct);

            var mensagem = isolado
                ? EstadoIntegridade.PorImplementar
                : EstadoIntegridade.PorImplementar + "  ·  ATENÇÃO: falha ao isolar o barramento";

            return new IntegridadeResponse(BarramentoComutado: true, isolado, copia,
                Pendentes(), 0, mensagem, EstadoLed.Aviso);
        }
        finally
        {
            _acesso.Release();
        }
    }

    private static IReadOnlyList<ResultadoTesteDto> Pendentes() =>
        [.. CatalogoTestes.Todos.Select(t => new ResultadoTesteDto(t.Nome, EstadoTeste.Pendente, "n/d"))];

    // ── Barramento ──────────────────────────────────────────────────────────

    private static bool ErroDeBarramento(string? resposta) =>
        resposta is not null && resposta.StartsWith("Erro:");

    /// <summary>
    /// Devolve o barramento a Hi-Z. Isolar é arrumação de rotina: quando corre bem não se
    /// anuncia. Quando falha devolve false — deixar o alvo ligado sem ninguém saber seria
    /// pior que o erro — e quem chamou avisa na sua própria resposta.
    /// </summary>
    private async Task<bool> IsolarAsync(CancellationToken ct)
    {
        try
        {
            var resposta = await _servicos.CriarBusManager(_portaCom ?? "").IsolarBarramentoAsync(ct);
            var isolado = !ErroDeBarramento(resposta);

            if (!isolado)
                _log.LogWarning("Falha ao isolar o barramento: {Resposta}", resposta);

            return isolado;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Excepção ao isolar o barramento");
            return false;
        }
    }

    private Task ProgressoAsync(string texto, EstadoLed severidade) =>
        _hub.Clients.All.SendAsync(BancadaHub.EventoProgresso, new Progresso(texto, severidade));

    // ── Mapeamento para os contratos ────────────────────────────────────────

    private static McuDto Mapear(McuInfo mcu) => new(
        mcu.Nome, mcu.FlashBytes, mcu.EepromBytes, mcu.SramBytes,
        FusesDecodificados.FormatarBytes(mcu.FlashBytes),
        FusesDecodificados.FormatarBytes(mcu.EepromBytes),
        FusesDecodificados.FormatarBytes(mcu.SramBytes));

    private static FusesDto Mapear(FusesResult f) => new(f.Low, f.High, f.Extended, f.Lock);

    private static FusesDecodificadosDto Mapear(FusesDecodificados d) => new(
        d.Relogio, d.Ckdiv8Activo, d.BrownOut, d.SpiActivo, d.ResetActivo,
        d.EepromPreservada, d.BootRstActivo, d.Bloqueio, d.LeituraLivre,
        d.DescricaoLow, d.DescricaoHigh, d.DescricaoExtended, d.DescricaoLock);

    private static MapaFlashDto MapearFlash(McuInfo mcu, FusesDecodificados d) => new(
        mcu.FlashBytes,
        d.BootRstActivo ? d.BootloaderBytes : 0,
        d.FlashAplicacao,
        FusesDecodificados.FormatarBytes(d.BootloaderBytes),
        FusesDecodificados.FormatarBytes(d.FlashAplicacao),
        d.BootRstActivo);
}
