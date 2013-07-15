using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyWebRole.Model
{
    [Serializable]
    public class ItemRepresentation
    {
        public ItemRepresentation(string id, float[] vectorValues, bool itemExists)
        {
            this.id = id;
            this.vectorValues = vectorValues;
            this.itemExists = itemExists;
        }


        public string id { get; set; }
        public float[] vectorValues { get; set; }
        public bool itemExists { get; set; }
    }
}
