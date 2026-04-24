namespace StjacksAssistens.Models
{
    public class MecanicosReportViewModel
    {
        
        public Periodss Periodo { get; set; }
        public List<EmpleadoAusentismoRow> Empleados { get; set; }
    }

    public class EmpleadoAusentismoRow
    {
        public int Codigo { get; set; }
        public string Nombre { get; set; }
        public List<SemanaDatos> Semanas { get; set; } // Semana 1 (fila arriba) y Semana 2 (fila abajo)
    }

    public class SemanaDatos
    {
        // Guardamos la fecha solo si tiene "PP" para mostrarla en el cuadro
        public DateTime? Lunes { get; set; }
        public DateTime? Martes { get; set; }
        public DateTime? Miercoles { get; set; }
        public DateTime? Jueves { get; set; }
        public DateTime? Viernes { get; set; }
        public string Motivo { get; set; }
    }
}

