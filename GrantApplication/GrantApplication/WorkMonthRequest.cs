using GrantApplication;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
class WorkMonthRequest
{
	public int? employee;
	public int? supervisor;
	public int month;
	public int year;
	public int[] grantids;
	private IEnumerable<GrantMonth> _grantMonths;
	public IEnumerable<GrantMonth> grantMonths
	{
		get
		{
			if (_grantMonths == null)
				_grantMonths = getGrantMonths(grantids.Select(id => id.ToString()).ToArray(), employee.ToString(), year.ToString(), month.ToString());
			return _grantMonths;
		}
	}

	// return a GrantMonth object from enough details to specify a set of WorkMonth entries for a single month and employee
	private static IEnumerable<GrantMonth> getGrantMonths(string[] grants, string empid, string year, string month)
	{
		IEnumerable<GrantMonth> grantmonths = OleDBHelper.query(
			"SELECT * FROM WorkMonth"
			+ " WHERE GrantID IN " + OleDBHelper.sqlInArrayParams(grants)
			+ " AND EmpID = ?"
			+ " AND WorkYear= ?"
			+ " AND WorkingMonth = ?"
			+ " ORDER BY GrantID"
			, GrantMonth.fromRow
			, grants.Concat(new string[] { empid, year, month }).ToArray()
		);
		return grantmonths;
	}

	public WorkMonthRequest(int? employee, int? supervisor, int month, int year, int[] grantids, IEnumerable<GrantMonth> newmonths = null)
	{
		this.employee = employee;
		this.supervisor = supervisor;
		this.month = month;
		this.year = year;
		this.grantids = grantids;
		this._grantMonths = newmonths;
	}

	public static WorkMonthRequest fromGrantMonths(IEnumerable<GrantMonth> months, IEnumerable<int> extraids = null)
	{
		GrantMonth first = months.FirstOrDefault();
		if (first == null)
			return new WorkMonthRequest(-1, -1, -1, -1, new int[] { }, new GrantMonth[] { }.AsEnumerable());
		IEnumerable<int> ids = months.Select(month => month.grantID);
		if (extraids != null)
			ids = ids.Concat(extraids);
		return new WorkMonthRequest(first.EmpID, first.supervisorID, first.workMonth, first.workYear, ids.ToArray(), months);
	}

	// get WorkMonths the lazy way
	// this should have been the only method to handle both ways of specifying a particular month, so that we don't
	// have to have requestFromDate() etc.  The downside is an extra database request.
	// Unfortunately WorkMonths aren't added until an approval request is sent.
	// Note that grants will be sorted, not returned in input order, and leave+nongrant time should not be dealt with here
	// rather, they should be passed to requestFromWorkMonth
	public static WorkMonthRequest fromQuery(NameValueCollection query)
	{
		string grantstr = query["grant"];
		string empid = query["employee"];
		string year = query["year"];
		string month = query["month"];
		string workmonthstr = query["id"];
		string supid = query["supervisor"];
		try
		{
			if (workmonthstr != null)
			{
				string[] workmonthIds = workmonthstr.Split(',').Where(g => !string.IsNullOrEmpty(g)).OrderBy(str => str).ToArray();
				IEnumerable<GrantMonth> workmonths = getGrantMonths(workmonthIds);
				return WorkMonthRequest.fromGrantMonths(workmonths);
			}
			else if (year != null && month != null)
			{
				IEnumerable<string> grants = grantstr == null
					? new string[] { }.AsEnumerable()
					: grantstr.Split(',').Where(g => !string.IsNullOrEmpty(g)).OrderBy(str => str);
				return new WorkMonthRequest(parseInt(empid), parseInt(supid), int.Parse(month), int.Parse(year), grants.Select(parseQueryGrant).ToArray());
				//return getWorkMonthIDs(grants, empid, year, month);
			}
		}
		catch (Exception) { }
		return null;
	}

	public static int? parseInt(string s)
	{
		int i;
		return int.TryParse(s, out i) ? (int?)i : null;
	}

	public static int parseQueryGrant(string grant)
	{
		int ret;
		if (!int.TryParse(grant, out ret))
		{
			switch (grant)
			{
				case "non-grant": return Globals.GrantID_NonGrant;
				case "leave": return Globals.GrantID_Leave;
				default: throw new ArgumentException("unknown grant name");
			}
		}
		return ret;
	}

	public static string getGrantName(int grantid)
	{
		switch (grantid)
		{
			case Globals.GrantID_NonGrant: return "non-grant";
			case Globals.GrantID_Leave: return "leave";
			default: return grantid.ToString();
		}
	}

	// return a GrantMonth object from a WorkMonth ID
	private static IEnumerable<GrantMonth> getGrantMonths(string[] workmonthIDs)
	{
		return OleDBHelper.query(
				"SELECT * FROM WorkMonth WHERE ID IN (" + OleDBHelper.sqlInArrayParams(workmonthIDs) + ")"
				, GrantMonth.fromRow
				, workmonthIDs
			);
	}

	public Dictionary<string, IEnumerable<double>> getTimes(string[] grants)
	{
		return getTimeEntries(grants).ToDictionary(
			kv => kv.Key,
			kv => kv.Value.Select(time => time == null ? 0 : time.grantHours)
		);
	}

	public Dictionary<string, TimeEntry[]> getTimeEntries(string[] grants)
	{
		IEnumerable<string> allgrants = grantids.Select(id => id.ToString()).Concat(grants);
		if (allgrants.Count() == 0)
			return new Dictionary<string, TimeEntry[]>();

		int defaultMonthLength = DateTime.DaysInMonth(year, month + 1);

		IEnumerable<TimeEntry> times = OleDBHelper.query(
			"SELECT TimeEntry.* FROM TimeEntry"
			+ " WHERE TimeEntry.GrantID IN " + OleDBHelper.sqlInArrayParams(allgrants)
			+ " AND TimeEntry.EmpId = ?"
			+ " AND TimeEntry.YearNumber = ?"
			+ " AND TimeEntry.MonthNumber = ?"
			+ " ORDER BY TimeEntry.GrantID, TimeEntry.DayNumber ASC"
			, TimeEntry.fromRow
			, allgrants.Concat(new string[] { employee.ToString(), year.ToString(), month.ToString() }).ToArray()
		);
		Dictionary<string, TimeEntry[]> grouped = groupTimeEntries(times, defaultMonthLength);

		// make sure there is some entry for each key, even if we didn't find any TimeEntries for it
		allgrants.ForEach(key =>
		{
			if (!grouped.ContainsKey(key))
				grouped.Add(key, new TimeEntry[defaultMonthLength]);
		});
		return grouped;
	}

	private Dictionary<string, TimeEntry[]> groupTimeEntries(IEnumerable<TimeEntry> times, int defaultLength)
	{
		if (times == null || times.Count() == 0)
		{
			return new Dictionary<string, TimeEntry[]>();
		}
		Dictionary<string, TimeEntry[]> groupdict = times.GroupBy(time => time.grantID).ToDictionary(
			group => group.Key.ToString(),
			group =>
			{
				//int length = DateTime.DaysInMonth(group.First().yearNumber, group.First().monthNumber+1);
				TimeEntry[] days = new TimeEntry[defaultLength];
				group.ForEach(entry => days[entry.dayNumber - 1] = entry); // days are 1-indexed, months are 0-indexed?
				return days;
			}
		);
		return groupdict;
	}




}