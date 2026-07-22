namespace StjacksAssistens.ConfeccionModels;

public class AusentismoConsolidadoViewModel
{
    public DateTime Fecha { get; set; }
    public string Linea { get; set; }
    public double Incapacidad { get; set; }
    public double PermisoPersonal { get; set; }
    public double Clinica { get; set; } 
    public double ISSS { get; set; }
    public double TotalMinutos { get; set; }
    public int ModaParcial { get; set; }
    public double ModaTotal { get; set; }
}
