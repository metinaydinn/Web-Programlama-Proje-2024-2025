using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebProgramlamaProje.Models;

namespace WebProgramlamaProje.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly SalonDbContext _context;

        public AppointmentController(SalonDbContext context)
        {
            _context = context;
        }

        // GET: Appointment/Create
        public IActionResult Create()
        {
            // Tüm salonları form için al
            ViewBag.Salons = _context.Salons.ToList();
            return View();
        }

        // POST: Dinamik olarak seçilen salonun çalışma saatlerini ve çalışanlarını getir
        [HttpPost]
        public JsonResult GetSalonDetails([FromBody] SalonRequest salonRequest)
        {
            var salonId = salonRequest.SalonId; // SalonId'yi modelden al
            Console.WriteLine($"Gelen Salon ID: {salonId}"); // Gelen salonId'yi logla

            // Salon verisini doğrula
            var salon = _context.Salons.FirstOrDefault(s => s.Id == salonId);
            if (salon == null)
            {
                return Json(new { success = false, message = "Salon bulunamadı." });
            }

            // Çalışma saatlerini al
            var workingHours = _context.WorkingHours
                .Where(wh => wh.SalonId == salonId)
                .Select(wh => new
                {
                    wh.DayOfWeek,
                    StartTime = wh.StartTime == "BERBER KAPALI" ? null : wh.StartTime,
                    EndTime = wh.EndTime == "BERBER KAPALI" ? null : wh.EndTime
                })
                .ToList();
            // Belirli bir salonun servislerini al
            var services = _context.Services
                .Where(s => s.SalonId == salonId)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Price,
                    s.Duration
                })
                .ToList();
            
            // Çalışanları al
            var employees = _context.Employees
                .Where(e => e.SalonId == salonId)
                .Select(e => new
                {
                    e.Id,
                    e.Name
                })
                .ToList();
            Console.WriteLine($"Çalışan Sayısı: {employees.Count}"); // Çalışan sayısını logla
            if (!workingHours.Any() && !employees.Any())
            {
                return Json(new { success = false, message = "Bu salon için veri bulunamadı." });
            }

            // JSON formatında verileri geri döndür
            return Json(new
            {
                success = true,
                workingHours,
                employees,
                services
            });
        }





        // POST: Appointment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment)
        {

            var userId = HttpContext.Session.GetString("UserId");
            Console.WriteLine("userId" + userId);
            appointment.CustomerId = int.Parse(userId);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "User");
            }

            // Türkçe gün adlarını DayOfWeek enum'a eşleştirmek için bir sözlük
            var dayMapping = new Dictionary<string, DayOfWeek>
    {
        { "Pazar", DayOfWeek.Sunday },
        { "Pazartesi", DayOfWeek.Monday },
        { "Salı", DayOfWeek.Tuesday },
        { "Çarşamba", DayOfWeek.Wednesday },
        { "Perşembe", DayOfWeek.Thursday },
        { "Cuma", DayOfWeek.Friday },
        { "Cumartesi", DayOfWeek.Saturday }
    };

            // Veritabanından çalışma saatlerini ve günlerini al
            var workingHours = _context.WorkingHours
                .Where(wh => wh.SalonId == appointment.SalonId)
                .AsEnumerable() // Veriyi belleğe al
                .Where(wh => dayMapping[wh.DayOfWeek] == appointment.Date.DayOfWeek) // Gün kontrolü
                .Select(wh => new
                {
                    wh.StartTime,
                    wh.EndTime
                })
                .FirstOrDefault();

            if (workingHours != null)
            {
                var appointmentTime = appointment.Date.TimeOfDay;

                // Çalışma saatlerini kontrol et
                if (TimeSpan.TryParse(workingHours.StartTime, out var startTime) &&
                    TimeSpan.TryParse(workingHours.EndTime, out var endTime))
                {
                    if (appointmentTime < startTime || appointmentTime > endTime)
                    {
                        ModelState.AddModelError("Date", "Randevu saati berberin çalışma saatleri dışında, lütfen uygun bir saat seçin.");
                    }
                }
                else
                {
                    ModelState.AddModelError("Date", "Çalışma saatleri geçersiz.");
                }
            }
            else
            {
                // Eğer çalışma saatleri bulunamadıysa (kapalı gün)
                ModelState.AddModelError("Date", "Bu gün berber kapalı, lütfen başka bir gün seçin.");
            }





            //            Console.WriteLine($"SalonId: {appointment.SalonId}");
            //          Console.WriteLine($"ServiceId: {appointment.ServiceId}");
            //      Console.WriteLine($"EmployeeId: {appointment.EmployeeId}");
            //    Console.WriteLine($"CustomerId: {appointment.CustomerId}");
            //  Console.WriteLine($"Date: {appointment.Date}");
            if (ModelState.IsValid)
            {
                Console.WriteLine("Randevu ekleniyor...");
                // Tarih değerini UTC olarak ayarlayın
                appointment.Date = DateTime.SpecifyKind(appointment.Date, DateTimeKind.Utc);
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                Console.WriteLine("Randevu başarıyla eklendi.");
                return RedirectToAction("UserDashboard", "User");
            }
            else
            {
                // Hatalar varsa bunları kontrol et
                Console.WriteLine("Model geçersiz, hata mesajlarını kontrol et.");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Hata: {error.ErrorMessage}");
                }
                ViewBag.Salons = _context.Salons.ToList();
                

                return View(appointment);
            }

        }

        // GET: Appointment/Index
        public async Task<IActionResult> Index()
        {
            // Randevuları ilişkili verilerle birlikte getir
            var appointments = await _context.Appointments
                .Include(a => a.Salon)
                .Include(a => a.Employee)
                .Include(a => a.Service)
                .Include(a => a.Customer)
                .ToListAsync();

            return View(appointments);
        }

        // GET: Appointment/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Salon)
                .Include(a => a.Employee)
                .Include(a => a.Service)
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // GET: Appointment/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var appointment = await _context.Appointments
                .Include(a => a.Salon)
                .Include(a => a.Employee)
                .Include(a => a.Service)
                .Include(a => a.Customer)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return NotFound();
            }

            return View(appointment);
        }

        // POST: Appointment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                _context.Appointments.Remove(appointment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
