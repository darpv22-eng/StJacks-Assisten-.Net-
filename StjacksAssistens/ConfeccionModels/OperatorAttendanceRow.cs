namespace StjacksAssistens.ConfeccionModels
{
    public class OperatorAttendanceRow
    {
        public int OperatorsId { get; set; }
        public int Code { get; set; }
        public string Name { get; set; }
        // Guardamos ambos IDs por si los necesitas en los Modales
        public int? AreaId { get; set; }
        public int? LineaId { get; set; }

        // Aquí mostraremos algo bonito como: "Confección - Línea 21" o "Mecánicos"
        public string CategoryName { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        // Diccionario donde la llave es la fecha y el valor es el Status (X, PP...)
        public Dictionary<DateTime, string> DailyStatus { get; set; } = new Dictionary<DateTime, string>();
    }
}
