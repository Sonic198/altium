namespace Altium.Shared;

public static class CancellationTokenHelper
{
    public static CancellationToken CancelOnKeyPress()
    {
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            Console.WriteLine("Canceling...");
            cts.Cancel();
            e.Cancel = true;
        };
        return cts.Token;
    }
}
