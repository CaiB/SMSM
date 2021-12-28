using SMSMService.Tasks;
using System;

namespace SMSMService;
public static class Scheduler
{
    public static int[] DEFAULT_MINUTES = 
    {
            0,  1,  2,  3,  4,  5,  6,  7,  8,  9,
        10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
        20, 21, 22, 23, 24, 25, 26, 27, 28, 29,
        30, 31, 32, 33, 34, 35, 36, 37, 38, 39,
        40, 41, 42, 43, 44, 45, 46, 47, 48, 49,
        50, 51, 52, 53, 54, 55, 56, 57, 58, 59
    };

    public static int[] DEFAULT_HOURS = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23 };

    public static string[] DEFAULT_WEEKDAYS = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

    public static int[] DEFAULT_DAYS =
    {
            0,  1,  2,  3,  4,  5,  6,  7,  8,  9,
        10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
        20, 21, 22, 23, 24, 25, 26, 27, 28, 29,
        30, 31
    };

    /// <summary>Converts an array of string weekdays to their internal counterpart.</summary>
    /// <param name="weekdays">The weekday string array to parse</param>
    /// <returns>An array of weekday enums, the same length, and in the same order as the input</returns>
    /// <exception cref="ArgumentOutOfRangeException">If any of the weekdays could not be parsed</exception>
    public static DayOfWeek[] ParseWeekdays(string[] weekdays)
    {
        DayOfWeek[] Result = new DayOfWeek[weekdays.Length];
        for(int i = 0; i < weekdays.Length; i++)
        {
            Result[i] = weekdays[i].ToLowerInvariant() switch
            {
                "mon" or "monday" => DayOfWeek.Monday,
                "tue" or "tues" or "tuesday" => DayOfWeek.Tuesday,
                "wed" or "wednesday" => DayOfWeek.Wednesday,
                "thu" or "thur" or "thurs" or "thursday" => DayOfWeek.Thursday,
                "fri" or "friday" => DayOfWeek.Friday,
                "sat" or "saturday" => DayOfWeek.Saturday,
                "sun" or "sunday" => DayOfWeek.Sunday,
                _ => throw new ArgumentOutOfRangeException($"'{weekdays[i]}' at index {i} could not be parsed as a weekday."),
            };
        }
        return Result;
    }

    public static ScheduledTask[]? Tasks;
    private static System.Timers.Timer? Timer;

    public static void Start()
    {
        Timer = new()
        {
            AutoReset = true,
            Enabled = true,
            Interval = 60 * 1000 // Every minute
        };
        Timer.Elapsed += HandleTimer;
        Timer.Start();
    }

    public static void Stop()
    {
        Timer?.Stop();
        Timer?.Dispose();
    }

    private static void HandleTimer(object? sender, System.Timers.ElapsedEventArgs evt)
    {
        if (Tasks == null) { return; }

        DateTime Now = DateTime.Now;

        foreach (ScheduledTask Task in Tasks)
        {
            if (Task.IsScheduledAtTime(Now) && !Task.HasExceptionAtTime(Now))
            {
                TaskHandler.Tasks[Task.Task].Invoke(Task.TaskArgs);
            }
        }
    }
}

public class ScheduledTask
{
    public string Name { get; init; }
    public string Task { get; init; }
    public string? TaskArgs { get; init; }
    public int[] Minutes { get; init; }
    public int[] Hours { get; init; }
    public int[] Days { get; init; }
    public DayOfWeek[] Weekdays { get; init; }
    public TaskScheduleException[] Exceptions { get; init; }

    public ScheduledTask()
    {
        this.Name = "Invalid Task";
        this.Task = "invalid-task";
        this.Minutes = Array.Empty<int>();
        this.Hours = Array.Empty<int>();
        this.Days = Array.Empty<int>();
        this.Weekdays = Array.Empty<DayOfWeek>();
        this.Exceptions = Array.Empty<TaskScheduleException>();
    }

    /// <summary>Checks to see if this task is supposed to run in the minute given by the DateTime.</summary>
    /// <remarks>Does not check schedule exceptions.</remarks>
    /// <param name="time">The time to check</param>
    /// <returns>true if the task's regular schedule includes the given minute</returns>
    public bool IsScheduledAtTime(DateTime time) => this.Days.Contains(time.Day) && this.Weekdays.Contains(time.DayOfWeek) && this.Hours.Contains(time.Hour) && this.Minutes.Contains(time.Minute);

    /// <summary>Checks to see if this task has a schedule exception preventing it from running in the minute given by the DateTime.</summary>
    /// <param name="time">The time to check</param>
    /// <returns>true if there is a schedule exception in effect at the given minute, false otherwise</returns>
    public bool HasExceptionAtTime(DateTime time)
    {
        foreach (TaskScheduleException Exc in this.Exceptions)
        {
            if (Exc.Days.Contains(time.Day) && Exc.Weekdays.Contains(time.DayOfWeek) && Exc.Hours.Contains(time.Hour) && Exc.Minutes.Contains(time.Minute)) { return true; }
        }
        return false;
    }
}

public class TaskScheduleException // This is not a good name, but I can't come up with anything better :(
{
    public int[] Minutes { get; init; }
    public int[] Hours { get; init; }
    public int[] Days { get; init; }
    public DayOfWeek[] Weekdays { get; init; }

    public TaskScheduleException()
    {
        this.Minutes = Array.Empty<int>();
        this.Hours = Array.Empty<int>();
        this.Days = Array.Empty<int>();
        this.Weekdays= Array.Empty<DayOfWeek>();
    }
}

