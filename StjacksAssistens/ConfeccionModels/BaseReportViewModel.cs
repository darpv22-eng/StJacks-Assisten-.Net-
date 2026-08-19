using System.Collections.Generic;
using StjacksAssistens.ConfeccionModels;

namespace StjacksAssistens.ViewModels
{
    public class BaseReportViewModel
    {
        public IEnumerable<Operators> Operators { get; set; } = new List<Operators>();

        // Cambiamos dynamic por el tipo real de tu modelo de línea (si se llama Linea o ProductionLine)
        public IEnumerable<dynamic> Lines { get; set; } = new List<dynamic>();
        // Nota: Si tu modelo de línea se llama 'Linea', puedes poner:
        // public IEnumerable<Linea> Lines { get; set; } = new List<Linea>();
    }
}