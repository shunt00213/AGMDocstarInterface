using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGMDocstarInterface
{
    public class WebServerData
    {
        public String ServerUrl { get; set; }
        public String ZoneUrl { get; set; }
        public String UpdatedToken { get; set; }
        public Boolean Proxy { get; set; }
        public Boolean MoveInProgress { get; set; }
        public dynamic Exception { get; set; }
    }
}
