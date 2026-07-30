namespace ATMegaPestaV1.Diagnosticos;

/// <summary>
/// Um teste da verificação de integridade e os pinos do ATmega328P que exercita.
/// </summary>
/// <param name="Nome">Nome do teste, como aparece na lista de resultados.</param>
/// <param name="Pinos">Pinos que o teste toca — o que se ilumina no mapa GPIO.</param>
/// <param name="Tag">
/// Barramento a que o teste pertence (UART, SPI, I2C, PWM), ou null para os testes
/// que não são de um barramento em particular.
/// </param>
public record TesteCatalogo(string Nome, IReadOnlyList<string> Pinos, string? Tag = null);

/// <summary>
/// Que pinos cada teste da verificação de integridade exercita. Vive no Core porque é
/// um facto do ATmega328P e não uma escolha de apresentação: os pinos do SPI são D10-D13
/// em qualquer front end.
///
/// O catálogo diz o que <em>seria</em> medido. Nada aqui executa nada — ver
/// <see cref="EstadoIntegridade.PorImplementar"/>.
/// </summary>
public static class CatalogoTestes
{
    /// <summary>Pinos do ATmega328P no mapa GPIO, na ordem em que são apresentados.</summary>
    public static readonly IReadOnlyList<string> Pinos =
    [
        "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7",
        "D8", "D9", "D10", "D11", "D12", "D13",
        "A0", "A1", "A2", "A3", "A4", "A5", "RST"
    ];

    private static readonly string[] TodosOsDigitais =
        ["D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7",
         "D8", "D9", "D10", "D11", "D12", "D13"];

    public static readonly IReadOnlyList<TesteCatalogo> Todos =
    [
        new("UART",          ["D0", "D1"],                                  "UART"),
        new("GPIO Escrita",  TodosOsDigitais),
        new("GPIO Leitura",  TodosOsDigitais),
        new("SPI",           ["D10", "D11", "D12", "D13"],                  "SPI"),
        new("I²C",           ["A4", "A5"],                                  "I2C"),
        new("ADC",           ["A0", "A1", "A2", "A3", "A4", "A5"]),
        new("PWM",           ["D3", "D5", "D6", "D9", "D10", "D11"],        "PWM"),
    ];
}

/// <summary>
/// Porque é que a verificação de integridade não devolve resultados.
///
/// O firmware Prog_Tester V1.2 só expõe diagnósticos de Serial2 e de SPI: para GPIO,
/// I²C, ADC e PWM não há comando nenhum, e sem medição não há resultado. Um PASS/FAIL
/// sorteado seria indistinguível à bancada de uma medição verdadeira — daí este estado
/// existir em vez de se inventarem resultados.
/// </summary>
public static class EstadoIntegridade
{
    public const string PorImplementar =
        "Verificação de integridade por implementar — o firmware não expõe estes testes. " +
        "Nada foi medido.";
}
