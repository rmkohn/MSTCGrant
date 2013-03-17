using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace GrantApplication
{
    [Serializable()]
    public class Employee
    {
        private int _ID = 0;
        private string _empNum = string.Empty;
        private string _lastName = string.Empty;
        private string _firstName = string.Empty;
        private string _jobTitle = string.Empty;
        private string _emailAddress = string.Empty;
        private string _password = string.Empty;
        private bool _registered = false;
        private bool _manager = false;
        private string _defaultSupervisor = string.Empty;

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
        public string EmpNum
        {
            get
            {
                return _empNum;
            }
            set
            {
                _empNum = value;
            }
        }
        public string lastName
        {
            get
            {
                return _lastName;
            }
            set
            {
                _lastName = value;
            }
        }
        public string firstName
        {
            get
            {
                return _firstName;
            }
            set
            {
                _firstName = value;
            }
        }
        public string jobTitle
        {
            get
            {
                return _jobTitle;
            }
            set
            {
                _jobTitle = value;
            }
        }
        public string emailAddress
        {
            get
            {
                return _emailAddress;
            }
            set
            {
                _emailAddress = value;
            }
        }
        public string password
        {
            get
            {
                return _password;
            }
            set
            {
                _password = value;
            }
        }
        public bool registered
        {
            get
            {
                return _registered;
            }
            set
            {
                _registered = value;
            }
        }
        public string defaultSupervisor
        {
            get
            {
                return _defaultSupervisor;
            }
            set
            {
                _defaultSupervisor = value;
            }
        }
        public bool manager
        {
            get
            {
                return _manager;
            }
            set 
            {
                _manager = value;
            }
        }
        public override string ToString()
        {
            if (firstName == string.Empty && lastName == string.Empty)
            {
                return base.ToString();
            }
            return firstName + " " + lastName + " - " + jobTitle;
        }
		public Employee() { }
		public Employee(DataRow dr)
		{
			this.ID = (int)dr[0];
			this.EmpNum = dr[1].ToString();
			this.firstName = dr[3].ToString();
			this.lastName = dr[2].ToString();
			this.jobTitle = dr[4].ToString();
			if (dr[5] != DBNull.Value)
			{
				this.emailAddress = dr[5].ToString();
			}
			if (dr[6] != DBNull.Value)
			{
				this.password = dr[6].ToString();
			}
			this.registered = (bool)dr[7];
			this.manager = (bool)dr[8];
			this.defaultSupervisor = dr[9].ToString();
			if (this.defaultSupervisor == string.Empty || this.defaultSupervisor == "0")
			{
				this.defaultSupervisor = "699"; //Marie Schmieder's emp ID.
			}
		}
		private String HashPassword(string pass)
		{
			return pass;
			// FIXME for the love of god
		}

		internal static bool TestPassword(Employee emp, string p)
		{
			return emp != null && emp.password == emp.HashPassword(p);
		}
	}
   
}