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
- Place the SMSM files into a general location (e.g. `C:\Program Files\SMSM\`)
- Copy the sample config `SampleConfig.json` to a convenient location (recommended in the same location as the server `.jar`), and optionally rename it.
- Edit the config, making sure to set `Name`, `ServerDir`, `JavaPath`, `ServerJar`, and the `Schedule`. Change any other settings you desire. See below for explanations.
- Open an **Administrative** PowerShell window, and `cd` to the location where you placed SMSM.
- Run `.\Install-SMSM.ps1` and answer the prompts. Don't close this window when done.
- Your service should now be installed. Verify by using `services.msc`, look for the name `Minecraft Server ____ (SMSM)`. It should be stopped, do not start it yet.
- Assign the user `NT Service\<Name>` (as shown by the install script) write permissions to the folder where SMSM is installed, as well as the entire Minecraft server folder and all contents. This is the service account that the server will run under, and it has minimal permissions.
- Start the service. This is only required this first time, it will auto-start at boot from now on.
- Make sure the service shows as `Running`, and you receive no errors trying to start it.
- Your Minecraft server should be running. You're done!

You can install multiple servers, by reusing the same SMSM files, and simply creating another config `.json` pointing to a different server. Just repeat the above steps for the second config.

## Management
To manage the server, start the management utility from an **administrator** PowerShell window: `.\SMSMRemoteTool.exe <Name>`, where `Name` is the name used to install the service (should be the `Name` in the config `.json`).  
If you are successfully connected, you should see `-> Connected to management interface on "<Name>".`
You can now enter any command from the section below.  
Once you are done, type `exit` and press enter. This stops the remote session, but does **not** turn off the server. To completely shut down the server, use the `stop` command, and then stop the service.

## Configuration
All settings are configured in the config `.json` file you selected when installing the service. The below table explains the options available.

| Option | Required? | Default | Type | Description |
|---|---|---|---|---|
| `Name` | **Required** | N/A | string | The name of this instance. Used to name the service, access the management interface, and in various other locations.  A name without spaces is recommended. |
| `ServerDir` | **Required** | N/A | Path string | The location of the Minecraft server files. The server JAR should be located in this folder. |
| `JavaPath` | Recommended | Java on PATH | Path string | A path to `java.exe`, including the filename. If not specified, SMSM attempts to use your system default Java installation, which may be incorrect, so it is recommended to set this. |
| `MinRAM` | Optional | 1024 | int | The amount of RAM to assign as the minimum, in MB. |
| `MaxRAM` | Optional | 2048 | int | The amount of RAM to assign as the maximum, in MB. |
| `JavaArgs` | Optional | `""` | string | Additional arguments to pass to Java (not the server). |
| `ServerJar` | **Required** | N/A | string | The name of the server JAR file. Do not include the path. |
| `ServerArgs` | Optional | `""` | string | Additional arguments to pass to the server (not Java). |
| `AutoStart` | Optional | `true` | bool | Whether to start the Minecraft server when SMSM starts. |
| `BackupsToKeep` | Optional | `20` | int | The number of backup ZIP files to keep. If there are more than this number, the oldest ones will be deleted until this number is reached. |
| `BackupExclusions` | Optional | None | string array | A list of non-case-sensitive exclusion patterns for files and directories to exclude from being backed up. Wildcards are supported, such as `?`,`*`,`**`. |
| `Schedule` | Optional | None | object array | A list of scheduled tasks that SMSM will do, regardless of whether the Minecraft server is running or not. See the below section for further details. |

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