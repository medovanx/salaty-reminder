namespace SalatyReminder.Core.Prayers;

public sealed class DayTimes(DateOnly date, DateTime[] times)
{
    public DateOnly Date { get; } = date;
    public DateTime this[Prayer p] => times[(int)p];
}
