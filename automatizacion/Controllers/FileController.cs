using Microsoft.AspNetCore.Mvc;

using System.Net.Http.Headers;
using automatizacion.Models;
using System.Text.Json;
using System.Text;
namespace automatizacion.Controllers
{
    public class FileController : Controller
    {
        private readonly HttpClient _httpClient;
        private static byte[] archivoTemporal;
        public FileController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Selecciona un archivo CSV.");
            }
            // Guardar el archivo en la variable estática (memoria)
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                archivoTemporal = memoryStream.ToArray(); // Guarda el archivo en la variable estática
            }
            using var content = new MultipartFormDataContent();
            using var stream = new MemoryStream(archivoTemporal);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");


            content.Add(fileContent, "file", file.FileName);

            HttpResponseMessage response = await _httpClient.PostAsync("http://127.0.0.1:8000/uploadexcel", content);

            if (!response.IsSuccessStatusCode)
            {
                return BadRequest("Error al enviar el archivo.");
            }

            string result = await response.Content.ReadAsStringAsync();
            List<string> headers = JsonSerializer.Deserialize<List<string>>(result);


            FileCsv filecsv = new FileCsv();
            //List<string> headers = new List<string> { "Rank", "State", "Postal", "Population" };
            filecsv.header = headers;
            return View(filecsv);

        }
        [HttpPost]
        public async Task<IActionResult> GuardarDividir(Dictionary<string, string> seleccionados)
        {
            var resultado = new Dictionary<string, List<string>>();

            foreach (var item in seleccionados)
            {
                string hoja = item.Value;
                if (!resultado.ContainsKey(hoja))
                {
                    resultado[hoja] = new List<string>();
                }
                resultado[hoja].Add(item.Key);
            }

            // Convierte a JSON y lo devuelve
            string jsonData = JsonSerializer.Serialize(resultado);
            using var content = new MultipartFormDataContent();

            // Agregar el JSON al formulario como texto
            var jsonContent = new StringContent(jsonData, Encoding.UTF8, "application/json");
            content.Add(jsonContent, "columnas_hoja");

            // Agregar el archivo al formulario
            using var stream = new MemoryStream(archivoTemporal);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            content.Add(fileContent, "file", "archivo.xlsx");

            // URL de FastAPI
            string fastApiUrl = "http://localhost:8000/dividir";

            // Enviar POST a FastAPI
            HttpResponseMessage response = await _httpClient.PostAsync(fastApiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                // Recibir archivo de respuesta
                byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "archivo_resultado.xlsx");
            }
            else
            {
                return BadRequest("Error al generar el archivo en FastAPI.");
            }
        }
    }
}