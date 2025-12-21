using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SortationEngine
{
    public class Parcel
    {
        // Class Variables
        private Guid id;
        private int arrivalTick;
        private int destinationId = -1;

        // Gets and Sets
        public Guid Id { get; private set; }    // Uses Guid (Globally Unique Identifier) instead of numerical type e.g. int

        public int ArrivalTick { get; private set; }    // Represents the time a parcel enters the system
        public int DestinationId { get; private set; }  // The Destination of a parcel

        // Constructor
        public Parcel(int anArrivalTick, int? overriddenDestination = null)
        {
            this.Id = Guid.NewGuid();
            this.ArrivalTick = anArrivalTick;
            this.DestinationId = overriddenDestination ?? Random.Shared.Next(0, SimulationConfig.TotalStations);
        }

        // Override ToString for easier debugging
        public override string ToString()
        {
            return $"[Parcel {Id.ToString().Substring(0, 4)} | Tick: {ArrivalTick}]";
        }
    }
}
