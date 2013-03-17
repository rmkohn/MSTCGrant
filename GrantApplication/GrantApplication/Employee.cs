using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace GrantApplication
{
    [Serializable()]
    public class Employee
    {
        public int ID = 0;
        public string EmpNum = string.Empty;
        public string lastName = string.Empty;
        public string firstName = string.Empty;
        public string jobTitle = string.Empty;
        public string emailAddress = string.Empty;
        public string password = string.Empty;
        public bool registered = false;
        public bool manager = false;
        public string defaultSupervisor = string.Empty;
		public byte[] salt;

      
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
			try
			{
				string saltstr = (string)dr[10];
				for (int i = 0; i < saltstr.Length; i += 2)
				{
					salt[i / 2] = byte.Parse(saltstr.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
				}
			}
				
			catch (Exception)
			{
				securePassword();
			}
			if (this.salt == null || this.salt.Length == 0)
				securePassword();
		}
		private void securePassword()
		{
			this.salt = new byte[64];
			System.Security.Cryptography.RandomNumberGenerator.Create().GetBytes(salt);
			this.password = HashPassword(password);
			OleDBHelper.nonQuery(
				"UPDATE EmployeeList SET UserPass = " + password + ", salt = " + salt
				+ "WHERE ID = " + ID,
				new string[] { }
			);

		}
		private String HashPassword(string pass)
		{
			Rfc2898DeriveBytes pbkdf = new Rfc2898DeriveBytes(pass, salt, 1000);
			return pbkdf.GetBytes(24).ToString();
			//return pass;
			// FIXME for the love of god
		}

		static public bool TestPassword(Employee emp, string p)
		{
			return emp != null && emp.password == emp.HashPassword(p);
		}
	}
   
}