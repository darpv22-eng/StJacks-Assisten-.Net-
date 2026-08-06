namespace StjacksAssistens.ConfeccionModels
{
    public class ObservacionRequestDto
    {
        public int OperatorCode { get; set; }
        public int PeriodId { get; set; }
        public string Observation { get; set; }
        public int Semana { get; set; }
    }
}
