using Microsoft.AspNetCore.SignalR;

namespace ATMegaPestaV1.Api.Bench;

/// <summary>
/// Progress channel for the front ends.
///
/// It only carries server → client traffic: the bench's operations are asked for over HTTP,
/// and what comes through here are the status lines the WPF used to write on the card
/// while the operation ran ("A activar o USBAsp no barramento..."). An ISP access takes
/// seconds and nobody should be left staring at a frozen screen.
/// </summary>
public class BenchHub : Hub
{
    /// <summary>Name of the progress event, so the client does not guess the string.</summary>
    public const string ProgressEvent = "progress";
}

/// <summary>A progress line: the text and the colour it appears in.</summary>
public record Progress(string Text, LedState Severity);
