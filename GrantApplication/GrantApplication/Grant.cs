using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;

namespace GrantApplication
{
    [Serializable()]
    public class Grant
    {
        public int ID = 0;
        public string stateCatalogNum = string.Empty;
        public string category = string.Empty;
        public string grantNumber = string.Empty;
        public string grantTitle = string.Empty;
        public string grantManagerLast = string.Empty;
        public string grantManagerFirst = string.Empty;

        public Grant(Grant g)
        {
            this.category = g.category;
            this.grantManagerFirst = g.grantManagerFirst;
            this.grantManagerLast = g.grantManagerLast;
            this.grantNumber = g.grantNumber;
            this.grantTitle = g.grantTitle;
            this.ID = g.ID;
            this.stateCatalogNum = g.stateCatalogNum;
        }

        public Grant()
        {
        }
		public static Grant fromRow(DataRow dr)
		{
			Grant g = new Grant();
			g.ID = (int)dr[0];
			g.stateCatalogNum = dr[1].ToString();
			g.category = dr[2].ToString();
			g.grantNumber = dr[3].ToString();
			g.grantTitle = dr[4].ToString();
			g.grantManagerLast = dr[5].ToString();
			g.grantManagerFirst = dr[6].ToString();
			return g;
		}
    }
}