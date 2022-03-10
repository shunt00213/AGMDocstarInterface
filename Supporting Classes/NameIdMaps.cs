using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGMDocstarInterface
{
    public class NameIdMaps
    {
        public Dictionary<string,string> ContentTypes { get; set; }
        public Dictionary<string, string> SecurityClasses { get; set; }
        public Dictionary<string, string> CustomFieldMetas { get; set; }
    }
}
