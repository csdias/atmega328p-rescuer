namespace ATMegaPestaV1.Services;

/// <summary>
/// Opções do menu série do firmware Prog_Tester V1.2 (ATMega2560 Pro Mini / MASTER).
/// A assinatura passou a ser a opção 1 e as restantes deslizaram uma posição.
/// </summary>
public static class MenuTester
{
    public const char Assinatura       = '1';
    public const char AtivarUsbAsp     = '2';
    public const char AtivarMegaMaster = '3';
    public const char IsolarBarramento = '4';
    public const char TestarSerial2    = '5';
    public const char TesteSpi         = '6';

    /// <summary>Resposta esperada da opção 1 (<c>exibirAssinatura</c>).</summary>
    public const string AssinaturaEsperada = "ATmega2560_Pro_ON";
}

/// <summary>
/// Resultado da leitura da assinatura do equipamento (opção 1 do menu).
/// </summary>
public record AssinaturaResult(bool Valida, string? Assinatura, string? Resposta);

/// <summary>
/// Representa o BusManager (firmware master no ATMega2560 Pro Mini via CH340).
/// Responsável por identificar o equipamento e por comutar o barramento ISP
/// entre USBAsp e Mega, isolá-lo, ou correr os diagnósticos de Serial2/SPI.
/// </summary>
public interface IBusManager
{
    /// <summary>Envia "1" → devolve a assinatura do equipamento, já validada.</summary>
    Task<AssinaturaResult> VerificarAssinaturaAsync(CancellationToken ct = default);

    /// <summary>Envia "2" → activa o USBAsp no barramento ISP (USBASP_SPI).</summary>
    Task<string?> SelectUsbAspAsync(CancellationToken ct = default);

    /// <summary>Envia "3" → comuta o barramento para o ATMega2560 (uC_MASTER_SPI).</summary>
    Task<string?> SwitchToMegaAsync(CancellationToken ct = default);

    /// <summary>Envia "4" → isola totalmente o barramento em Hi-Z (SPI_CANCEL_ACCESS).</summary>
    Task<string?> IsolarBarramentoAsync(CancellationToken ct = default);

    /// <summary>Envia "5" → testa a ligação Serial2 ao Nano (TEST_SERIAL2_COM).</summary>
    Task<string?> TestarSerial2Async(CancellationToken ct = default);

    /// <summary>Envia "6" → executa a sequência completa de teste SPI (SPI_TEST).</summary>
    Task<string?> ExecutarTesteSpiAsync(CancellationToken ct = default);
}
