using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class HairAIService
{
    private readonly string _apiUrl;

    public HairAIService(string apiUrl)
    {
        _apiUrl = apiUrl; // Flask API URL'si
    }

    public async Task<string> AnalyzePhotoAsync(string filePath)
    {
        using (var httpClient = new HttpClient())
        {
            // Dosya verisini göndermek için form-data hazırla
            var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(filePath));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            form.Add(fileContent, "file", "uploaded_image.jpg");

            // POST isteği gönder
            var response = await httpClient.PostAsync(_apiUrl + "/analyze", form);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"API çağrısı başarısız: {response.StatusCode}");
            }

            // Yanıtı JSON olarak al ve işle
            var jsonResponse = await response.Content.ReadAsStringAsync();
            return jsonResponse;
        }
    }
}
