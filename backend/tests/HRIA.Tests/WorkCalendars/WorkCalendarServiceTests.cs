using HRIA.Application.Common.Exceptions;
using HRIA.Application.WorkCalendars;
using HRIA.Application.WorkCalendars.Dtos;
using HRIA.Domain.Enums;
using HRIA.Tests.Common;

namespace HRIA.Tests.WorkCalendars;

public class WorkCalendarServiceTests
{
    private static WorkCalendarService CreateService() =>
        new(TestDb.Create(), FakeCurrentUser.Admin());

    private static CreateWorkCalendarRequest Calendar2026(params DayOfWeek[] nonWorking) =>
        new(2026, "Calendario 2026",
            nonWorking.Length > 0 ? nonWorking : new[] { DayOfWeek.Saturday, DayOfWeek.Sunday });

    [Fact]
    public async Task CreateAsync_PorDefecto_MarcaSabadoYDomingo()
    {
        var svc = CreateService();

        var created = await svc.CreateAsync(Calendar2026());

        Assert.Contains(DayOfWeek.Saturday, created.NonWorkingWeekDays);
        Assert.Contains(DayOfWeek.Sunday, created.NonWorkingWeekDays);
        Assert.Equal(2, created.NonWorkingWeekDays.Count);
    }

    [Fact]
    public async Task CreateAsync_AñoDuplicado_LanzaConflicto()
    {
        var svc = CreateService();
        await svc.CreateAsync(Calendar2026());

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.CreateAsync(Calendar2026()));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_SieteDiasNoLaborables_LanzaBadRequest()
    {
        var svc = CreateService();
        var todos = Enum.GetValues<DayOfWeek>();

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.CreateAsync(Calendar2026(todos)));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_CentroQueTrabajaSabado_SoloMarcaDomingo()
    {
        var svc = CreateService();

        var created = await svc.CreateAsync(Calendar2026(DayOfWeek.Sunday));

        Assert.Equal(new[] { DayOfWeek.Sunday }, created.NonWorkingWeekDays);
    }

    [Fact]
    public async Task AddHolidayAsync_FueraDelAñoDelCalendario_LanzaBadRequest()
    {
        var svc = CreateService();
        var calendar = await svc.CreateAsync(Calendar2026());

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.AddHolidayAsync(
            calendar.Id, new HolidayInput(new DateOnly(2027, 1, 1), "Año nuevo", HolidayKind.Nacional)));

        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task AddHolidayAsync_FechaRepetida_LanzaConflicto()
    {
        var svc = CreateService();
        var calendar = await svc.CreateAsync(Calendar2026());
        await svc.AddHolidayAsync(calendar.Id, new HolidayInput(new DateOnly(2026, 1, 1), "Año nuevo", HolidayKind.Nacional));

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.AddHolidayAsync(
            calendar.Id, new HolidayInput(new DateOnly(2026, 1, 1), "Otro", HolidayKind.Convenio)));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task AddHolidayAsync_DiaDeConvenio_SeGuardaConSuTipo()
    {
        var svc = CreateService();
        var calendar = await svc.CreateAsync(Calendar2026());

        var holiday = await svc.AddHolidayAsync(
            calendar.Id, new HolidayInput(new DateOnly(2026, 5, 4), "Puente de convenio", HolidayKind.Convenio));

        Assert.Equal(HolidayKind.Convenio, holiday.Kind);
    }

    [Fact]
    public async Task GetByYearAsync_DescuentaFestivosDeLosDiasLaborables()
    {
        var svc = CreateService();
        var calendar = await svc.CreateAsync(Calendar2026());
        var sinFestivos = (await svc.GetByYearAsync(2026)).WorkingDaysInYear;

        // 1 de enero de 2026 es jueves: descuenta un día laborable.
        await svc.AddHolidayAsync(calendar.Id, new HolidayInput(new DateOnly(2026, 1, 1), "Año nuevo", HolidayKind.Nacional));
        var conFestivo = (await svc.GetByYearAsync(2026)).WorkingDaysInYear;

        Assert.Equal(sinFestivos - 1, conFestivo);
    }

    [Fact]
    public async Task GetByYearAsync_FestivoEnFinDeSemana_NoDescuentaDosVeces()
    {
        var svc = CreateService();
        var calendar = await svc.CreateAsync(Calendar2026());
        var sinFestivos = (await svc.GetByYearAsync(2026)).WorkingDaysInYear;

        // 3 de enero de 2026 es sábado: ya era no laborable.
        await svc.AddHolidayAsync(calendar.Id, new HolidayInput(new DateOnly(2026, 1, 3), "Festivo en sábado", HolidayKind.Local));
        var conFestivo = (await svc.GetByYearAsync(2026)).WorkingDaysInYear;

        Assert.Equal(sinFestivos, conFestivo);
    }

    [Fact]
    public async Task GetYearDaysAsync_DevuelveTodosLosDiasDelAño()
    {
        var svc = CreateService();
        await svc.CreateAsync(Calendar2026());

        var days = await svc.GetYearDaysAsync(2026);

        Assert.Equal(365, days.Count);
        Assert.Equal(new DateOnly(2026, 1, 1), days[0].Date);
        Assert.Equal(new DateOnly(2026, 12, 31), days[^1].Date);
    }

    [Fact]
    public async Task GetYearDaysAsync_AñoBisiesto_Devuelve366Dias()
    {
        var svc = CreateService();

        var days = await svc.GetYearDaysAsync(2028);

        Assert.Equal(366, days.Count);
    }

    [Fact]
    public async Task GetYearDaysAsync_SinCalendarioDefinido_UsaFinDeSemanaPorDefecto()
    {
        var svc = CreateService();

        // No se crea calendario: la vista anual debe poder pintarse igualmente.
        var days = await svc.GetYearDaysAsync(2026);
        var sabado = days.First(d => d.Date == new DateOnly(2026, 1, 3));
        var jueves = days.First(d => d.Date == new DateOnly(2026, 1, 1));

        Assert.True(sabado.IsWeekend);
        Assert.False(sabado.IsWorkingDay);
        Assert.True(jueves.IsWorkingDay);
    }

    [Fact]
    public async Task GetYearDaysAsync_MarcaElFestivoConSuNombreYTipo()
    {
        var svc = CreateService();
        var calendar = await svc.CreateAsync(Calendar2026());
        await svc.AddHolidayAsync(calendar.Id, new HolidayInput(new DateOnly(2026, 5, 4), "Puente de convenio", HolidayKind.Convenio));

        var days = await svc.GetYearDaysAsync(2026);
        var day = days.First(d => d.Date == new DateOnly(2026, 5, 4));

        Assert.False(day.IsWorkingDay);
        Assert.False(day.IsWeekend);
        Assert.Equal("Puente de convenio", day.HolidayName);
        Assert.Equal(HolidayKind.Convenio, day.HolidayKind);
    }

    [Fact]
    public async Task RemoveHolidayAsync_VuelveAContarComoLaborable()
    {
        var svc = CreateService();
        var calendar = await svc.CreateAsync(Calendar2026());
        var holiday = await svc.AddHolidayAsync(
            calendar.Id, new HolidayInput(new DateOnly(2026, 1, 1), "Año nuevo", HolidayKind.Nacional));

        await svc.RemoveHolidayAsync(calendar.Id, holiday.Id);
        var days = await svc.GetYearDaysAsync(2026);

        Assert.True(days.First(d => d.Date == new DateOnly(2026, 1, 1)).IsWorkingDay);
    }
}
