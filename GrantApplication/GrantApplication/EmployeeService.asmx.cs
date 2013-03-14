using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;


namespace GrantApplication
{
    /// <summary>
    /// Summary description for EmployeeService
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class EmployeeService : System.Web.Services.WebService
    {

        [WebMethod]
        public string HelloWorld()
        {
            return "Hello World";
        }

        [WebMethod(true)]
        public string[] GetCompletionList(string prefixText, int count)
        {
            return new List<string>().ToArray();                                   
        } 
    }
}
