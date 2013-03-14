using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GrantApplication
{
    [Serializable()]
    //sup, emp, selDate, selGrants[xx].ID)
    public class MailObject
    {
        public List<Employee> supervisors = null;
        public Employee emp = null;
        public DateTime selDate;
        public List<Grant> grants = null;
        public bool approved = false;
        public string reason = string.Empty;
        public int month = 0;
        public int year = 0;
        public string bodyTxt = string.Empty;
    }
}