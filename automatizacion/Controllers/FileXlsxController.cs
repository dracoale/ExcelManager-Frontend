using Microsoft.AspNetCore.Mvc;

using System.Net.Http.Headers;
using automatizacion.Models;

using System.Text;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Generic;
namespace automatizacion.Controllers
{
    public class FileXlsxController : Controller
    {
        private readonly HttpClient _httpClient;
        private List<FileXlsx> fileXlsxList;
        public FileXlsxController(HttpClient httpClient)
        {
            _httpClient = httpClient;
            fileXlsxList = new List<FileXlsx>();

        }

        //public override void OnActionExecuting(ActionExecutingContext context)
        //{
        //    base.OnActionExecuting(context);
        //    var usuarioJson = HttpContext.Session.GetString("FileXlsx");
        //    if (usuarioJson != null)
        //    {
        //        fileXlsx = JsonConvert.DeserializeObject<FileXlsx>(usuarioJson);
        //        // Console.WriteLine(usuario.Nombre);  // Alexis
        //    }
        //}
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file, IFormFile file2, string action, int numFile=0)
        {
            if (file == null || file.Length == 0)
            {
               // TempData["ErrorMessage"] = 0;
                return RedirectToAction("Index");
            }

            var fileXlsx = new FileXlsx
            {
                NombreFile = file.FileName
            };
           
            // Guardar el archivo en memoria
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileXlsx.File = memoryStream.ToArray();
            }

            //TempData["ErrorMessage"] = 1;


            // Si el actionType es "headers", ejecuta la lógica de obtener headers
                if(action == null)
                {
                HttpContext.Session.SetString("FileXlsx", JsonConvert.SerializeObject(fileXlsx));
                return NoContent();
                }
            else { 
                using var content = new MultipartFormDataContent();
                using var stream = new MemoryStream(fileXlsx.File);
                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

                content.Add(fileContent, "file", fileXlsx.NombreFile);

                HttpResponseMessage response = await _httpClient.PostAsync("http://127.0.0.1:8000/uploadexcel", content);

                if (!response.IsSuccessStatusCode)
                {
                    return BadRequest("Error al enviar el archivo.");
                }

                string result = await response.Content.ReadAsStringAsync();

                 List<string> headers = JsonConvert.DeserializeObject<List<string>>(result);
                fileXlsx.header =  headers ;
                fileXlsxList.Insert(numFile,fileXlsx);
                HttpContext.Session.SetString("FileXlsxList", JsonConvert.SerializeObject(fileXlsxList));


                
                return RedirectToAction(action); // Si solo se sube el archivo, vuelve al Index
                }
            }
        


        [HttpGet]
        public  IActionResult DividirEnHojas()
        {
            var usuarioJson = HttpContext.Session.GetString("FileXlsxList");
            if (usuarioJson != null)
            {
                fileXlsxList = JsonConvert.DeserializeObject< List < FileXlsx >> (usuarioJson);
                // Console.WriteLine(usuario.Nombre);  // Alexis
            }
            return View(fileXlsxList[0]);
        }
        [HttpPost]
        public async Task<IActionResult> DividirEnHojas(Dictionary<string, string> seleccionados)
        {
            var usuarioJson = HttpContext.Session.GetString("FileXlsxList");
            if (usuarioJson != null)
            {
                fileXlsxList = JsonConvert.DeserializeObject<List<FileXlsx>>(usuarioJson);
                // Console.WriteLine(usuario.Nombre);  // Alexis
            }
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
            string jsonData = JsonConvert.SerializeObject(resultado);
            using var content = new MultipartFormDataContent();

            // Agregar el JSON al formulario como texto
            var jsonContent = new StringContent(jsonData, Encoding.UTF8, "application/json");
            content.Add(jsonContent, "columnas_hoja");

            // Agregar el archivo al formulario
            using var stream = new MemoryStream(fileXlsxList[0].File);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            content.Add(fileContent, "file", "archivo.xlsx");




            HttpResponseMessage response = await _httpClient.PostAsync("http://localhost:8000/dividir", content);

            if (response.IsSuccessStatusCode)
            {

                byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "archivo_Convertido.xlsx");
            }
            else
            {
                return BadRequest("Error al generar el archivo en FastAPI.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConvertirCsvAXlsx(string nombre ="archivoConvertido")
        {
            var usuarioJson = HttpContext.Session.GetString("FileXlsxList");
            if (usuarioJson != null)
            {
                fileXlsxList = JsonConvert.DeserializeObject<List<FileXlsx>>(usuarioJson);
                // Console.WriteLine(usuario.Nombre);  // Alexis
            }
            // TempData["ErrorMessage"] = null;
            //int option = 1;
            //string jsonData = JsonSerializer.Serialize(option);
            using var content = new MultipartFormDataContent();

            //// Agregar el JSON al formulario como texto
            //var jsonContent = new StringContent(jsonData, Encoding.UTF8, "application/json");
            //content.Add(jsonContent, "columnas_hoja");

            // Agregar el archivo al formulario
            using var stream = new MemoryStream(fileXlsxList[0].File);
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            content.Add(fileContent, "file", "archivo.csv");
            HttpResponseMessage response = await _httpClient.PostAsync("http://localhost:8000/convertir", content);
            if (response.IsSuccessStatusCode)
            {

                byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombre+".xlsx");
            }
            else
            {
                return BadRequest("Error al generar el archivo en FastAPI.");
            }
            //return View();
        }
        [HttpGet]
        public IActionResult ConvertirCsvAXlsx()
        {

            return View();
        }

        [HttpGet]
        public IActionResult UnirXlsx()
        {
            var usuarioJson = HttpContext.Session.GetString("FileXlsxList");
            if (usuarioJson != null)
            {
                fileXlsxList = JsonConvert.DeserializeObject<List<FileXlsx>>(usuarioJson);
                // Console.WriteLine(usuario.Nombre);  // Alexis
            }
            return View(fileXlsxList);
        }
        [HttpPost]
        public IActionResult UnirtXlsx()
        {

            return View();
        }
    }
}