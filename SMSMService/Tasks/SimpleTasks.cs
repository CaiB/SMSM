using System;

namespace SMSMService.Tasks
{
    public interface IServerTask
    {
        public bool DoTask(string? arguments);
    }

    public static class TaskHandler // This isn't the prettiest way to do this but meh
    {
        public static Dictionary<string, IServerTask> Tasks = new();

        public static void AddTasks()
        {
            Tasks.Add("stop", new StopServer());
            Tasks.Add("restart", new RestartServer());
            Tasks.Add("command", new SendRawInput());
            Tasks.Add("message", new SendMessage());
            Tasks.Add("save", new SaveWorld());
            Tasks.Add("backup", new CreateBackup());
        }
    }

    public class StopServer : IServerTask
    {
        public bool DoTask(string? arguments)
        {
            Server.SendInput("/stop");
            return true;
        }
    }

    public class RestartServer : IServerTask
    {
        public bool DoTask(string? arguments)
        {
            Server.SendInput("/stop");
            Server.WaitForExit();
            return Server.StartServer();
        }
    }

    public class SendRawInput : IServerTask
    {
        public bool DoTask(string? arguments)
        {
            if (arguments != null) { Server.SendInput(arguments); }
            return true;
        }
    }

    public class SendMessage : IServerTask
    {
        public bool DoTask(string? arguments)
        {
            if (arguments != null) { Server.SendInput($"/say {arguments}"); }
            return true;
        }
    }

    public class SaveWorld : IServerTask
    {
        public bool DoTask(string? arguments)
        {
            Server.SendInput("/save-all");
            return true;
        }
    }

    public class CreateBackup : IServerTask
    {
        public bool DoTask(string? arguments)
        {
            Server.SendInput("/save-off");
            Server.SendInput("/say pretending to do backup");
            Server.SendInput("/save-on");
            return true;
        }
    }
}
