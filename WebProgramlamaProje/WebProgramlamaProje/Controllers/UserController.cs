using Microsoft.AspNetCore.Mvc;
using WebProgramlamaProje.Models;

namespace WebProgramlamaProje.Controllers
{
    public class UserController : Controller
    {
        private readonly SalonDbContext _context;

        public UserController(SalonDbContext context)
        {
            _context = context;
        }

        // Giriş ekranı (Admin Girişi, Üye Girişi, Kayıt Ol seçenekleri)
        [HttpGet]
        public IActionResult Login()
        {
            var userRole = HttpContext.Session.GetString("UserRole");

            // Eğer kullanıcı giriş yapmışsa, rolüne göre yönlendir
            if (userRole == "Admin")
            {
                return RedirectToAction("Index", "Salon"); // Admin için Salon ekranına yönlendirme
            }
            else if (userRole == "Customer")
            {
                return RedirectToAction("UserDashboard", "User"); // Üye için Kullanıcı Paneline yönlendirme
            }
            return View();
        }

        // Admin Girişi Sayfası (GET)
        [HttpGet]
        public IActionResult AdminLogin()
        {
            return View();
        }

        // Admin Girişi İşlemi (POST)
        [HttpPost]
        public IActionResult AdminLogin(string email, string password)
        {
            const string adminEmail = "g221210383@sakarya.edu.tr";
            const string adminPassword = "sau";

            if (email == adminEmail && password == adminPassword)
            {
                // Admin session bilgileri kaydediliyor
                HttpContext.Session.SetString("UserRole", "Admin");
                HttpContext.Session.SetString("UserEmail", email);
                ViewData["Message"] = "Admin girişi başarılı!";

                return RedirectToAction("Index", "Salon"); // Admin'i Salon ekranına yönlendir
            }

            TempData["Error"] = "Geçersiz admin kullanıcı adı veya şifre!";
            return View();
        }

        // Üye Girişi Sayfası (GET)
        [HttpGet]
        public IActionResult UserLogin()
        {
            return View();
        }

        // Üye Girişi İşlemi (POST)
        [HttpPost]
        public IActionResult UserLogin(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
            if (user != null)
            {
                // Kullanıcı session bilgileri kaydediliyor
                HttpContext.Session.SetString("UserRole", "Customer");
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserName", user.Name);
                ViewData["Message"] = "Üye girişi başarılı!";
                return RedirectToAction("UserDashboard", "User"); // Kullanıcıyı kendi paneline yönlendir
            }

            TempData["Error"] = "Geçersiz üye kullanıcı adı veya şifre!";
            return View();
        }

        // Kayıt Ol Sayfası (GET)
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // Kayıt Ol İşlemi (POST)
        [HttpPost]
        public IActionResult Register(string name, string email, string password, string phone)
        {
            if (_context.Users.Any(u => u.Email == email))
            {
                TempData["Error"] = "Bu e-posta zaten kayıtlı!";
                return RedirectToAction("Register");
            }

            // Yeni kullanıcı oluşturuluyor
            var newUser = new User
            {
                Name = name,
                Email = email,
                Password = password,
                Phone = phone,
                Role = "Customer" // Varsayılan olarak müşteri
            };

            _context.Users.Add(newUser);
            _context.SaveChanges();

            TempData["Message"] = "Kayıt başarılı! Şimdi giriş yapabilirsiniz.";
            return RedirectToAction("Login");
        }

        // Çıkış İşlemi
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Tüm session verileri temizleniyor
            TempData["Message"] = "Başarıyla çıkış yaptınız!";
            return RedirectToAction("Login");
        }

        // Yetkisiz Erişim için Yönlendirme (Opsiyonel)
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Kullanıcı Paneli (Üye Girişi sonrası gösterilecek sayfa)
        [HttpGet]
        public IActionResult UserDashboard()
        {
            if (HttpContext.Session.GetString("UserRole") != "Customer")
            {
                TempData["Error"] = "Bu sayfaya erişim yetkiniz yok!";
                return RedirectToAction("Login");
            }

            return View();
        }
    }
}
