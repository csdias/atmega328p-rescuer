using Microsoft.AspNetCore.SignalR;

namespace ATMegaPestaV1.Api.Bancada;

/// <summary>
/// Canal de progresso para os front ends.
///
/// Só serve tráfego servidor → cliente: as operações da bancada pedem-se por HTTP, e o
/// que vem por aqui são as linhas de estado que o WPF escrevia no cartão enquanto a
/// operação corria ("A activar o USBAsp no barramento...").  Um acesso ISP demora
/// segundos e ninguém deve ficar a olhar para um ecrã parado.
/// </summary>
public class BancadaHub : Hub
{
    /// <summary>Nome do evento de progresso, para o cliente não adivinhar a string.</summary>
    public const string EventoProgresso = "progresso";
}

/// <summary>Uma linha de progresso: o texto e a cor com que aparece.</summary>
public record Progresso(string Texto, EstadoLed Severidade);
