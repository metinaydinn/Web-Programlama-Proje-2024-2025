namespace WebProgramlamaProje.Models
{
    public class WorkingHour
    {
        public int Id { get; set; }

        // Gün adı (örneğin: Pazartesi, Salı)
        public string DayOfWeek { get; set; }

        // Başlangıç saati
        public string? StartTime { get; set; }

        // Bitiş saati
        public string? EndTime { get; set; }

        // Salon ile ilişki (isteğe bağlı)
        public int? SalonId { get; set; }
        public Salon? Salon { get; set; }

        // Çalışan ile ilişki (isteğe bağlı)
        public int? EmployeeId { get; set; } // Employee ile ilişki için yabancı anahtar
        public Employee? Employee { get; set; } // Employee ile navigasyon özelliği
    }
}
