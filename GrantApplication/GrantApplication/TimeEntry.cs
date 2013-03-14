using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GrantApplication
{
    [Serializable()]
    public class TimeEntry
    {
        public int ID = 0;
        public int monthNumber = 0;
        public int dayNumber = 0;
        public int yearNumber = 0;
        public int grantID = 0;
        public double grantHours = 0.0;
        public int empID = 0;
        public int supervisorID = 0;
    }
}