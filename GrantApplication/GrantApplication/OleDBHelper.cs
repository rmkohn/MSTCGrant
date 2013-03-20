using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;
using System.Linq;
using System.Web;

namespace GrantApplication
{
	// if this gets any more complex it will definitely be time to bring in an external library
	// honestly it's what I should have done to begin with
	// but for now, <100 lines of fairly accessible code seems preferable to ~5000 incomprehensible
	// lines from a random external dependency
	// even if it's less comprehensive and almost certainly slower
	public class OleDBHelper
	{
		private OleDBHelper() { }

		static public T withConnection<T>(Func<OleDbConnection, T> fn)
		{
			OleDbConnection conn = new OleDbConnection(ConfigurationManager.ConnectionStrings["AccessConnectionString"].ConnectionString);
			conn.Open();
			T ret = fn(conn);
			conn.Close();
			return ret;
		}

		static public IEnumerable<T> query<T>(String querystr, IEnumerable<string> parameters, Func<DataRow, T> selector)
		{
			return withConnection(conn => query(querystr, parameters, selector, conn));
		}

		static public int nonQuery(String statement, IEnumerable<string> parameters)
		{
			return withConnection(conn => nonQuery(statement, parameters, conn));
		}


		// make a parameterized query, map the returned DataRows through a provided function, and return the result
		static public IEnumerable<T> query<T>(String query, IEnumerable<string> parameters, Func<DataRow, T> selector, OleDbConnection conn)
		{
			DbDataAdapter adapter = new OleDbDataAdapter();
			DbCommand cmd = new OleDbCommand(query, conn);
			parameters.ForEach(val =>
				cmd.Parameters.Add(new OleDbParameter(null, val))
			);
			DataSet set = new DataSet();
			adapter.SelectCommand = cmd;

			adapter.Fill(set);
			return set.Tables[0].Rows.Flatten<DataRow>().Select(selector);
		}

		static public int nonQuery(String statement, IEnumerable<string> parameters, OleDbConnection conn)
		{
			DbCommand cmd = new OleDbCommand(statement, conn);
			parameters.ForEach(val =>
				cmd.Parameters.Add(new OleDbParameter(null, val))
			);

			return cmd.ExecuteNonQuery();
		}

		// add " AND sqlkey[i] = ?" type conditions to a SELECT query, for the non-null entries in sqlvals
		// returns the new query string directly, and the non-null values in sqlparams
		static public String appendConditions(string sqlquery, string[] sqlkeys, string[] sqlvals, out IEnumerable<string> sqlparams)
		{
			IEnumerable<Tuple<string, string>> sqlkv = sqlkeys
				.Zip(sqlvals, (key, val) => new Tuple<string, string>(key, val))
				.Where(kv => kv.Item2 != null);
			sqlkv.ForEach(kv => sqlquery += " AND " + kv.Item1 + " = ?");
			sqlparams = sqlkv.Select(kv => kv.Item2);
			return sqlquery;
		}

		// fill out (?)s for an IN (?, ...) statement on the provided array
		static public string sqlInArrayParams(object[] inarray)
		{
			return inarray.Skip(1).Aggregate("( ?", (accum, g) => accum + ", ?") + " )";
		}
	}
}