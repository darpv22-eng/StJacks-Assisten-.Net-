namespace StjacksAssistens.Models
{
    // Todo lo que necesita la pantalla de Ausentismo rediseñada (dashboard).
    public class AusentismoDashboardViewModel
    {
        public string Periodo { get; set; } = "";

        // Filas del consolidado (tabla de detalle, idéntica al Excel).
        public List<AusentismoConsolidadoViewModel> Filas { get; set; } = new();

        // ---- KPIs ----
        public double TotalMinutos { get; set; }
        public double JornadasEquivalentes { get; set; }   // TotalMinutos / 510
        public int DiasConAusencia { get; set; }
        public string LineaTop { get; set; } = "—";
        public double LineaTopMinutos { get; set; }
        public string CategoriaTop { get; set; } = "—";
        public double CategoriaTopMinutos { get; set; }

        // ---- Agregados para gráficos ----
        public Dictionary<string, double> PorCategoria { get; set; } = new(); // nombre -> minutos
        public List<SerieItem> PorLinea { get; set; } = new();                // ranking desc
        public List<SerieItem> PorDia { get; set; } = new();                  // tendencia

        // ---- Mapa de calor (líneas × días) ----
        public List<string> Lineas { get; set; } = new();
        public List<DateTime> Dias { get; set; } = new();
        // clave: "<linea>|yyyy-MM-dd" -> minutos
        public Dictionary<string, double> Celdas { get; set; } = new();
    }

    public class SerieItem
    {
        public string Etiqueta { get; set; } = "";
        public double Valor { get; set; }
    }
}
