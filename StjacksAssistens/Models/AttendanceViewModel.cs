namespace StjacksAssistens.Models
{
    public class AttendanceViewModel
    {
        public Periodss? CurrentPeriod { get; set; }
        public List<DateTime> DaysInPeriod { get; set; } = new List<DateTime>();
        public List<OperatorAttendanceRow> Rows { get; set; } = new List<OperatorAttendanceRow>();
    }
}