namespace HRIA.Domain.Entities;

/// <summary>
/// Calendario laboral de la empresa para un año natural: qué días de la semana
/// no se trabaja (habitualmente sábado y domingo) y qué días concretos son
/// festivos. Es la referencia para decidir si una fecha es laborable.
/// </summary>
public class WorkCalendar
{
    /// <summary>Sábado y domingo: el valor por defecto si nadie configura nada.</summary>
    public const int DefaultNonWorkingMask = (1 << (int)DayOfWeek.Saturday) | (1 << (int)DayOfWeek.Sunday);

    public int Id { get; set; }

    public int Year { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Días de la semana no laborables, como máscara de bits: el bit N
    /// corresponde a <see cref="DayOfWeek"/> N (0 = domingo … 6 = sábado).
    /// Se guarda así, y no como tabla aparte, porque son siempre 7 valores
    /// fijos y evita una unión en cada cálculo de días laborables.
    /// </summary>
    public int NonWorkingWeekDaysMask { get; set; } = DefaultNonWorkingMask;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Holiday> Holidays { get; set; } = new List<Holiday>();

    /// <summary>¿Ese día de la semana es no laborable para la empresa?</summary>
    public bool IsNonWorkingWeekDay(DayOfWeek day) => (NonWorkingWeekDaysMask & (1 << (int)day)) != 0;

    /// <summary>Marca o desmarca un día de la semana como no laborable.</summary>
    public void SetNonWorkingWeekDay(DayOfWeek day, bool nonWorking)
    {
        var bit = 1 << (int)day;
        NonWorkingWeekDaysMask = nonWorking
            ? NonWorkingWeekDaysMask | bit
            : NonWorkingWeekDaysMask & ~bit;
    }

    /// <summary>Días de la semana marcados como no laborables.</summary>
    public IEnumerable<DayOfWeek> NonWorkingWeekDays =>
        Enum.GetValues<DayOfWeek>().Where(IsNonWorkingWeekDay);
}
