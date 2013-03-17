using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.SessionState;

namespace GrantApplication
{
	public class IISHandler1 : IHttpHandler, IRequiresSessionState
	{
		/// <summary>
		/// You will need to configure this handler in the Web.config file of your 
		/// web and register it with IIS before being able to use it. For more information
		/// see the following link: http://go.microsoft.com/?linkid=8101007
		/// </summary>
		#region IHttpHandler Members

		public bool IsReusable
		{
			// Return false in case your Managed Handler cannot be reused for another request.
			// Usually this would be false in case you have some state information preserved per request.
			get { return true; }
		}

		public void ProcessRequest(HttpContext context)
		{
		//	context.Response.ContentType = "text/json";
			context.Response.ContentType = "text/plain";
			int emailkey = -1;
			NameValueCollection query = context.Request.QueryString;
			if (Int32.TryParse((String)query["key"], out emailkey))
			{
				// check with db -- we'll be emailing random keys associated with the data needed to log someone in and examine a time approval request
				// but for now, this is just for testing
				context.Session["ID"] = emailkey;
			}
			else if (((String)query["q"]) == "login")
			{
				doLogin(context, query["id"], query["pass"]);
			}
			else
			{
				int? id = (int?)context.Session["ID"];
				if (!id.HasValue)
				{
					writeResult(context, false, "Invalid Session ID");
				}
				else
				{
					switch ((String)context.Request.QueryString["q"])
					{
						case "listgrants":
							IEnumerable<Grant> grants = dbQuery(
								 "SELECT GrantInfo.* FROM GrantInfo, EmployeeList"
								+ " WHERE EmployeeList.ID = ?"
								+ " AND EmployeeList.LastName = GrantInfo.GrantManagerLast"
								+ " AND EmployeeList.FirstName = GrantInfo.GrantManagerFirst"
								, new String[] { id.ToString() }
								, Grant.fromRow
							);
							writeResult(context, true, grants);
							break;
						case "listrequests":
							string status = null;
							try
							{
								status = ((int)Enum.Parse(typeof(GrantMonth.status), query["status"], true)).ToString();
							}
							catch (ArgumentNullException) { }
							catch (ArgumentException)
							{
								writeResult(context, false, query["status"] + " is not a valid status");
								return;
							}
							string sqlquery = "SELECT WorkMonth.* FROM WorkMonth, GrantInfo WHERE GrantInfo.ID = WorkMonth.GrantID"
							//string sqlquery = "SELECT WorkMonth.* FROM WorkMonth"
							                +" AND SupervisorID = " + id;
							//string[] sqlkeys = new string[] { "GrantInfo.GrantNumber", "EmployeeList.EmployeeNum", "WorkMonth.Status" };
							string[] sqlkeys = new string[] { "GrantInfo.ID", "WorkMonth.EmpID", "WorkMonth.Status" };
							string[] sqlvals = new string[] { query["grant"], query["employee"], status};
							IEnumerable<string> sqlparams;
							sqlquery = appendConditions(sqlquery, sqlkeys, sqlvals, out sqlparams);
							IEnumerable<GrantMonth> gm = dbQuery(
								sqlquery,
								sqlparams,
								row => new GrantMonth(row)
							);
							writeResult(context, true, gm);
							break;
						case "viewrequest":
							viewrequest(context, id, query);
							break;
						case "listemployees":
							IEnumerable<SafeEmployee> defaultemployees = dbQuery(
								"SELECT * FROM EmployeeList"
								+ " WHERE DefaultSupervisor = " + id
								, new string[] { }
								, row => new SafeEmployee(new Employee(row))
								{

								}
							);
							writeResult(context, true, defaultemployees);
							break;
						case "approve":
							sendApproval(context, query, id);
							break;
						case "debug":
							List<KeyValuePair<String, String>> kvs = new List<KeyValuePair<String, String>>();
							foreach (String key in context.Session.Keys)
							{
								kvs.Add(new KeyValuePair<string, string>(key, context.Session[key].ToString()));
							}
							writeResult(context, true, kvs);
							break;
						case "logout":
							context.Session.Abandon();
							writeResult(context, true, "Logged out successfully");
							break;
						default:
							writeResult(context, false, "Unrecognized request");
							break;
					}
				}
			}
		}

		private void viewrequest(HttpContext context, int? id, NameValueCollection query)
		{
			string grantstr = query["grant"];
			string empid = query["employee"];
			string year = query["year"];
			string month = query["month"];
			if (grantstr == null || empid == null || year == null || month == null)
			{
				writeResult(context, false, "Missing employee id or grant number or date");
			}
			else
			{
				string[] grants = grantstr.Split(',');
				//List<Tuple<TimeEntry, string>> times = dbQuery(
				IEnumerable<TimeEntry> times = dbQuery(
					//"SELECT TimeEntry.*, GrantInfo.GrantNumber FROM TimeEntry, GrantInfo, EmployeeList"
					"SELECT TimeEntry.* FROM TimeEntry, GrantInfo, EmployeeList"
					+ " WHERE TimeEntry.GrantID = GrantInfo.ID"
					+ " AND TimeEntry.EmpID = EmployeeList.ID"
					//+ " AND TimeEntry.SupervisorID = " + id
					// the existing queries don't use it, plus it seems to be null a lot
					//+ " AND GrantInfo.GrantNumber IN "+ sqlInArrayParams(grants)
					//+ " AND EmployeeList.EmployeeNum = ?"
					+ " AND TimeEntry.EmpID = ?"
					+ " AND TimeEntry.GrantID IN "+ sqlInArrayParams(grants)
					+ " AND TimeEntry.YearNumber = ?"
					+ " AND TimeEntry.MonthNumber = ?"
					+ " ORDER BY TimeEntry.GrantID, TimeEntry.DayNumber ASC"
					, grants.Concat(new string[] {empid, year, month})
					//, row => new Tuple<TimeEntry, string>(TimeEntry.fromRow(row), (string)row[8])
					, TimeEntry.fromRow
				);
				//Dictionary<string, double[]> groupTimes = times.GroupBy(time => time.Item2).ToDictionary(
				//	group => group.Key.ToString(),
				//	group => group.Select(time => time.Item1.grantHours).ToArray()
				//);
				Dictionary<string, IEnumerable<double>> groupTimes = times.GroupBy(time => time.grantID).ToDictionary(
					group => group.Key.ToString(),
					group => group.Select(time => time.grantHours)
				);
				writeResult(context, true, groupTimes);
			}
		}

		private void sendApproval(HttpContext context, NameValueCollection query, int? id)
		{
			string grant = query["grant"];
			string employee = query["employee"];
			bool? approve = null;
			int? month = null;
			int? year = null;
			try
			{
				approve = bool.Parse(query["approval"]);
				month = Int32.Parse(query["month"]);
				year = Int32.Parse(query["year"]);
			}
			catch (Exception) {
				writeResult(context, false, "Formatting error in field");
				return;
			}
			if (grant == null || month == null || year == null || employee == null)
			{
				writeResult(context, false, "Missing required field");
			}
			else
			{
				string reason = query["reason"];
				reason = reason != null ? reason : "No reason given.";
				Employee user = dbQuery("SELECT * FROM EmployeeList WHERE ID = " + id, new string[] { }, row => new Employee(row)).First();
				List<Employee> employees = dbQuery("SELECT * FROM EmployeeList WHERE ID = ?", new string[] { employee }, row => new Employee(row)).ToList();
				if (employees.Count == 0)
				{
					writeResult(context, false, "No such employee");
					return;
				}
				string[] grantstrs = grant.Split(',');
				string grantquery = sqlInArrayParams(grantstrs);
				//List<Grant> grants = dbQuery("SELECT * FROM GrantInfo WHERE GrantNumber IN " + grantquery, grantstrs, Grant.fromRow);
				List<Grant> grants = dbQuery("SELECT * FROM GrantInfo WHERE ID IN " + grantquery, grantstrs, Grant.fromRow).ToList();
				if (grants.Count == 0)
				{
					writeResult(context, false, "No such grant(s)");
					return;
				}
				if (GrantApplication._Default.approveOrDisapproveStateless(approve.Value, reason, employees[0], user, grants, month.Value, year.Value))
				{
					writeResult(context, true, "Sent email to " + employees[0].firstName + " " + employees[0].lastName);
				}
				else
				{
					writeResult(context, false, "Error sending email");
				}
			}
		}

		private string sqlInArrayParams(object[] inarray)
		{
			return inarray.Skip(1).Aggregate("( ?", (accum, g) => accum + ", ?") + " )";
		}

		public void doLogin(HttpContext context, String empId, String pass)
		{
			empId = (empId != null) ? empId : "";
			IEnumerable<Employee> emps = dbQuery<Employee>(
				"SELECT * FROM EmployeeList WHERE registered = true AND EmployeeNum = ?",
				new String[] { empId },
				row => new Employee(row)
			);
			Employee emp = emps.FirstOrDefault();
			if (!Employee.TestPassword(emp, context.Request.QueryString["pass"]))
			{
				writeResult(context, false, "Wrong ID or password");
			}
			else
			{
				context.Session["ID"] = emp.ID;
				writeResult(context, true, "Logged in as " + emp.firstName + " " + emp.lastName);
			}
			return;
		}

		private void writeResult(HttpContext context, Boolean success, Object message)
		{
			JavaScriptSerializer s = new JavaScriptSerializer();
			Dictionary<String, Object> dict = new Dictionary<String, Object>();
			dict["success"] = success;
			dict["message"] = message;
#if DEBUG
			context.Response.Write(JsonPrettyPrinter.FormatJson(s.Serialize(dict)));
#else
			context.Response.Write(s.Serialize(dict));
#endif

		}
		
		// make a parameterized query, map the returned DataRows through a provided function, and return the result
		static private IEnumerable<T> dbQuery<T>(String query, IEnumerable<string> parameters, Func<DataRow, T> selector)
		{
			OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
			DbDataAdapter adapter = new OleDbDataAdapter();
			DbCommand cmd = new OleDbCommand(query, conn);
			parameters.ForEach(val =>
			{
				DbParameter p = new OleDbParameter(null, val);
				cmd.Parameters.Add(p);
			});
			DataSet set = new DataSet();
			adapter.SelectCommand = cmd;

			conn.Open();
			adapter.Fill(set);
			conn.Close();
			return set.Tables[0].Rows.Flatten<DataRow>().Select(selector);
		}
		
		// add " AND sqlkey[i] = ?" type conditions to a SELECT query, for the non-null entries in sqlvals
		// returns the new query string directly, and the non-null values in sqlparams
		static private String appendConditions(string sqlquery, string[] sqlkeys, string[] sqlvals, out IEnumerable<string> sqlparams)
		{
			IEnumerable<Tuple<string, string>> sqlkv = sqlkeys
				.Zip(sqlvals, (key, val) => new Tuple<string, string>(key, val))
				.Where(kv => kv.Item2 != null);
			sqlkv.ForEach(kv => sqlquery += " AND " + kv.Item1 + " = ?");
			sqlparams = sqlkv.Select(kv => kv.Item2);
			return sqlquery;
		}
		//static private String makeParams(List<String> keys) {
		//	if (keys.Count == 0)
		//		return "";
		//	string init = "WHERE " + keys[0] + " = ?";
		//	return keys.Skip(1).Aggregate(init,  (acc, key) => acc + " AND " + key + " = ?");
		//}
		class SafeEmployee
		{
			public string firstname;
			public string lastname;
			public string id;
			public SafeEmployee(Employee source)
			{
				firstname = source.firstName;
				lastname = source.lastName;
				id = source.ID.ToString();
			}
		}

		#endregion
	}
}
