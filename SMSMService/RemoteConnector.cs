using SMSMService.Tasks;
using System;
using System.IO.Pipes;
using System.Text;

namespace SMSMService;
public static class RemoteConnector
{
    private static Thread? PipeThread;
    private static NamedPipeServerStream? PipeServer;
    private static PipeInterface? PipeRW;
    private static bool Exiting = false;
    
    public static void Start(string serverName)
    {
        PipeThread = new Thread(RunPipeServer) { Name = "Pipe Server Thread" };
        PipeThread.Start(serverName);
    }

    private static void RunPipeServer(object? nameObj)
    {
        if (nameObj is not string ServerName) { return; }
        ServerName = ServerName.Replace(' ', '_');
        PipeServer = new NamedPipeServerStream($"SMSM-Mgmt-{ServerName}", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        while (!Exiting)
        {
            try
            {
                PipeServer.WaitForConnection();
                PipeRW = new(PipeServer);
                lock (PipeRW) { PipeRW.WriteLine($"Connected to management interface on \"{ServerName}\"."); } // TODO: Send recent server output?
                Log.Info("Management client connected.");
                Log.Handlers += LogOutputHandler;

                float RAMUsage = Server.RAMUsage / 1000000F;
                if (Server.ServerReady) { Log.Info($"Minecraft server is running, RAM usage: {RAMUsage:F1}MB"); }
                else { Log.Info("Minecraft server is not running."); }

                while (true)
                {
                    string? Input = PipeRW.ReadLine();
                    if (Input != null)
                    {
                        int SpaceIndex = Input.IndexOf(' ');
                        string Command = SpaceIndex > 0 ? Input.Substring(0, SpaceIndex) : Input;
                        string? Args = SpaceIndex > 0 ? Input.Substring(SpaceIndex + 1) : null;
                        bool IsCommand = TaskHandler.Tasks.ContainsKey(Command);
                        if (IsCommand)
                        {
                            Log.Info($"Executing command '{Command}' '{Args}' from management client.");
                            TaskHandler.Tasks[Command].Invoke(Args);
                            Log.Info("Command completed.");
                        }
                        else
                        {
                            Log.Info($"Got invalid command '{Command}' from management client.");
                            lock (PipeRW!) { PipeRW.WriteLine($"The command '{Command}' could not be understood."); }
                        }
                    }
                    else
                    {
                        Log.Handlers -= LogOutputHandler;
                        Log.Info("Management client has disconnected.");
                        PipeServer.Disconnect();
                        break;
                    }
                    Thread.Sleep(10);
                }
            }
            catch (Exception) { } // Ignore, this is us cancelling.
        }
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
            catch (Exception) { }
        }
    }

    public static void Stop()
    {
        Exiting = true;
        PipeRW?.Dispose();
        PipeServer?.Dispose();
        PipeServer = null;
        PipeThread?.Join();
    }

    private static void LogOutputHandler(string message)
    {
        if (PipeRW != null && PipeServer != null)
        {
            lock (PipeRW) { PipeRW.WriteLine(message); }
        }
    }
}
