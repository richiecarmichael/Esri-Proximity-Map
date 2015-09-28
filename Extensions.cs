using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESRI.PrototypeLab.ProximityMap {
    public static class Extensions {
        public static string ToString(this IDictionary<string, object> dictionary, string key, string def) {
            if (dictionary == null) { throw new ArgumentNullException("dictionary"); }
            if (key == null) { throw new ArgumentNullException("key"); }
            if (!dictionary.ContainsKey(key)) { return def; }
            if (dictionary[key] == null) { return def; }
            return dictionary[key].ToString();
        }
    }
}
