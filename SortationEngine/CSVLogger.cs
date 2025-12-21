using System.IO;

namespace SortationEngine
{
    public static class CSVLogger
    {
        private static int nextId = -1;
        public static void Log(string filePath, SimulationResult result)
        {
            bool fileExists = File.Exists(filePath);

            if (CSVLogger.nextId == -1 && fileExists)
            {
                string lastLine = File.ReadLines(filePath).LastOrDefault();

                // Check whether the last line is not empty or the header
                if (!string.IsNullOrEmpty(lastLine) && !lastLine.StartsWith("RunId"))
                {
                    var parts = lastLine.Split(',');
                    if (int.TryParse(parts[0], out int lastIdInFile))
                    {
                        CSVLogger.nextId = lastIdInFile + 1;
                    }
                }
            }

            if (CSVLogger.nextId == -1) CSVLogger.nextId = 1;

            using (StreamWriter writer = new StreamWriter(filePath, append: true))
            {
                if (!fileExists)
                {
                    writer.WriteLine("RunId,Success,Duration,TotalTrucks,TotalParcels,MinInterarrival,MaxBeltLoad,MaxStationLoad,AvgProcessingTime,StationLoadStdDev");
                }

                string line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8:F2},{9:F2}",
                    CSVLogger.nextId,
                    result.Success,
                    result.DurationTicks,
                    result.TotalTrucks,
                    result.TotalParcels,
                    result.MinInterarrivalTicks,
                    result.MaxBeltLoad,
                    result.MaxStationLoad,
                    result.AvgProcessingTime,
                    result.StationLoadStdDev
                );

                writer.WriteLine(line);
                CSVLogger.nextId++;
            }
        }
    }
}