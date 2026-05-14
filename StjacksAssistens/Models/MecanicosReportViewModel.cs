namespace StjacksAssistens.Models
{
    //Reporte para planilla de mecanicos, se muestra el periodo seleccionado y una tabla con los mecanicos y sus ausencias por semana
    public class MecanicosReportViewModel
    {
        
        public Periodss Periodo { get; set; }
        public List<EmpleadoAusentismoRow> Empleados { get; set; }
    }

    public class EmpleadoAusentismoRow
    {
        public int Codigo { get; set; }
        public string Nombre { get; set; }
        public List<SemanaDatos> Semanas { get; set; }
        public int HorasAusente { get; set; }
        public int MinutosAusente { get; set; }
    }

    public class TimeRequest
    {
        public string? OperatorCode { get; set; } // El '?' evita la advertencia CS8618
        public int PeriodId { get; set; }
        public int Hours { get; set; }
        public int Minutes { get; set; }
    }

    public class SemanaDatos
    {
        public DateTime? Lunes { get; set; }
        public DateTime? Martes { get; set; }
        public DateTime? Miercoles { get; set; }
        public DateTime? Jueves { get; set; }
        public DateTime? Viernes { get; set; }
        public string Motivo { get; set; }
    }
    public class ObservationRequest
    {
        public string OperatorCode { get; set; } // Usamos Code porque es lo que pusimos en data-opid
        public int PeriodId { get; set; }
        public string Observation { get; set; }
    }
}

