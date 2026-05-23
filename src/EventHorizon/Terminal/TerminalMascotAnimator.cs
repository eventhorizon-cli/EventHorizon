namespace EventHorizon.Terminal;

internal static class TerminalMascotAnimator
{
    private static readonly IReadOnlyList<string[]> Frames =
    [
        [
            " /\\_/\\\\         ",
            "( ^.^ )         ",
            "/ >⌨< \\\\  ^     ",
            "coding...       "
        ],
        [
            " /\\_/\\\\         ",
            "( o.o )         ",
            "/ >⌨< \\\\  ~     ",
            "coding.._       "
        ],
        [
            " /\\_/\\\\         ",
            "( -.- )         ",
            "/ >⌨< \\\\  .     ",
            "coding...       "
        ],
        [
            " /\\_/\\\\         ",
            "( ^.^ )         ",
            "/ >⌨< \\\\  ~     ",
            "coding.._       "
        ],
        [
            " /\\_/\\\\         ",
            "( ^w^ )         ",
            "/ >⌨< \\\\  ^     ",
            "coding...       "
        ],
        [
            " /\\_/\\\\         ",
            "( -.^ )         ",
            "/ >⌨< \\\\  ~     ",
            "coding.._       "
        ]
    ];

    public static int GetFrameIndex(DateTimeOffset timestamp)
        => (int)((timestamp.ToUnixTimeMilliseconds() / 440) % Frames.Count);

    public static IReadOnlyList<string> GetFrameLines(int frameIndex)
        => Frames[Math.Abs(frameIndex) % Frames.Count];
}
