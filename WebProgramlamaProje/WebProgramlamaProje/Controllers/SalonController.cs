﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebProgramlamaProje.Models;

namespace WebProgramlamaProje.Controllers
{
    public class SalonController : Controller
    {
        private readonly SalonDbContext _context;

        public SalonController(SalonDbContext context)
        {
            _context = context;
        }

        // Salonları listeleme
        public IActionResult Index()
        {
            var salons = _context.Salons.ToList();
            return View(salons);
        }

        // Yeni salon ekleme (GET)
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // Yeni salon ekleme (POST)
        [HttpPost]
        public IActionResult Create(Salon salon)
        {
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
            // Salon ve ilişkili hizmetleri yükle
            var salon = _context.Salons
                                .Include(s => s.Services)
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
            if (ModelState.IsValid)
            {
                // Veritabanından mevcut salon ve ilişkili hizmetleri yükle
                var existingSalon = _context.Salons
                                            .Include(s => s.Services)
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

                // Değişiklikleri kaydet
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            // Eğer doğrulama hatası varsa yeniden sayfayı döndür
            return View(salon);
        }



        



        // Salon silme
        public IActionResult Delete(int id)
        {
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
