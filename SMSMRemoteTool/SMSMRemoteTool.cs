using System.IO.Pipes;
using System.Text;

namespace SMSM.RemoteTool;

public static class SMSMRemoteTool
{
    private static NamedPipeClientStream? PipeClient;
    private static PipeInterface? PipeRW;
    private static Thread? ReadThread;
    private static bool Exiting = false;

    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Please specify the server name to connect to as a command line argument.");
            Console.WriteLine("If the server name contains spaces, wrap it in quotes.");
            Console.WriteLine("No other arguments are accepted.");
            Environment.Exit(-1);
        }

        PipeClient = new(".", $"SMSM-Mgmt-{args[0]}", PipeDirection.InOut, PipeOptions.Asynchronous);
        Console.WriteLine("Connecting to server...");
        PipeClient.Connect();
        Console.WriteLine("Connected.");

        PipeRW = new(PipeClient);
        ReadThread = new(ReadLoop) { Name = "Pipe Read Thread" };
        ReadThread.Start();

        while (true)
        {
            string? UserInput = Console.ReadLine();
            if (UserInput == null || UserInput.ToLowerInvariant() == "exit")
            {
                Stop();
                break;
            }
            PipeRW.WriteLine(UserInput);
        }
    }

    public static void Stop()
    {
        Exiting = true;
        PipeRW?.Dispose();
        PipeClient?.Close();
        PipeClient?.Dispose();
        ReadThread?.Join();
    }

    private static void ReadLoop()
    {
        if (PipeRW == null) { throw new NullReferenceException(nameof(PipeRW)); }
        while (!Exiting)
        {
            string? ReadResult = PipeRW.ReadLine();
            if (ReadResult == null) { break; }
            Console.WriteLine($"-> {ReadResult}");
        }
        Console.WriteLine("Connection closed.");
    }

    private class PipeInterface : IDisposable
    {
        private readonly StreamReader Reader;
        private readonly StreamWriter Writer;

        public PipeInterface(Stream stream)
        {
            this.Reader = new(stream, Encoding.UTF8);
            this.Writer = new(stream, Encoding.UTF8);
        }

        public string? ReadLine() => this.Reader.ReadLine();

        public void WriteLine(string line)
        {
            this.Writer.WriteLine(line);
            this.Writer.Flush();
        }

        public void Dispose()
        {
            try
            {
                ((IDisposable)this.Reader).Dispose();
                ((IDisposable)this.Writer).Dispose();
            }
            catch(Exception) { }
        }
    }
}