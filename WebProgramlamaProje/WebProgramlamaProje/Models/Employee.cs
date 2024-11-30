namespace WebProgramlamaProje.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<string> Specialty { get; set; }
        public List<string> Skills { get; set; }
        public int SalonId { get; set; }
        public Salon? Salon { get; set; }
        public List<WorkingHour> WorkingHours { get; set; } = new List<WorkingHour>();

        public List<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
