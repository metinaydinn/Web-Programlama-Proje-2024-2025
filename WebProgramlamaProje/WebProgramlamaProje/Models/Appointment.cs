namespace WebProgramlamaProje.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }

        // İlişkiler
        public int SalonId { get; set; }
        public Salon Salon { get; set; }
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }
        public int ServiceId { get; set; }
        public Service Service { get; set; }
        public int CustomerId { get; set; } // Müşteri ilişkisi
        public Customer Customer { get; set; } // Navigation property Entity Framework, navigation property'leri kullanarak veritabanında ilişkiler (foreign key bağlantıları) oluşturur.
    }
}
