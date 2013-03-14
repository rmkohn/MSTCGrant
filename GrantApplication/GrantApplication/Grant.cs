using System;
using System.Collections.Generic;
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
    }
}