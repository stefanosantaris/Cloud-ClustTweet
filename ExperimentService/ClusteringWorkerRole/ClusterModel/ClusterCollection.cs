using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusteringWorkerRole.ClusterModel
{
    [Serializable]
    public class ClusterCollection
    {
        public List<ClusterGroup> collectionList { get; set; }
        public ClusterCollection()
        {
            collectionList = new List<ClusterGroup>();
        }
    }
}
