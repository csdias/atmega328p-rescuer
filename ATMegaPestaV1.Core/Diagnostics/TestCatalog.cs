namespace ATMegaPestaV1.Diagnostics;

/// <summary>
/// A test in the integrity check and the ATmega328P pins it exercises.
/// </summary>
/// <param name="Name">Test name, as it appears in the results list.</param>
/// <param name="Pins">Pins the test touches — what lights up on the GPIO map.</param>
/// <param name="Tag">
/// Bus the test belongs to (UART, SPI, I2C, PWM), or null for the tests that do not
/// belong to any bus in particular.
/// </param>
public record CatalogTest(string Name, IReadOnlyList<string> Pins, string? Tag = null);

/// <summary>
/// Which pins each integrity check test exercises. It lives in the Core because it is a
/// fact about the ATmega328P and not a presentation choice: the SPI pins are D10-D13 in
/// any front end.
///
/// The catalog says what <em>would</em> be measured. Nothing here runs anything — see
/// <see cref="IntegrityState.NotImplemented"/>.
///
/// The test names stay in Portuguese: they are shown to the student in the results list.
/// </summary>
public static class TestCatalog
{
    /// <summary>ATmega328P pins on the GPIO map, in the order they are presented.</summary>
    public static readonly IReadOnlyList<string> Pins =
    [
        "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7",
        "D8", "D9", "D10", "D11", "D12", "D13",
        "A0", "A1", "A2", "A3", "A4", "A5", "RST"
    ];

    private static readonly string[] AllDigital =
        ["D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7",
         "D8", "D9", "D10", "D11", "D12", "D13"];

    public static readonly IReadOnlyList<CatalogTest> All =
    [
        new("UART",          ["D0", "D1"],                                  "UART"),
        new("GPIO Escrita",  AllDigital),
        new("GPIO Leitura",  AllDigital),
        new("SPI",           ["D10", "D11", "D12", "D13"],                  "SPI"),
        new("I²C",           ["A4", "A5"],                                  "I2C"),
        new("ADC",           ["A0", "A1", "A2", "A3", "A4", "A5"]),
        new("PWM",           ["D3", "D5", "D6", "D9", "D10", "D11"],        "PWM"),
    ];
}

/// <summary>
/// Why the integrity check returns no results.
///
/// The Prog_Tester V1.2 firmware only exposes Serial2 and SPI diagnostics: for GPIO, I²C,
/// ADC and PWM there is no command at all, and without a measurement there is no result.
/// A made-up PASS/FAIL would be indistinguishable at the rig from a real measurement —
/// hence this state existing instead of inventing results.
/// </summary>
public static class IntegrityState
{
    public const string NotImplemented =
        "Verificação de integridade por implementar — o firmware não expõe estes testes. " +
        "Nada foi medido.";
}
