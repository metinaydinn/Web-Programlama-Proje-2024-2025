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
                    ModelState.AddModelError("Date", "Bu gün berber kapalı, lütfen başka bir gün seçin.");
                }
            }




            // Çakışma kontrolü
            if (IsAppointmentTimeConflict(appointment))
            {
                ModelState.AddModelError("Date", "Bu saat aralığında randevu mevcut. Lütfen farklı bir saat seçin.");
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
                // Çalışanın randevularına ekleme
                var employee = await _context.Employees
                                              .Include(e => e.Appointments) // Randevuları dahil edin
                                              .FirstOrDefaultAsync(e => e.Id == appointment.EmployeeId);
                if (employee != null)
                {
                    employee.Appointments.Add(appointment); // Çalışanın randevularına ekle
                    Console.WriteLine("Randevu çalışanın listesine eklendi.");
                }
                else
                {
                    Console.WriteLine("Çalışan bulunamadı.");
                }
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



        private bool IsAppointmentTimeConflict(Appointment appointment)
        {
            // İşlem süresini al
            var serviceDuration = _context.Services
                .Where(s => s.Id == appointment.ServiceId)
                .Select(s => s.Duration)
                .FirstOrDefault();

            if (serviceDuration <= 0)
            {
                Console.WriteLine("Geçersiz işlem süresi.");
                return false;
            }

            // Yeni randevunun başlangıç ve bitiş zamanlarını UTC olarak ayarla
            var appointmentStartTime = appointment.Date; // Yerel zaman
            var appointmentEndTime = appointmentStartTime.AddMinutes(serviceDuration + 10);
            Console.WriteLine("appointmentStartTime: " + appointmentStartTime);
            Console.WriteLine("appointmentEndTime: " + appointmentEndTime);
            // Aynı berber (EmployeeId) için mevcut randevuları getir
            var appointments = _context.Appointments
                .Include(a => a.Service)
                .Where(a => a.EmployeeId == appointment.EmployeeId) // Aynı berber
                .ToList(); // Veriyi belleğe alıyoruz

            // Mevcut randevularla çakışma kontrolü yap
            foreach (var a in appointments)
            {
                // Mevcut randevunun başlangıç ve bitiş zamanlarını hesapla
                var existingStartTime = a.Date.ToUniversalTime();
                var existingEndTime = existingStartTime.AddMinutes(a.Service != null ? a.Service.Duration + 10 : 10);
                Console.WriteLine("Başlangıç saati: " + existingStartTime);
                Console.WriteLine("Bitiş saati: " + existingEndTime);
                // Saat aralığı çakışma kontrolü
                if (appointmentStartTime < existingEndTime && appointmentEndTime > existingStartTime)
                {
                    Console.WriteLine($"Çakışan randevu: Başlangıç = {existingStartTime}, Bitiş = {existingEndTime}");
                    return true; // Çakışma tespit edildi
                }
            }

            return false; // Çakışma yok
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

        // GET: Appointment/MyAppointments
        public async Task<IActionResult> MyAppointments()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "User");
            }

            var appointments = await _context.Appointments
                .Include(a => a.Salon)
                .Include(a => a.Employee)
                .Include(a => a.Service)
                .Include(a => a.Customer)
                .Where(a => a.CustomerId.ToString() == userId)
                .ToListAsync();

            return View(appointments);
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
