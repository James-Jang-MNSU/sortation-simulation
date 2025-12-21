using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SortationEngine;

namespace SortationEngine
{ 
    public class BatchGenerator
    {
        public static List<Parcel> GenerateBatch(int currentTick)
        {
            List<Parcel> batch = new List<Parcel>();

            // 1. Determine Batch Size (Normal Distribution)
            double val = StatUtils.NextGaussian(SimulationConfig.BatchSizeMean, SimulationConfig.BatchSizeStdDev);
            int batchSize = Math.Max(1, (int)Math.Round(val));

            // 2. Pick a "Target Station" for batch
            int targetStationId = Random.Shared.Next(0, SimulationConfig.TotalStations);

            // 3. Calculate cluster size
            int clusteredCount = (int)(batchSize * SimulationConfig.ClusterRatio);
            int remainderCount = batchSize - clusteredCount;

            // 4. Generate Clustered Parcels (Forced Destination)
            for (int i = 0; i < clusteredCount; i++)
            {
                // The extra argument targets the parcel from the constructor
                batch.Add(new Parcel(currentTick, targetStationId));
            }

            // 5. Generate Random Parcels (Noise)
            for (int i = 0; i < remainderCount; i++)
            {
                // Pass one argument to constructor to let the Parcel randomize itself
                batch.Add(new Parcel(currentTick));
            }

            // 6. Shuffle (Fisher-Yates Algorithm)
            int n = batch.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Shared.Next(n + 1);
                (batch[k], batch[n]) = (batch[n], batch[k]);
            }

            return batch;
        }
    }
}