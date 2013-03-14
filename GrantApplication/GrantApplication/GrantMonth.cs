using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.OleDb;

namespace GrantApplication
{
    [Serializable()]
    public class GrantMonth
    {
        public enum status
        {
            New = 0,  //had to use uppercase N because 'new' is a reserved word.
            pending = 1,
            disapproved = 2,
            approved = 3,
            final_approved = 4
        }
        private int _ID = 0;
        private int _empID = 0;
        private int _workYear = 0;
        private int _workMonth = 0;
        private int _grantID = 0;
        private int _supervisorID = 0;
        private status eStatus = status.pending;
        public int ID
        {
            get
            {
                return _ID;
            }
            set
            {
                _ID = value;
            }
        }
        public int EmpID
        {
            get
            {
                return _empID;
            }
            set
            {
                _empID = value;
            }
        }
        public int workYear
        {
            get
            {
                return _workYear;
            }
            set
            {
                _workYear = value;
            }
        }
        public int grantID
        {
            get
            {
                return _grantID;
            }
            set
            {
                _grantID = value;
            }
        }
        public int supervisorID
        {
            get
            {
                return _supervisorID;
            }
            set
            {
                _supervisorID = value;
            }
        }
        public int curStatus
        {
            get
            {
                return System.Convert.ToInt32(eStatus);
            }
            set
            {
                eStatus = (status)Enum.Parse(typeof(status), value.ToString());
            }
        }
        public int workMonth
        {
            get
            {
                return _workMonth;
            }
            set
            {
                _workMonth = value;
            }
        }
        public GrantMonth()
        {
        }
        public GrantMonth(DataRow dr)
        {
            ID = (int)dr[0];
            EmpID = (int)dr[1];
            workMonth = (int)dr[2];
            workYear = (int)dr[3];
            grantID = (int)dr[4];
            supervisorID = (int)dr[5];
            curStatus = (int)dr[6];
        }
       
    }
}