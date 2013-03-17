using System;
using System.Collections.Generic;
using System.Data;
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

		public static TimeEntry fromRow(DataRow dr)
		{
			TimeEntry te = new TimeEntry();
			te.ID = (int)dr[0];
			te.monthNumber = (int)dr[1];
			te.dayNumber = (int)dr[2];
			te.yearNumber = (int)dr[3];
			te.grantID = (int)dr[4];
			te.grantHours = (double)dr[5];
			te.empID = (int)dr[6];
			if (dr[7] != DBNull.Value)
			{
				te.supervisorID = (int)dr[7];
			}
			return te;
		}
    }
}