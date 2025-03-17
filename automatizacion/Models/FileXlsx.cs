namespace automatizacion.Models
{
    public class FileXlsx
    {
        public List<string> header { get; set; }
       

        public Dictionary<string, string> dividir { get; set; }

        public int CantidadHojas { get; set; } = 2;

        public string NombreFile { get; set; }
        public Byte[] File { get; set; }
        
    }
}