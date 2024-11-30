namespace WebProgramlamaProje.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; } // Müşterinin Adı
        public string Email { get; set; } // Müşterinin E-posta Adresi
        public string Phone { get; set; } // Müşteri Telefon Numarası

        // Randevular ile ilişki
        public List<Appointment> Appointments { get; set; }
    }
}
