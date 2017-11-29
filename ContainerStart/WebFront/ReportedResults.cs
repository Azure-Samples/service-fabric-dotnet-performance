using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebFront
{
    public class ReportedResults
    {           
        public List<KeyValuePair<string, long>> GlobalReports { get; set; }
        public List<KeyValuePair<string, long>> LocalReports { get; set; }
    }
}
