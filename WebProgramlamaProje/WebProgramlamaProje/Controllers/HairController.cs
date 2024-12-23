using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

public class HairController : Controller
{
    private readonly HairAIService _hairAIService;

    public HairController()
    {
        // Flask API'nin URL'sini veriyoruz.
        _hairAIService = new HairAIService("http://127.0.0.1:5000");
    }

    // GET: AnalyzeHair
    public IActionResult AnalyzeHair()
    {
        // Formun gösterilmesi için basit bir GET isteği.
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AnalyzeHair(IFormFile photo)
    {
        try
        {
            // Fotoğrafın yüklü olup olmadığını kontrol ediyoruz
            if (photo == null || photo.Length == 0)
            {
                return Json(new { success = false, message = "Dosya yüklenmedi." });
            }

            // Fotoğrafı geçici olarak kaydetmek için bir yol belirliyoruz
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            Directory.CreateDirectory(uploadsFolder); // Klasör yoksa oluştur
            var filePath = Path.Combine(uploadsFolder, photo.FileName);

            // Fotoğrafı kaydedin
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(stream);
            }

            // Flask API'ye fotoğrafı gönderin
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://127.0.0.1:5000/");
                var form = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(await System.IO.File.ReadAllBytesAsync(filePath));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                form.Add(fileContent, "file", Path.GetFileName(filePath));

                // Flask API'ye POST isteği gönderin
                var response = await client.PostAsync("analyze", form);

                // API çağrısı başarılı mı kontrol edin
                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { success = false, message = "Flask API çağrısı başarısız oldu." });
                }

                // Yanıt içeriğini okuyun
                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

                var hairModelDescriptions = new Dictionary<string, string>
            {
                { "styles/anime_shiny_black_hair.png", "Anime Parlak Siyah Saç" },
                { "styles/classic_black_hair.png", "Klasik Siyah Saç" },
                { "styles/classic_black_wave_hair.png", "Klasik Siyah Dalgalı Saç" },
                { "styles/classic_brown_hair.png", "Klasik Kahverengi Saç" },
                { "styles/classic2_black_wave_hair.png", "Klasik Siyah Dalgalı Saç 2" },
                { "styles/galactic_black_hair.png", "Galaksi Siyah Saç" },
                { "styles/light_brown_spiky_hair.png", "Açık Kahverengi Dikenli Saç" },
                { "styles/messy_brown_hair.png", "Dağınık Kahverengi Saç" },
                { "styles/modern_black_side_part.png", "Modern Siyah Yan Bölüm Saç" },
                { "styles/modern_layered_black_hair.png", "Modern Katmanlı Siyah Saç" },
                { "styles/platinum_spiky_hair.png", "Platin Dikenli Saç" },
                { "styles/slick_black_hair.png", "Siyah Düz Saç" },
                { "styles/slick_black_side_part_hair.png", "Siyah Yan Bölüm Düz Saç" },
                { "styles/slick_brown_hair.png", "Düz Kahverengi Saç" },
                { "styles/textured_fringe_black.png", "Dokulu Siyah Perçem Saç" },
                { "styles/wavy_dark_hair.png", "Dalgalı Koyu Saç" }
            };
             


                var faceShape = (string)jsonResponse.face_shape;
                var hairModel = (string)jsonResponse.hair_model;
                var hairColor = (string)jsonResponse.hair_color;
                string hairModelDescription = hairModelDescriptions.ContainsKey(hairModel)
                ? hairModelDescriptions[hairModel]
            :   hairModel; // Eğer sözlükte yoksa dosya adını kullan


                         

                // Yanıttan dönen dosyayı kaydedin
                var outputImageUrl = (string)jsonResponse.output_image;
                var outputImagePath = Path.Combine(uploadsFolder, "output_with_hair.png");

                using (var downloadClient = new HttpClient())
                {
                    var imageBytes = await downloadClient.GetByteArrayAsync(outputImageUrl);
                    await System.IO.File.WriteAllBytesAsync(outputImagePath, imageBytes);
                }

                // Yanıtı frontend'e döndürün
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        face_shape = faceShape,
                        hair_model = hairModelDescription,
                        hair_color = hairColor,
                        output_image = "/uploads/output_with_hair.png"
                    }
                });
            }
        }
        catch (Exception ex)
        {
            // Hata durumunda mesajı döndürün
            return Json(new { success = false, message = ex.Message });
        }
    }



}
