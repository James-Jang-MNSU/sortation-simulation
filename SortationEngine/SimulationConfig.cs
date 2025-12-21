using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SortationEngine
{
    public static class SimulationConfig
    {
        // Simulation Length: 8 Hours (1 Simulation Tick = 1 Second)
        public const int ShiftLengthSeconds = 8 * 60 * 60;

        // Hub Setting: 20 stations per hub
        public const int TotalStations = 20;

        // Station Setting
        public const int StationCapacity = 50;

        // Parcel Processing Time: Parameters for the normal distribution
        public const double ProcessingTimeMean = 5.0;
        public const double ProcessingTimeStdDev = 1.5;

        // Truck Arrival Rate: Arrives every 7.5 minutes at induction point.
        public const double AvgTruckArrivalRate = 1.0 / (7.5 * 60);
        
        // Batch Size: 
        public const double BatchSizeMean = 100;
        public const double BatchSizeStdDev = 25;

        // Batch Clustering
        public const double ClusterRatio = 0.7;
    }
}
