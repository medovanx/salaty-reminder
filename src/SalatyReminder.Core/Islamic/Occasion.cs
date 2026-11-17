namespace SalatyReminder.Core.Islamic;

/// <summary>What a calendar day is: a named occasion, a sunnah fasting day, or both.</summary>
public enum OccasionKey
{
    IslamicNewYear, Tasua, Ashura, RamadanStart, LastTenNights, EidAlFitr,
    FirstTenDhulHijjah, Arafah, EidAlAdha, Tashreeq,
}

public enum FastKind { None, MondayThursday, WhiteDays, Ashura, Arafah, SixOfShawwal }

/// <param name="Occasion">The day's occasion, if it has one.</param>
/// <param name="Fast">A sunnah fast for the day; None on the days fasting is forbidden (the Eids and Tashreeq).</param>
public readonly record struct DayInfo(OccasionKey? Occasion, FastKind Fast)
{
    /// <summary>Eids and the days of Tashreeq, when fasting is not allowed.</summary>
    public bool FastingForbidden => Occasion is OccasionKey.EidAlFitr or OccasionKey.EidAlAdha or OccasionKey.Tashreeq;

    /// <summary>The big days, drawn as a dot on the calendar and named on the card.</summary>
    public bool IsMajor => Occasion is OccasionKey.IslamicNewYear or OccasionKey.Ashura or OccasionKey.RamadanStart
        or OccasionKey.EidAlFitr or OccasionKey.Arafah or OccasionKey.EidAlAdha;
}
