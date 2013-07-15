using ClusteringWorkerRole.ClusterModel;
using MyWebRole.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClusteringWorkerRole.Similarity
{
    interface SimilarityHandlerInterface
    {
        void CalculateSimilarity(ItemRepresentation item);
        void OrderSimilarities();
        ItemClusterRepresentation GetGroupId();
    }
}
