using System.Collections.Generic;

namespace StjacksAssistens.TintoModels
{
    public class TintoDashboardViewModel
    {
        public IEnumerable<Groups> Groups { get; set; } = new List<Groups>();
        public IEnumerable<OperatorsTintos> Operators { get; set; } = new List<OperatorsTintos>();

        // Propiedades auxiliares por si necesitas binding directo
        public Groups NewGroup { get; set; } = new Groups();
        public OperatorsTintos NewOperator { get; set; } = new OperatorsTintos();
    }
}