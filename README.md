# SMSM: Simplistic Minecraft Server Manager

A Windows service to help you run a Minecraft server, taking care of only the operation, and getting out of the way otherwise.

This is intended for people who know how to set up and configure a Minecraft server without hand-holding.

**Features:**
- Runs as a Windows service
- Supports multiple independent instances on the same machine
- Allows scheduling of actions
- Can take backups of the world
- Doesn't fuck up your configs
- Doesn't use Docker

Thanks to AMP for giving me the incentive to learn how to implement a Windows service :)

⚠ SMSM doesn't make any attempt to be secure. The remote management utility connects via a named pipe, which may be network-accessible in some configurations. It usually requires administrator privileges to access, but this can be disabled if you misconfigure Windows. Please verify that access is properly restricted after installation!

## Installation
Prerequisite: [.NET 6](https://dotnet.microsoft.com/en-us/)
- Make sure your Minecraft server is set up and ready to run.
- Decide if you want to use an existing account, or create a new service account (recommended). Create it if necessary, and make sure the password is not set to expire. Ideally this should be a non-administrative account.
- Place the SMSM files into a location where the chosen account has permissions to access (e.g. `C:\Program Files\SMSM\`)
- Make sure the service account also has write permissions to the Minecraft server directory.
- Copy the sample config `SampleConfig.json` to a convenient location (recommended in the same location as the server `.jar`), and optionally rename it.
- Edit the config, making sure to set `Name`, `ServerDir`, `JavaPath`, `ServerJar`, and the `Schedule`. Change any other settings you desire. See below for explanations.
- Open an **Administrative** PowerShell window, and `cd` to the location where you placed SMSM.
- Run `.\Install-SMSM.ps1` and answer the prompts.
- Your service should now be installed. Verify by using `services.msc`, look for the name `Minecraft server ____ (SMSM)`.
- Start the service. This is only required this first time, it will auto-start at boot from now on.
- Make sure the service shows as `Running`, and you receive no errors trying to start it.
- Your Minecraft server should be running. You're done!

You can install multiple servers, by reusing the same SMSM files, and simply creating another `config.json` pointing to a different server. Just repeat the above steps for the second config.

## Management
To manage the server, start the management utility from an **administrator** PowerShell window: `.\SMSMRemoteTool.exe <Name>`, where `Name` is the name used to install the service (should be the `Name` in the `config.json`).  
If you are successfully connected, you should see `-> Connected to management interface on "<Name>".`
You can now enter any command from the section below.  
Once you are done, type `exit` and press enter. This stops the remote session, but does **not** turn off the server. To completely shut down the server, use the `stop` command, and then stop the service.

## Commands
These are used both by the remote management tool, and by the task scheduler.
| Task | Argument | Description |
|---|---|---|
| `"start"` | None | Starts the server if it is not running. |
| `"stop"` | None | Stops the server if it is running. |
| `"restart"` | None | Stops the server if it is running, then starts it. |
| `"command"` | Command | Sends the command in the argument as-is to the server console. |
| `"message"` | Text | Sends the message text out to all players as a server-wide announcement. |
| `"save"` | None | Saves all non-saved changes to the world. |
| `"backup"` | None | Disables auto-saving, takes a backup of the world, then re-enables auto-saving. |

## Scheduling Tasks
SMSM has a simple task scheduler built in. You can specify as many tasks as you want, but the order in which they run if scheduled at the same time is not guaranteed.

### Schedule Definition
To define a scheduled task, add an object to the `"Schedule"` array. Specify a `"Name"` and the `"Task"` (see `Commands` above). Then specify the times you want to run the task using `"Minutes"`, `"Hours"`, `"Days"`, and `"Weekdays"`. It will run if all 4 lists have a match for the current time. By default, each of these is set to include all possible values. Override any you want to specify.


**Examples:**

Every 5 minutes:
```json
"Minutes": [0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55]
```

Every 2 hours at xx:34 (00:34, 02:34, 04:34, etc):
```json
"Minutes": [34],
"Hours": [0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22]
```

Every Wednesday at 01:50:
```json
"Minutes": [50],
"Hours": [1],
"Weekdays": ["Wed"]
```

Every hour, but only on the 3rd of every month:
```json
"Minutes": [0],
"Days": [3]
```

### Schedule Exceptions
Schedule exceptions are defined in a similar way to the base schedule, in that all specified items must match. However, they default to nothing if that item is not specified. You can use the string `"All"` to include all possible items for a value.  
They are placed in the `"Exceptions"` object array, and any number of exceptions can be added for each scheduled task.

Every 5 minutes, except at 03:10
```json
"Minutes": [0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50, 55],
"Exceptions":
[
    {
        "Minutes": [10],
        "Hours": [3],
        "Days": "All",
        "Weekdays": "All"
    }
]
```

Every hour, except on Sundays at midnight or Wednesdays at noon:
```json
"Minutes": [0],
"Exceptions":
[
    {
        "Minutes": "All",
        "Hours": [0],
        "Days": "All",
        "Weekdays": ["Sun"]
    },
    {
        "Minutes": "All",
        "Hours": [12],
        "Days": "All",
        "Weekdays": ["Wed"]
    }
]
```

Every Wednesday at 01:50, except if it is the 1st of the month:
```json
"Minutes": [50],
"Hours": [1],
"Weekdays": ["Wed"],
"Exceptions":
[
    {
        "Minutes": "All",
        "Hours": "All",
        "Days": [1],
        "Weekdays": "All"
    }
]
```