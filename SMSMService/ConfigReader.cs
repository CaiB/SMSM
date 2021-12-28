using Newtonsoft.Json.Linq;
using System;

namespace SMSMService;
public class ConfigReader
{
    public static bool ReadConfig(string path)
    {
        if (!File.Exists(path)) { throw new FileNotFoundException($"Config file was not found at '{path}'."); }
        JObject JSON;
        using (StreamReader Reader = File.OpenText(path)) { JSON = JObject.Parse(Reader.ReadToEnd()); }

        string Name = ReadProperty(JSON, "Name", "Minecraft Server", true);
        Log.SetName(Name);
        string? ServerDir = ReadProperty<string?>(JSON, "ServerDir", null, true);
        string? JavaPath = ReadProperty<string?>(JSON, "JavaPath", null, true);
        uint MinRAM = ReadProperty(JSON, "MinRAM", 1024U, true);
        uint MaxRAM = ReadProperty(JSON, "MaxRAM", 2048U, true);
        string JavaArgs = ReadProperty(JSON, "JavaArgs", "", false);
        string? ServerJar = ReadProperty<string?>(JSON, "ServerJar", null, true);
        string ServerArgs = ReadProperty(JSON, "ServerArgs", "", false);

        ScheduledTask[] Schedule = Array.Empty<ScheduledTask>();
        if (JSON.ContainsKey("Schedule") && JSON["Schedule"] is JArray ScheduleArray)
        {
            List<ScheduledTask> Tasks = new();
            foreach (JObject ScheduleEntry in ScheduleArray)
            {
                TaskScheduleException[] Exceptions = Array.Empty<TaskScheduleException>();
                if (ScheduleEntry.ContainsKey("Exceptions") && ScheduleEntry["Exceptions"] is JArray ExceptionArray)
                {
                    List<TaskScheduleException> ParsedExceptions = new();
                    foreach (JObject ExceptionEntry in ExceptionArray)
                    {
                        ParsedExceptions.Add(new()
                        {
                            Minutes = ReadArrayOrAll(ExceptionEntry, "Minutes", Array.Empty<int>(), Scheduler.DEFAULT_MINUTES, false),
                            Hours = ReadArrayOrAll(ExceptionEntry, "Hours", Array.Empty<int>(), Scheduler.DEFAULT_HOURS, false),
                            Days = ReadArrayOrAll(ExceptionEntry, "Days", Array.Empty<int>(), Scheduler.DEFAULT_DAYS, false),
                            Weekdays = Scheduler.ParseWeekdays(ReadArrayOrAll(ExceptionEntry, "Weekdays", Array.Empty<string>(), Scheduler.DEFAULT_WEEKDAYS, false))
                        });
                    }
                    Exceptions = ParsedExceptions.ToArray();
                }

                string? TaskCommand = ReadProperty<string?>(ScheduleEntry, "Task", null, true);
                if (TaskCommand == null) { continue; }

                Tasks.Add(new()
                {
                    Name = ReadProperty(ScheduleEntry, "Name", "Scheduled Task", true),
                    Task = TaskCommand,
                    Minutes = ReadArray(ScheduleEntry, "Minutes", Scheduler.DEFAULT_MINUTES, false),
                    Hours = ReadArray(ScheduleEntry, "Hours", Scheduler.DEFAULT_HOURS, false),
                    Days = ReadArray(ScheduleEntry, "Days", Scheduler.DEFAULT_DAYS, false),
                    Weekdays = Scheduler.ParseWeekdays(ReadArray(ScheduleEntry, "Weekdays", Scheduler.DEFAULT_WEEKDAYS, false)),
                    Exceptions = Exceptions
                });
            }
            Schedule = Tasks.ToArray();
        }

        // Check all the paths to make sure we are ready to run.
        if (string.IsNullOrWhiteSpace(JavaPath)) { JavaPath = Native.GetFullPathFromWindows("java.exe"); }
        if (JavaPath == null || !File.Exists(JavaPath))
        {
            Log.Error($"Java was not found in the specified location '{JavaPath}'.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(ServerDir)) { ServerDir = Directory.GetCurrentDirectory(); }
        if (!Directory.Exists(ServerDir))
        {
            Log.Error($"The server directory '{ServerDir}' does not exist.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(ServerJar))
        {
            Log.Error("The server jar file name was not specified.");
            return false;
        }

        string ServerJarPath = Path.Combine(ServerDir, ServerJar);
        if (!File.Exists(ServerJarPath))
        {
            Log.Error($"The server jar file was not found at '{ServerJarPath}'.");
            return false;
        }

        string GUIOption = SMSM.USE_NOGUI ? "--nogui" : "";
        string Arguments = $"-Xms{MinRAM}M -Xmx{MaxRAM}M {JavaArgs} -jar \"{ServerJarPath}\" {GUIOption} {ServerArgs}";
        Log.Info($"Using Java installed at \"{JavaPath}\"");
        Log.Info($"Will run with arguments: \"{Arguments}\"");

        SMSM.JavaPath = JavaPath;
        SMSM.JavaArgs = Arguments;
        SMSM.ServerDir = ServerDir;

        return true;
    }

    /// <summary>Reads a property from JSON, and returns the default if it wasn't found, optionally writing a warning to the log in this case.</summary>
    /// <typeparam name="T">The type of the property to read</typeparam>
    /// <param name="source">The JSON object to read from</param>
    /// <param name="name">The name of the property to read</param>
    /// <param name="defaultValue">The default value to use if the property was not found</param>
    /// <param name="warnIfNotFound">Whether to output a warning to the log if the property was not found</param>
    /// <returns>A property value, either from JSON or the default</returns>
    private static T ReadProperty<T>(JObject source, string name, T defaultValue, bool warnIfNotFound)
    {
        T? Result = source.Value<T>(name);
        if (Result != null) { return Result; }
            
        if (warnIfNotFound) { Log.Warn($"'{name}' was not found in the config file. Using defautl value of '{defaultValue}'."); }
        return defaultValue;
    }

    /// <summary>Reads an array from JSON, and returns the default if it wasn't found, optionally writing a warning to the log in this case.</summary>
    /// <typeparam name="T">The type of the array to read. Specify the type of an element, not the array.</typeparam>
    /// <param name="source">The JSON object to read from</param>
    /// <param name="name">The name of the array to read</param>
    /// <param name="defaultValue">The default value to use if the array was not found</param>
    /// <param name="warnIfNotFound">Whether to output a warning to the log if the array was not found</param>
    /// <returns>An array, either from JSON or the default</returns>
    private static T[] ReadArray<T>(JObject source, string name, T[] defaultValue, bool warnIfNotFound)
    {
        if (source.ContainsKey(name) && source[name] is JArray Array)
        {
            T[]? TypedArray = Array.ToObject<T[]>();
            return TypedArray ?? defaultValue;
        }

        if (warnIfNotFound) { Log.Warn($"'{name}' was not found or invalid in the config file (expected array). Using default value."); }
        return defaultValue;
    }

    /// <summary>Reads an array from JSON, and returns the default if it wasn't found, optionally writing a warning to the log in this case. Also accepts "All" in the JSON instead, in which case it returns a different default value.</summary>
    /// <typeparam name="T">The type of the array to read. Specify the type of an element, not the array.</typeparam>
    /// <param name="source">The JSON object to read from</param>
    /// <param name="name">The name of the array to read</param>
    /// <param name="defaultValue">The default value to use if the array was not found</param>
    /// <param name="allValue">The value to return if "All" is specified in JSON</param>
    /// <param name="warnIfNotFound">Whether to output a warning to the log if the array was not found</param>
    /// <returns>An array, either from JSON, the default, or the all array</returns>
    private static T[] ReadArrayOrAll<T>(JObject source, string name, T[] defaultValue, T[] allValue, bool warnIfNotFound)
    {
        if (source.ContainsKey(name))
        {
            if (source[name] is JArray Array)
            {
                T[]? TypedArray = Array.ToObject<T[]>();
                return TypedArray ?? defaultValue;
            }
            else
            {
                string? Value = source.Value<string>(name);
                if (Value != null && Value.ToLowerInvariant() == "all") { return allValue; }
            }
        }

        if (warnIfNotFound) { Log.Warn($"'{name}' was not found or invalid in the config file (expected array). Using default value."); }
        return defaultValue;
    }
}
