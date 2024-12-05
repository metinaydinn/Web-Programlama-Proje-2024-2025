namespace WebProgramlamaProje.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } // Kullanıcı adı
        public string Email { get; set; } // Kullanıcı e-posta adresi
        public string Phone { get; set; } // Kullanıcı telefon numarası
        public string Password { get; set; } // Kullanıcı şifresi
        public string Role { get; set; } // Kullanıcı rolü (Admin veya Customer)
        public List<Appointment> Appointments { get; set; } // Randevular ile ilişki
    }

}
