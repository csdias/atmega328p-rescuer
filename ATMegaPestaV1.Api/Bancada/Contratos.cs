using ATMegaPestaV1.Diagnosticos;

namespace ATMegaPestaV1.Api.Bancada;

/// <summary>
/// Estado de um indicador, no vocabulário que os LEDs do WPF já usavam. Serializa como
/// texto ("ok", "aviso", ...) para o front end não depender da ordem dos membros.
/// </summary>
public enum EstadoLed
{
    Inactivo,
    Ok,
    Aviso,
    Erro
}

/// <summary>Um indicador: a cor e a linha de texto que a acompanha.</summary>
public record Indicador(EstadoLed Estado, string Detalhe);

/// <summary>Parâmetros da bancada que o front end precisa de saber ao arrancar.</summary>
public record ConfigResponse(
    int MaxTentativas,
    bool VerificarAssinatura,
    int MaxTentativasLeitura,
    string PastaCopias);

/// <summary>
/// Resultado de uma varredura ao USB, já com a assinatura verificada.
/// </summary>
/// <param name="PodeAvancar">Bancada completa e equipamento identificado.</param>
/// <param name="Esgotado">
/// Tentativas gastas sem sucesso — o front end mostra o encerramento, como o WPF faz.
/// </param>
public record DeteccaoResponse(
    Indicador Ch340,
    Indicador UsbAsp,
    Indicador Assinatura,
    string? PortaCom,
    bool PodeAvancar,
    int Tentativa,
    int MaxTentativas,
    bool Esgotado,
    string Mensagem,
    EstadoLed Severidade);

/// <summary>Os quatro bytes, como o avrdude os devolveu.</summary>
public record FusesDto(string? Low, string? High, string? Extended, string? Lock);

/// <summary>Os mesmos bytes em linguagem humana.</summary>
public record FusesDecodificadosDto(
    string Relogio,
    bool Ckdiv8Activo,
    string BrownOut,
    bool SpiActivo,
    bool ResetActivo,
    bool EepromPreservada,
    bool BootRstActivo,
    string Bloqueio,
    bool LeituraLivre,
    string DescricaoLow,
    string DescricaoHigh,
    string DescricaoExtended,
    string DescricaoLock);

/// <summary>Características fixas do chip identificado, já formatadas.</summary>
public record McuDto(
    string Nome,
    int FlashBytes,
    int EepromBytes,
    int SramBytes,
    string Flash,
    string Eeprom,
    string Sram);

/// <summary>
/// O mapa da Flash. A fatia da aplicação é o que <em>sobra</em> depois de reservado o
/// bootloader — capacidade, não ocupação: o conteúdo da Flash não é lido.
/// </summary>
public record MapaFlashDto(
    int TotalBytes,
    int BootloaderBytes,
    int AplicacaoBytes,
    string Bootloader,
    string Aplicacao,
    bool BootloaderReservado);

/// <summary>
/// Resultado de uma leitura das configurações do chip-alvo.
/// </summary>
/// <param name="Identificado">O chip respondeu ao ISP.</param>
/// <param name="ConfiguracoesLidas">
/// Chip identificado <em>e</em> fuses descodificados. É isto — e não
/// <paramref name="Identificado"/> — que destranca a verificação de integridade.
/// </param>
/// <param name="Esgotado">Tentativas de ISP gastas: o front end propõe a alta tensão.</param>
/// <param name="BarramentoIsolado">
/// O barramento voltou a Hi-Z. Falso é um aviso a mostrar, não um detalhe interno.
/// </param>
public record LeituraResponse(
    bool Identificado,
    bool ConfiguracoesLidas,
    McuDto? Mcu,
    string? Assinatura,
    FusesDto? Fuses,
    FusesDecodificadosDto? Descodificado,
    MapaFlashDto? MapaFlash,
    string Estado,
    EstadoLed Severidade,
    string? Instrucao,
    string? Tentativas,
    int Tentativa,
    int MaxTentativas,
    bool Esgotado,
    bool BarramentoIsolado,
    string? SaidaAvrdude);

/// <summary>Um ficheiro da cópia e por onde se descarrega.</summary>
public record FicheiroCopia(string Nome, string Url, long Bytes);

/// <summary>
/// Resultado de uma cópia de segurança. A pasta é do lado do servidor — o browser não
/// escolhe pastas — e os ficheiros descarregam-se pelos URLs devolvidos.
/// </summary>
public record CopiaResponse(
    bool Sucesso,
    string Carimbo,
    string Pasta,
    IReadOnlyList<FicheiroCopia> Ficheiros,
    FusesDto? Fuses,
    string Mensagem,
    EstadoLed Severidade,
    string? SaidaAvrdude,
    bool BarramentoIsolado);

/// <summary>O que quem pediu a verificação decidiu sobre a cópia antes de avançar.</summary>
public enum EscolhaTransferencia
{
    ProsseguirComCopia,
    ProsseguirSemCopia
}

public record IntegridadeRequest(EscolhaTransferencia Escolha);

/// <summary>Estado de um teste na lista de resultados.</summary>
public enum EstadoTeste
{
    Pendente,
    Passou,
    Falhou
}

public record ResultadoTesteDto(string Nome, EstadoTeste Estado, string Tempo);

/// <summary>
/// Resultado da verificação de integridade.
///
/// <paramref name="Resultados"/> vem sempre pendente e <paramref name="ProgressoPct"/> a
/// zero: ver <see cref="EstadoIntegridade.PorImplementar"/>. O barramento é comutado para
/// o master e isolado no fim, porque isso acontece de facto — o que não acontece é a
/// medição.
/// </summary>
public record IntegridadeResponse(
    bool BarramentoComutado,
    bool BarramentoIsolado,
    CopiaResponse? Copia,
    IReadOnlyList<ResultadoTesteDto> Resultados,
    int ProgressoPct,
    string Mensagem,
    EstadoLed Severidade);

/// <summary>Que testes existem, que pinos tocam, e o aviso de que não são executados.</summary>
public record CatalogoResponse(
    IReadOnlyList<string> Pinos,
    IReadOnlyList<TesteCatalogo> Testes,
    string Aviso);
