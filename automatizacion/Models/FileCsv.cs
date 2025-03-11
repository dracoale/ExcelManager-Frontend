namespace automatizacion.Models
{
    public class FileCsv
    {
        public List<string> header { get; set; }
        public string encabezado { get; set; }

        public Dictionary<string, string> dividir { get; set; }
    }
}
