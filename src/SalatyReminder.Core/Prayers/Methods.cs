namespace SalatyReminder.Core.Prayers;

public static class Methods
{
    public static readonly MethodDef[] All =
    [
        new("Egyptian", 19.5, 17.5, 0, "Egyptian General Authority of Survey", "الهيئة المصرية العامة للمساحة"),
        new("MWL", 18, 17, 0, "Muslim World League", "رابطة العالم الإسلامي"),
        new("UmmAlQura", 18.5, 0, 90, "Umm al-Qura, Makkah", "أم القرى، مكة المكرمة"),
        new("Karachi", 18, 18, 0, "University of Islamic Sciences, Karachi", "جامعة العلوم الإسلامية، كراتشي"),
        new("ISNA", 15, 15, 0, "Islamic Society of North America", "الجمعية الإسلامية لأمريكا الشمالية"),
        new("Kuwait", 18, 17.5, 0, "Kuwait", "الكويت"),
        new("Qatar", 18, 0, 90, "Qatar", "قطر"),
        new("Dubai", 18.2, 18.2, 0, "Dubai", "دبي"),
        new("Turkey", 18, 17, 0, "Diyanet, Turkey", "رئاسة الشؤون الدينية، تركيا"),
    ];

    public static MethodDef Get(string key) => All.FirstOrDefault(m => m.Key == key) ?? All[0];
}
