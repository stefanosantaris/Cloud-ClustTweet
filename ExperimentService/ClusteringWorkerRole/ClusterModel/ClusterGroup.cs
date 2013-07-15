using MyWebRole.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusteringWorkerRole.ClusterModel
{
    [Serializable]
    public class ClusterGroup
    {


        public ClusterGroup()
        {
            clusterGroupList = new List<ItemRepresentation>();
        }


        public List<ItemRepresentation> clusterGroupList { get; set; }
        public ItemRepresentation centralizedItem { get; set; }
        public int groupId { get; set; }

    }
}
