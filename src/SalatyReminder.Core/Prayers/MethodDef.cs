namespace SalatyReminder.Core.Prayers;

/// <summary>A calculation method: twilight angles for Fajr and Isha, or a fixed Isha delay.</summary>
/// <param name="IshaMinutes">When set, Isha is this many minutes after Maghrib instead of an angle.</param>
public sealed record MethodDef(string Key, double FajrAngle, double IshaAngle, int IshaMinutes, string NameEn, string NameAr);
