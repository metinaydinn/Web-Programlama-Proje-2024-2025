namespace WebProgramlamaProje.Models
{
    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Duration { get; set; }

        public int SalonId { get; set; }
        public Salon? Salon { get; set; }
    }
}
