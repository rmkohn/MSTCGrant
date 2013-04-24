using System;
using System.Web;

namespace GrantApplication
{
	public class NoCache : IHttpHandler
	{
		public bool IsReusable
		{
			get { return true; }
		}

		public void ProcessRequest(HttpContext context)
		{
			// no-cache is equivalent to max=age=0, the others are overkill
			context.Response.AppendHeader("Cache-Control", "max-age=0, no-cache, no-store, must-revalidate");
			// thanks, stackoverflow (equivalent to max-age=0, for obvious reasons)
			context.Response.AppendHeader("Expires", DateTime.Now.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'"));
			// equivalent to cache-control: no-cache, but for http/1.0
			context.Response.AppendHeader("Pragma", "no-cache");
			// hey, does cache still look like a word to you? cache cache cache cache
			context.Response.WriteFile("volker.html");
		}
	}
}
