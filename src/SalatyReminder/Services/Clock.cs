namespace SalatyReminder.Services;

/// <summary>The time the UI shows. Always the real clock, except in --shots, which pins it to show a chosen moment.</summary>
static class Clock
{
    public static DateTime? Pinned { get; set; }
    public static DateTime Now => Pinned ?? DateTime.Now;
}
