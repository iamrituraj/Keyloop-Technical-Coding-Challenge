namespace Keyloop.Scheduler.Domain.Entities;

public class Dealership
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string OpeningTime { get; set; } = "08:00"; // "HH:mm"
    public string ClosingTime { get; set; } = "18:00"; // "HH:mm"
    public int SlotIntervalMinutes { get; set; } = 30;
    public string Timezone { get; set; } = "Asia/Kolkata"; // IANA Timezone

    public ICollection<ServiceBay> ServiceBays { get; set; } = new List<ServiceBay>();
    public ICollection<Technician> Technicians { get; set; } = new List<Technician>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public (DateTime Open, DateTime Close) GetOperatingWindow(DateTime date)
    {
        var openParts = OpeningTime.Split(':');
        var closeParts = ClosingTime.Split(':');
        var dayOpen = date.Date.AddHours(int.Parse(openParts[0])).AddMinutes(int.Parse(openParts[1]));
        var dayClose = date.Date.AddHours(int.Parse(closeParts[0])).AddMinutes(int.Parse(closeParts[1]));
        return (dayOpen, dayClose);
    }

    public bool IsWithinOperatingHours(DateTime start, DateTime end)
    {
        var (dayOpen, dayClose) = GetOperatingWindow(start);
        return start >= dayOpen && end <= dayClose;
    }
}
