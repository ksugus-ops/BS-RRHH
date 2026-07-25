using FluentAssertions;
using HRIA.Domain.Entities;
using HRIA.Domain.Enums;
using Xunit;

namespace HRIA.Tests.TimeTracking;

public class WorkdayCalculationTests
{
    private static DateTime Utc(int h, int m) => new(2026, 3, 10, h, m, 0, DateTimeKind.Utc);

    [Fact]
    public void WorkedDuration_SubtractsBreaks_BR07()
    {
        // 09:00 -> 17:00 = 8h; descanso 12:00-12:30 = 30 min; trabajado = 7h30m = 450 min.
        var wd = new Workday
        {
            CheckIn = Utc(9, 0),
            CheckOut = Utc(17, 0),
            Status = WorkdayStatus.Completed
        };
        wd.Breaks.Add(new Break { StartTime = Utc(12, 0), EndTime = Utc(12, 30) });

        wd.WorkedDuration(DateTime.UtcNow).TotalMinutes.Should().Be(450);
    }

    [Fact]
    public void WorkedDuration_MultipleBreaks_AreAllSubtracted()
    {
        var wd = new Workday { CheckIn = Utc(8, 0), CheckOut = Utc(16, 0) }; // 480 min
        wd.Breaks.Add(new Break { StartTime = Utc(10, 0), EndTime = Utc(10, 15) }); // 15
        wd.Breaks.Add(new Break { StartTime = Utc(13, 0), EndTime = Utc(13, 45) }); // 45

        wd.WorkedDuration(DateTime.UtcNow).TotalMinutes.Should().Be(480 - 60);
    }

    [Fact]
    public void WorkedDuration_OpenWorkday_CountsUntilAsOf()
    {
        var wd = new Workday { CheckIn = Utc(9, 0), CheckOut = null, Status = WorkdayStatus.Open };
        // Sin salida: se calcula hasta asOf (11:00) => 120 min.
        wd.WorkedDuration(Utc(11, 0)).TotalMinutes.Should().Be(120);
    }

    [Fact]
    public void WorkedDuration_OpenBreak_CountsUntilAsOf()
    {
        var wd = new Workday { CheckIn = Utc(9, 0), CheckOut = null, Status = WorkdayStatus.Open };
        wd.Breaks.Add(new Break { StartTime = Utc(10, 0), EndTime = null }); // descanso en curso
        // A las 11:00: bruto 120 min - descanso 60 min = 60 min.
        wd.WorkedDuration(Utc(11, 0)).TotalMinutes.Should().Be(60);
    }

    [Fact]
    public void WorkedDuration_NeverNegative()
    {
        var wd = new Workday { CheckIn = Utc(9, 0), CheckOut = Utc(9, 30) };
        wd.Breaks.Add(new Break { StartTime = Utc(9, 0), EndTime = Utc(10, 0) }); // descanso mayor que la jornada
        wd.WorkedDuration(DateTime.UtcNow).TotalMinutes.Should().Be(0);
    }

    [Fact]
    public void HasOpenBreak_ReflectsOpenBreak()
    {
        var wd = new Workday { CheckIn = Utc(9, 0) };
        wd.HasOpenBreak.Should().BeFalse();
        wd.Breaks.Add(new Break { StartTime = Utc(10, 0), EndTime = null });
        wd.HasOpenBreak.Should().BeTrue();
    }
}
