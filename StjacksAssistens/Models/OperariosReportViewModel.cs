using StjacksAssistens.Models;

public class OperariosReportViewModel
{
    public Periodss Periodo { get; set; }
    public List<EmpleadoAusencia> Empleados { get; set; }
    public string AreaNombre { get; set; } // Ejemplo: "Confeccion P2"
    public string CDC { get; set; } // Ejemplo: "407"
    public string TipoPlanilla { get; set; } // Ejemplo: "06 Obra"
}

public class EmpleadoAusencia
{
    public string Codigo { get; set; }
    public string Nombre { get; set; }
    public List<SemanaDetalle> Semanas { get; set; }
}

public class SemanaDetalle
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