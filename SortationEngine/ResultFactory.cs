using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SortationEngine
{
    // Container for results of a single simulation
    public record SimulationResult(
        int RunId,
        int Success,
        int DurationTicks,
        int TotalTrucks,
        int TotalParcels,
        int MinInterarrivalTicks,
        int MaxBeltLoad,
        int MaxStationLoad,
        double AvgProcessingTime,
        double StationLoadStdDev 
    );

    // Result Harvester (Extracts and Calculates)
    public static class ResultFactory
    {
        public static SimulationResult Create(int runId, SortationHub hub, bool success, int duration)
        {
            List<Station> stations = hub.Stations;

            // Find the single highest queue depth among stations
            int globalMaxQueue = stations.Max(s => s.MaxQueueDepth);

            // Calculate global processing speed (seconds/parcel)
            long totalTicks = stations.Sum(s => s.TotalProcessingTicks);
            int totalProcessed = stations.Sum(s => s.TotalParcelsProcessed);
            double avgSpeed = totalProcessed > 0 ? (double)totalTicks / totalProcessed : 0;

            // Calculate standard deviation of loads
            List<double> loads = stations.Select(s => (double)s.TotalParcelsAssigned).ToList();
            double meanLoad = loads.Average();
            double sumSquares = loads.Sum(val => Math.Pow(val - meanLoad, 2));
            double stdDev = Math.Sqrt(sumSquares / (stations.Count - 1));

            return new SimulationResult(
                RunId: runId,
                Success: Convert.ToInt32(success),
                DurationTicks: duration,
                TotalTrucks: hub.TotalTrucks,
                TotalParcels: hub.TotalParcelsIngested,
                MinInterarrivalTicks: hub.MinInterarrivalTicks,
                MaxBeltLoad: hub.MaxBeltLoad,
                MaxStationLoad: globalMaxQueue,
                AvgProcessingTime: avgSpeed,
                StationLoadStdDev: stdDev
            );
        }
    }
}