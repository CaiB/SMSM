using System;
using System.Diagnostics;

namespace SMSMService;
public static class Server
{
    private static Process? ServerProc;
    private static bool ServerReady = false;
    private static readonly object ServerLockObj = new();
    public static bool StartServer()
    {
        if (ServerProc != null) { return false; }
        try
        {
            Log.Info("Starting server process");
            ServerProc = new();
            ServerProc.StartInfo = new()
            {
                Arguments = SMSM.JavaArgs,
                WorkingDirectory = SMSM.ServerDir,
                FileName = SMSM.JavaPath,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            ServerProc.EnableRaisingEvents = true;
            ServerProc.Exited += ServerExitHandler;
            ServerProc.OutputDataReceived += ServerOutputHandler;
            ServerProc.ErrorDataReceived += ServerErrorHandler;
            ServerProc.Start();
            ServerProc.BeginOutputReadLine();
            ServerProc.BeginErrorReadLine();
            ServerReady = true;
            Log.Info($"Server process started as PID {ServerProc.Id}");
            return true;
        }
        catch (Exception exc)
        {
            Log.Error(exc.ToString());
            return false;
        }
    }

    public static void SendInput(string input)
    {
        lock (ServerLockObj)
        {
            if (ServerProc != null && ServerReady)
            {
                ServerProc.StandardInput.WriteLine(input);
            }
        }
    }

    public static void KillServer()
    {
        ServerReady = false;
        ServerProc?.Kill();
        ServerProc = null;
    }

    public static void WaitForExit() => ServerProc?.WaitForExit();

    private static void ServerExitHandler(object? sender, EventArgs evt)
    {
        Log.Info("Server process exited.");
        if (ServerProc != null && ServerProc.HasExited) { ServerProc = null; }
    }

    public static event DataReceivedEventHandler? ServerOutput, ServerError;

    private static void ServerOutputHandler(object sender, DataReceivedEventArgs evt)
    {
        if (evt.Data == null) { return; }
        Log.Server(evt.Data);
        ServerOutput?.Invoke(sender, evt);
    }

    private static void ServerErrorHandler(object sender, DataReceivedEventArgs evt)
    {
        ServerError?.Invoke(sender, evt);
    }
}
