# Date and Time

Huml.Net serialises and deserialises `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`,
and `TimeOnly` natively as quoted ISO-8601 / canonical strings. No custom converter is required.

## Supported Types

| Type              | Format             | Example                               | Availability     |
| ----------------- | ------------------ | ------------------------------------- | ---------------- |
| `DateTime`        | `"O"`              | `"2026-05-19T14:30:00.0000000"`       | All TFMs         |
| `DateTimeOffset`  | `"O"`              | `"2026-05-19T14:30:00.0000000+01:00"` | All TFMs         |
| `TimeSpan`        | `"c"`              | `"1.02:03:04"`                        | All TFMs         |
| `DateOnly`        | `yyyy-MM-dd`       | `"2026-05-19"`                        | .NET 6 and later |
| `TimeOnly`        | `HH:mm:ss.FFFFFFF` | `"14:30:00"`                          | .NET 6 and later |

All five types serialise as quoted HUML string scalars and deserialise back with full fidelity.

## Usage

```csharp
using Huml.Net;

public class Event
{
    public string Name { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public TimeSpan Duration { get; set; }
}

var ev = HumlSerializer.Deserialize<Event>("""
    %HUML v0.2.0
    Name: "conference"
    OccurredAt: "2026-05-19T09:00:00.0000000"
    Duration: "0.08:00:00"
    """);
// ev.OccurredAt == new DateTime(2026, 5, 19, 9, 0, 0)
// ev.Duration   == TimeSpan.FromHours(8)
```

## DateOnly and TimeOnly

`DateOnly` and `TimeOnly` are available on .NET 6 and later only. These types do not exist
on `netstandard2.1`. If your project targets `netstandard2.1`, use `DateTime` or a custom
converter instead.

```csharp
using Huml.Net;

public class Schedule
{
    public DateOnly Date { get; set; }
    public TimeOnly StartsAt { get; set; }
}

var schedule = HumlSerializer.Deserialize<Schedule>("""
    %HUML v0.2.0
    Date: "2026-05-19"
    StartsAt: "09:30:00"
    """);
// schedule.Date     == new DateOnly(2026, 5, 19)
// schedule.StartsAt == new TimeOnly(9, 30)
```

## Compatibility

| Type             | netstandard2.1 | .NET 8 | .NET 9 | .NET 10 |
| ---------------- | -------------- | ------ | ------ | ------- |
| `DateTime`       | Yes            | Yes    | Yes    | Yes     |
| `DateTimeOffset` | Yes            | Yes    | Yes    | Yes     |
| `TimeSpan`       | Yes            | Yes    | Yes    | Yes     |
| `DateOnly`       | No             | Yes    | Yes    | Yes     |
| `TimeOnly`       | No             | Yes    | Yes    | Yes     |

## Migration Note

Prior to 0.2.0-alpha.2, all five types fell through to the POCO reflection path. `DateTime`
and `DateTimeOffset` produced incorrect output and `DateOnly`/`TimeOnly` would throw
`InvalidCastException` during deserialisation. Any custom converter previously used to work
around this can be removed.

## See also

- [Write a custom converter](custom-converters.md) — for non-default date/time formats.
- [Handle errors](error-handling.md) — what a malformed date scalar throws.
