using System.IO.Ports;
using System.Text;

namespace ATMegaPestaV1.Services;

/// <summary>
/// Real BusManager implementation over the CH340 serial port.
/// Talks to the numeric menu of the Prog_Tester V1.2 firmware.
/// </summary>
public class BusManager : IBusManager
{
    private readonly string _comPort;
    private readonly int _baudRate;
    private readonly int _timeoutMs;

    // The SPI test waits up to 3 s for the Nano's COM_SET and only then does the
    // transfer, so it needs a wider read window.
    private const int SpiTimeoutMs = 8000;
    private const int Serial2TimeoutMs = 3000;

    public BusManager(string comPort, int baudRate, int timeoutMs)
    {
        _comPort = comPort;
        _baudRate = baudRate;
        _timeoutMs = timeoutMs;
    }

    public Task<SignatureResult> VerifySignatureAsync(CancellationToken ct = default) =>
        Task.Run(() =>
        {
            var response = SendCommand(MenuTester.Signature, _timeoutMs);

            if (response is null || response.StartsWith("Erro:"))
                return new SignatureResult(false, null, response);

            var signature = response
                .Split('\n')
                .Select(l => l.Trim())
                .FirstOrDefault(l => l == MenuTester.ExpectedSignature);

            return new SignatureResult(signature is not null, signature, response);
        }, ct);

    public Task<string?> SelectUsbAspAsync(CancellationToken ct = default) =>
        SendCommandAsync(MenuTester.EnableUsbAsp, _timeoutMs, ct);

    public Task<string?> SwitchToMegaAsync(CancellationToken ct = default) =>
        SendCommandAsync(MenuTester.EnableMegaMaster, _timeoutMs, ct);

    public Task<string?> IsolateBusAsync(CancellationToken ct = default) =>
        SendCommandAsync(MenuTester.IsolateBus, _timeoutMs, ct);

    public Task<string?> TestSerial2Async(CancellationToken ct = default) =>
        SendCommandAsync(MenuTester.TestSerial2, Math.Max(_timeoutMs, Serial2TimeoutMs), ct);

    public Task<string?> RunSpiTestAsync(CancellationToken ct = default) =>
        SendCommandAsync(MenuTester.SpiTest, Math.Max(_timeoutMs, SpiTimeoutMs), ct);

    private Task<string?> SendCommandAsync(char option, int timeoutMs, CancellationToken ct) =>
        Task.Run(() => SendCommand(option, timeoutMs), ct);

    private string? SendCommand(char option, int timeoutMs)
    {
        try
        {
            using var serial = new SerialPort(_comPort, _baudRate, Parity.None, 8, StopBits.One)
            {
                ReadTimeout = timeoutMs,
                WriteTimeout = _timeoutMs,
                // DTR/RTS off so the CH340 does not reset the Mega when the port opens.
                DtrEnable = false,
                RtsEnable = false,
                NewLine = "\n"
            };

            serial.Open();

            // Discard whatever menu the firmware may have left in the buffer.
            Thread.Sleep(50);
            serial.DiscardInBuffer();

            // The firmware reads a single character; Enter is ignored on its side.
            serial.Write(option.ToString());

            var response = ReadResponse(serial, option, timeoutMs);
            serial.Close();
            return response;
        }
        catch (Exception ex)
        {
            return $"Erro: {ex.Message}";
        }
    }

    /// <summary>
    /// Reads the answer to a command. The firmware echoes the character it received,
    /// prints the result and then shows the menu again — the menu's frame marks the end
    /// of the answer.
    /// </summary>
    private static string? ReadResponse(SerialPort serial, char option, int timeoutMs)
    {
        var sb = new StringBuilder();
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs * 2L);
        var awaitingEcho = true;

        while (DateTime.UtcNow < deadline)
        {
            string line;
            try
            {
                line = serial.ReadLine().Trim();
            }
            catch (TimeoutException)
            {
                break;
            }

            if (awaitingEcho)
            {
                awaitingEcho = false;
                if (line.Length == 1 && line[0] == option)
                    continue;
            }

            // Header ("====") or footer ("----") of the reprinted menu: answer is over.
            if (line.StartsWith("====") || line.StartsWith("----"))
                break;

            if (line.Length > 0)
                sb.AppendLine(line);
        }

        return sb.Length > 0 ? sb.ToString().Trim() : null;
    }
}
