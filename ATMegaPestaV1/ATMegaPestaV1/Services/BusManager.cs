using System.IO.Ports;
using System.Text;

namespace ATMegaPestaV1.Services;

/// <summary>
/// Implementação real do BusManager via porta série CH340.
/// Fala com o menu numérico do firmware Prog_Tester V1.2.
/// </summary>
public class BusManager : IBusManager
{
    private readonly string _portaCom;
    private readonly int _baudRate;
    private readonly int _timeoutMs;

    // O teste SPI espera até 3 s pelo COM_SET do Nano e só depois faz a
    // transferência, pelo que precisa de uma janela de leitura maior.
    private const int TimeoutSpiMs = 8000;
    private const int TimeoutSerial2Ms = 3000;

    public BusManager(string portaCom, int baudRate, int timeoutMs)
    {
        _portaCom = portaCom;
        _baudRate = baudRate;
        _timeoutMs = timeoutMs;
    }

    public Task<AssinaturaResult> VerificarAssinaturaAsync(CancellationToken ct = default) =>
        Task.Run(() =>
        {
            var resposta = EnviarComando(MenuTester.Assinatura, _timeoutMs);

            if (resposta is null || resposta.StartsWith("Erro:"))
                return new AssinaturaResult(false, null, resposta);

            var assinatura = resposta
                .Split('\n')
                .Select(l => l.Trim())
                .FirstOrDefault(l => l == MenuTester.AssinaturaEsperada);

            return new AssinaturaResult(assinatura is not null, assinatura, resposta);
        }, ct);

    public Task<string?> SelectUsbAspAsync(CancellationToken ct = default) =>
        EnviarComandoAsync(MenuTester.AtivarUsbAsp, _timeoutMs, ct);

    public Task<string?> SwitchToMegaAsync(CancellationToken ct = default) =>
        EnviarComandoAsync(MenuTester.AtivarMegaMaster, _timeoutMs, ct);

    public Task<string?> IsolarBarramentoAsync(CancellationToken ct = default) =>
        EnviarComandoAsync(MenuTester.IsolarBarramento, _timeoutMs, ct);

    public Task<string?> TestarSerial2Async(CancellationToken ct = default) =>
        EnviarComandoAsync(MenuTester.TestarSerial2, Math.Max(_timeoutMs, TimeoutSerial2Ms), ct);

    public Task<string?> ExecutarTesteSpiAsync(CancellationToken ct = default) =>
        EnviarComandoAsync(MenuTester.TesteSpi, Math.Max(_timeoutMs, TimeoutSpiMs), ct);

    private Task<string?> EnviarComandoAsync(char opcao, int timeoutMs, CancellationToken ct) =>
        Task.Run(() => EnviarComando(opcao, timeoutMs), ct);

    private string? EnviarComando(char opcao, int timeoutMs)
    {
        try
        {
            using var serial = new SerialPort(_portaCom, _baudRate, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = timeoutMs,
                WriteTimeout = _timeoutMs,
                // DTR/RTS desligados para o CH340 não reiniciar o Mega ao abrir a porta.
                DtrEnable = false,
                RtsEnable = false,
                NewLine = "\n"
            };

            serial.Open();

            // Descarta o menu que o firmware possa ter deixado no buffer.
            Thread.Sleep(50);
            serial.DiscardInBuffer();

            // O firmware lê um único carácter; Enter é ignorado do lado dele.
            serial.Write(opcao.ToString());

            var resposta = LerResposta(serial, opcao, timeoutMs);
            serial.Close();
            return resposta;
        }
        catch (Exception ex)
        {
            return $"Erro: {ex.Message}";
        }
    }

    /// <summary>
    /// Lê a resposta a um comando. O firmware faz eco do carácter recebido, imprime
    /// o resultado e volta a mostrar o menu — a moldura do menu marca o fim da resposta.
    /// </summary>
    private static string? LerResposta(SerialPort serial, char opcao, int timeoutMs)
    {
        var sb = new StringBuilder();
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs * 2L);
        var aguardaEco = true;

        while (DateTime.UtcNow < deadline)
        {
            string linha;
            try
            {
                linha = serial.ReadLine().Trim();
            }
            catch (TimeoutException)
            {
                break;
            }

            if (aguardaEco)
            {
                aguardaEco = false;
                if (linha.Length == 1 && linha[0] == opcao)
                    continue;
            }

            // Cabeçalho ("====") ou rodapé ("----") do menu reimpresso: resposta terminada.
            if (linha.StartsWith("====") || linha.StartsWith("----"))
                break;

            if (linha.Length > 0)
                sb.AppendLine(linha);
        }

        return sb.Length > 0 ? sb.ToString().Trim() : null;
    }
}
