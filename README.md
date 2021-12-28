# SMSM: Simplistic Minecraft Server Manager

A Windows service to help you run a Minecraft server, taking care of only the operation, and getting out of the way otherwise.

This is intended for people who know how to set up and configure a Minecraft server without hand-holding.

**(Intended) Features:**
- Runs as a Windows service
- Supports multiple independent instances on the same machine
- Allows scheduling of actions
- Can take backups of the world
- Doesn't fuck up your configs

Thanks AMP for giving me the incentive to learn how to implement a Windows service :)

## Scheduling Tasks
SMSM has a simple task scheduler built in. You can specify as many tasks as you want, but the order in which they run if scheduled at the same time is not guaranteed.

### Tasks
| Task | Argument | Description |
|---|---|---|
| `"start"` | None | Starts the server if it is not running. |
| `"stop"` | None | Stops the server if it is running. |
| `"restart"` | None | Stops the server if it is running, then starts it. |
| `"command"` | Command | Sends the command in the argument as-is to the server console. |
| `"message"` | Text | Sends the message text out to all players as a server-wide announcement. |
| `"save"` | None | Saves all non-saved changes to the world. |
| `"backup"` | None | Disables auto-saving, takes a backup of the world, then re-enables auto-saving. |

### Schedule Definition
To define a scheduled task, add an object to the `"Schedule"` array. Specify a `"Name"` and the `"Task"` (see above). Then specify the times you want to run the task using `"Minutes"`, `"Hours"`, `"Days"`, and `"Weekdays"`. It will run if all 4 lists have a match for the current time. By default, each of these is set to include all possible values. Override any you want to specify.  
Here's some examples:

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

Every hour, except on Sundays at midnight:
```json
"Minutes": [0],
"Exceptions":
[
    {
        "Minutes": "All",
        "Hours": [0],
        "Days": "All",
        "Weekdays": ["Sun"]
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