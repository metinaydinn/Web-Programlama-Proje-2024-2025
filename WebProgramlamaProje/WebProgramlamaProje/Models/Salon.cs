using System.ComponentModel.DataAnnotations;

namespace WebProgramlamaProje.Models
{
    public class Salon
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Salon adı gereklidir.")]
        [StringLength(100, ErrorMessage = "Salon adı en fazla 100 karakter olabilir.")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Adres gereklidir.")]
        [StringLength(200, ErrorMessage = "Adres en fazla 200 karakter olabilir.")]
        public string Address { get; set; }
        [Required(ErrorMessage = "Telefon numarası gereklidir.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Telefon numarası 10 haneli olmalıdır (örneğin: 5551234567).")]
        public string Phone { get; set; }
        public List<Appointment> Appointments { get; set; } = new List<Appointment>();
        public List<Service> Services { get; set; } = new List<Service>();
        public List<Employee> Employees { get; set; } = new List<Employee>();
        public List<WorkingHour> WorkingHours { get; set; } = new List<WorkingHour>();
    }
}
