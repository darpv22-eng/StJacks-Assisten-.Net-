namespace StjacksAssistens.Models
{
    public class OperatorAttendanceRow
    {
        public int OperatorsId { get; set; }
        public int Code { get; set; }
        public string Name { get; set; }
        public string CategoryName { get; set; }
        // Diccionario donde la llave es la fecha y el valor es el Status (X, PP...)
        public Dictionary<DateTime, string> DailyStatus { get; set; } = new Dictionary<DateTime, string>();
    }
}
