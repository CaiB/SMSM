using System;

namespace SMSMService.Tasks
{
    public static class TaskHandler
    {
        public delegate bool ServerTask(string? arguments);
        public static Dictionary<string, ServerTask> Tasks = new();

        public static void AddTasks()
        {
            Tasks.Add("start",
                (string? args) => { return Server.StartServer(); }
            );
            Tasks.Add("stop",
                (string? args) =>
                {
                    Server.SendInput("/stop");
                    return true;
                }
            );
            Tasks.Add("restart",
                (string? args) =>
                {
                    Server.SendInput("/stop");
                    Server.WaitForExit();
                    return Server.StartServer();
                }
            );
            Tasks.Add("command",
                (string? args) =>
                {
                    if (args != null) { Server.SendInput(args); }
                    return true;
                }
            );
            Tasks.Add("message",
                (string? args) =>
                {
                    if (args != null) { Server.SendInput($"/say {args}"); }
                    return true;
                }
            );
            Tasks.Add("save",
                (string? args) =>
                {
                    Server.SendInput("/save-all");
                    return true;
                }
            );
            Tasks.Add("backup",
                (string? args) =>
                {
                    Server.SendInput("/save-all");
                    Server.SendInput("/save-off");
                    Thread.Sleep(500); // TODO: Wait for the output that says this was done to make sure we aren't starting too early.
                    BackupTask.Run();
                    Thread.Sleep(500);
                    Server.SendInput("/save-on");
                    return true;
                }
            );
        }
    }
}
