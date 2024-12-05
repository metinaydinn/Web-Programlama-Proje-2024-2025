using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebProgramlamaProje.Models;

namespace WebProgramlamaProje.Controllers
{
    public class SalonController : Controller
    {
        private readonly SalonDbContext _context;
        // Admin yetkilendirme kontrolü
        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin";
        }
        public SalonController(SalonDbContext context)
        {
            _context = context;
        }

        // Salonları listeleme
        public IActionResult Index()
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Bu sayfaya erişim yetkiniz yok!";
                return RedirectToAction("Login", "User"); // Giriş ekranına yönlendirme
            }
            var salons = _context.Salons.ToList();
            return View(salons);
        }

        // Yeni salon ekleme (GET)
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Bu sayfaya erişim yetkiniz yok!";
                return RedirectToAction("Login", "User"); // Giriş ekranına yönlendirme
            }
            return View();
        }

        // Yeni salon ekleme (POST)
        [HttpPost]
        public IActionResult Create(Salon salon)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Bu sayfaya erişim yetkiniz yok!";
                return RedirectToAction("Login", "User"); // Giriş ekranına yönlendirme
            }
            if (salon.Services != null)
            {
                foreach (var service in salon.Services)
                {
                    // Her bir service nesnesine ilgili salon atanır
                    service.Salon = salon;
                }
            }
            if (salon.WorkingHours != null)
            {
                foreach (var workingHour in salon.WorkingHours)
                {
                    if (string.IsNullOrEmpty(workingHour.StartTime) || string.IsNullOrEmpty(workingHour.EndTime))
                    {
                        workingHour.StartTime = "BERBER KAPALI";
                        workingHour.EndTime = "BERBER KAPALI";
                    }
                    workingHour.Salon = salon; // İlişkili salonu atıyoruz
                }
            }


            // Çalışanlara 'Salon' ilişkisinin atanması
            if (salon.Employees != null)
            {
                foreach (var employee in salon.Employees)
                {
                    employee.Salon = salon;

                    // Çalışan için çalışma saatlerini kontrol et
                    if (employee.WorkingHours != null)
                    {
                        foreach (var workingHour in employee.WorkingHours)
                        {
                            workingHour.EmployeeId = employee.Id; // Çalışan ile ilişki
                        }
                    }
                }
            }

            // SalonId'nin 5 basamaktan oluştuğunu kontrol et
            if (salon.Id.ToString().Length != 5)
            {
                ModelState.AddModelError("Id", "Salon ID'si 5 basamaklı olmalıdır.");
                return View(salon); // Hata varsa tekrar formu göster
            }

            // Veritabanında aynı ID'ye sahip bir salon var mı kontrol et
            var existingSalon = _context.Salons.FirstOrDefault(s => s.Id == salon.Id);
            if (existingSalon != null)
            {
                ModelState.AddModelError("Id", "Bu Salon ID'si zaten mevcut. Lütfen farklı bir ID girin.");
                return View(salon); // Hata varsa tekrar formu göster
            }

            // Koleksiyonların null olmaması için boş liste olarak başlatma
            /*salon.Services ??= new List<Service>();
            salon.Employees ??= new List<Employee>();
            salon.WorkingHours ??= new List<WorkingHour>();
            salon.Appointments ??= new List<Appointment>();*/
            // Her bir çalışan için WorkingHours boş liste olarak başlatılır
            foreach (var employee in salon.Employees)
            {
                employee.WorkingHours ??= new List<WorkingHour>();
            }

            // ModelState geçerliyse salonu ekle
            if (ModelState.IsValid)
            {
                _context.Salons.Add(salon);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(salon); // Hata varsa tekrar formu göster
        }

        // Salon detayları
        public IActionResult Details(int id)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Bu sayfaya erişim yetkiniz yok!";
                return RedirectToAction("Login", "User"); // Giriş ekranına yönlendirme
            }
            var salon = _context.Salons
                .Include(s => s.Services) // Hizmetleri dahil et
                .Include(s => s.Employees) // Çalışanları dahil et
                .Include(s => s.WorkingHours) // Çalışanları dahil et
                .FirstOrDefault(s => s.Id == id);

            if (salon == null)
            {
                return NotFound($"Salon ID {id} bulunamadı.");
            }

            return View(salon);
        }

        // Salon düzenleme (GET)
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Bu sayfaya erişim yetkiniz yok!";
                return RedirectToAction("Login", "User"); // Giriş ekranına yönlendirme
            }

            // Salon ve ilişkili hizmetleri yükle
            var salon = _context.Salons
                                .Include(s => s.Services)
                                .Include(s => s.Employees) // Çalışanları yükle
                                .FirstOrDefault(s => s.Id == id);

            if (salon == null)
            {
                return NotFound($"Salon ID {id} bulunamadı.");
            }

            // Eğer yeni bir hizmet eklenmesi bekleniyorsa, boş bir liste tanımlayabiliriz
            if (salon.Services == null)
            {
                salon.Services = new List<Service>();
            }

            return View(salon);
        }


        // Salon düzenleme (POST)
        [HttpPost]
        public IActionResult Edit(Salon salon)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Bu sayfaya erişim yetkiniz yok!";
                return RedirectToAction("Login", "User"); // Giriş ekranına yönlendirme
            }
            if (ModelState.IsValid)
            {
                // Veritabanından mevcut salon ve ilişkili hizmetleri yükle
                var existingSalon = _context.Salons
                                            .Include(s => s.Services)
                                            .Include(s => s.Employees) // Çalışanları yükle
                                            .FirstOrDefault(s => s.Id == salon.Id);

                if (existingSalon == null)
                {
                    return NotFound($"Salon ID {salon.Id} bulunamadı.");
                }

                // Salon temel bilgilerini güncelle
                existingSalon.Name = salon.Name;
                existingSalon.Address = salon.Address;
                existingSalon.Phone = salon.Phone;

                // Mevcut hizmetleri güncelle veya yeni hizmetleri ekle
                foreach (var service in salon.Services)
                {
                    if (service.Id == 0)
                    {
                        // Yeni hizmet ekle
                        existingSalon.Services.Add(new Service
                        {
                            Name = service.Name,
                            Price = service.Price,
                            Duration = service.Duration,
                            SalonId = salon.Id
                        });
                    }
                    else
                    {
                        // Mevcut hizmeti bul ve güncelle
                        var existingService = existingSalon.Services.FirstOrDefault(s => s.Id == service.Id);

                        if (existingService != null)
                        {
                            existingService.Name = service.Name;
                            existingService.Price = service.Price;
                            existingService.Duration = service.Duration;
                        }
                    }
                }

                // Mevcut olmayan hizmetleri sil
                var incomingServiceIds = salon.Services.Select(s => s.Id).ToList();
                var servicesToRemove = existingSalon.Services
                                                    .Where(s => !incomingServiceIds.Contains(s.Id))
                                                    .ToList();

                foreach (var serviceToRemove in servicesToRemove)
                {
                    _context.Services.Remove(serviceToRemove);
                }

                // Mevcut çalışanları güncelle veya yeni çalışanları ekle
                foreach (var employee in salon.Employees)
                {
                    if (employee.Id == 0)
                    {
                        // Yeni çalışan ekle
                        existingSalon.Employees.Add(new Employee
                        {
                            Name = employee.Name,
                            Specialty = employee.Specialty,
                            Skills = employee.Skills,
                            SalonId = salon.Id
                        });
                    }
                    else
                    {
                        // Mevcut çalışanı bul ve güncelle
                        var existingEmployee = existingSalon.Employees.FirstOrDefault(e => e.Id == employee.Id);

                        if (existingEmployee != null)
                        {
                            existingEmployee.Name = employee.Name;
                            existingEmployee.Specialty = employee.Specialty;
                            existingEmployee.Skills = employee.Skills;
                        }
                    }
                }

                // Mevcut olmayan çalışanları sil
                var incomingEmployeeIds = salon.Employees.Select(e => e.Id).ToList();
                var employeesToRemove = existingSalon.Employees
                                                      .Where(e => !incomingEmployeeIds.Contains(e.Id))
                                                      .ToList();

                foreach (var employeeToRemove in employeesToRemove)
                {
                    _context.Employees.Remove(employeeToRemove);
                }


                // Değişiklikleri kaydet
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            // Eğer doğrulama hatası varsa yeniden sayfayı döndür
            return View(salon);
        }



        [HttpPost]
        public IActionResult DeleteService([FromBody] int serviceId)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Bu sayfaya erişim yetkiniz yok!";
                return RedirectToAction("Login", "User"); // Giriş ekranına yönlendirme
            }
            // Veritabanından hizmeti bul
            var service = _context.Services.FirstOrDefault(s => s.Id == serviceId);

            if (service != null)
            {
                // Hizmeti sil
                _context.Services.Remove(service);
                _context.SaveChanges();

                // Silme başarılı
                return Json(new { success = true, message = "Hizmet başarıyla silindi." });
            }

            // Hizmet bulunamadı
            return Json(new { success = false, message = "Hizmet bulunamadı." });
        }


        [HttpPost]
        public IActionResult DeleteEmployee([FromBody] int employeeId)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Bu sayfaya erişim yetkiniz yok!";
                return RedirectToAction("Login", "User"); // Giriş ekranına yönlendirme
            }
            var employee = _context.Employees.FirstOrDefault(e => e.Id == employeeId);

            if (employee != null)
            {
                _context.Employees.Remove(employee);
                _context.SaveChanges();

                return Json(new { success = true, message = "Çalışan başarıyla silindi." });
            }

            return Json(new { success = false, message = "Çalışan bulunamadı." });
        }





        // Salon silme
        public IActionResult Delete(int id)
        {
            if (!IsAdmin())
            {
                TempData["Error"] = "Bu sayfaya erişim yetkiniz yok!";
                return RedirectToAction("Login", "User"); // Giriş ekranına yönlendirme
            }
            var salon = _context.Salons.FirstOrDefault(s => s.Id == id);
            if (salon != null)
            {
                _context.Salons.Remove(salon);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}