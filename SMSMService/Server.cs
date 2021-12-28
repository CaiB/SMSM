using System;
using System.Diagnostics;

namespace SMSMService;
public static class Server
{
    private static Process? ServerProc;
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
                RedirectStandardInput = true
            };
            ServerProc.Exited += ServerExitHandler;
            ServerProc.EnableRaisingEvents = true;
            ServerProc.Start();
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
        if (ServerProc != null)
        {
            ServerProc.StandardInput.WriteLine(input);
        }
    }

    public static void KillServer()
    {
        ServerProc?.Kill();
        ServerProc = null;
    }

    public static void WaitForExit() => ServerProc?.WaitForExit();

    private static void ServerExitHandler(object? sender, EventArgs evt)
    {
        Log.Info("Server process exited.");
        if (ServerProc != null && ServerProc.HasExited) { ServerProc = null; }
    }
}
