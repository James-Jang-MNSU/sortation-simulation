using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using SortationEngine;
using Spectre.Console;
using static System.Collections.Specialized.BitVector32;

namespace SortationEngine
{
    public class SortationHub
    {
        // Class Variables
        private Queue<Parcel> conveyorBelt = new Queue<Parcel>();
        private List<Station> stations = new List<Station>();
        private const int MaxQueueDepth = 300;  // Capacity of conveyor belt
        private bool systemJam = false;
        private int lastArrivalTick = -1;
        
        // Gets and Sets
        public Queue<Parcel> ConveyorBelt { get; private set; }
        public List<Station> Stations { get; private set; }
        public bool SystemJam { get; private set; }


        public int TotalTrucks { get; private set; }
        public int TotalParcelsIngested { get; private set; }
        public int MinInterarrivalTicks { get; private set; }
        public int MaxBeltLoad { get; private set; }

        // Constructor
        public SortationHub()
        {
            this.ConveyorBelt = new Queue<Parcel>();
            this.Stations = new List<Station>();
            this.SystemJam = false;
            for (int i = 0; i < SimulationConfig.TotalStations; i++)
            {
                this.Stations.Add(new Station(i));
            }

            this.TotalTrucks = 0;
            this.TotalParcelsIngested = 0;
            this.MinInterarrivalTicks = int.MaxValue;
            this.MaxBeltLoad = 0;
        }

        // Class Methods
        public void Ingest(Parcel aParcel)   // Adds a new batch to the belt
        {
            // System jams when parcel count exceeds conveyor belt capacity
            if (this.ConveyorBelt.Count >= SortationHub.MaxQueueDepth)
            {
                this.SystemJam = true;
                return;
            }
            this.ConveyorBelt.Enqueue(aParcel);
            this.TotalParcelsIngested++;
            if (this.ConveyorBelt.Count > this.MaxBeltLoad)
            {
                MaxBeltLoad = this.ConveyorBelt.Count;
            }
        }
        public void RunTick()
        {
            // 1. Sort parcel
            int sortCapacityPerTick = 5; // The scanner can read up to 5 parcels/sec
            int sortedCount = 0;
            while (this.ConveyorBelt.Count > 0 && sortedCount < sortCapacityPerTick)
            {
                Parcel topParcel = this.ConveyorBelt.Peek();
                Station targetStation = this.Stations[topParcel.DestinationId];
                if (!targetStation.IsFull)
                {
                    this.ConveyorBelt.Dequeue();    // Remove from Belt
                    targetStation.Add(topParcel);   // Add to Station Queue
                    sortedCount++;
                }
                else
                {
                    break;
                }
            }
            // 2. Assign parcels to idle stations and process 1 tick
            foreach (Station station in this.Stations)
            {
                if (!station.IsBusy && station.QueueCount > 0)
                {
                    Parcel parcelToProcess = station.Queue.Dequeue();
                    station.Assign(parcelToProcess);
                }
                station.Tick();
            }
            /*
            foreach (Station station in this.Stations) 
            {
                if (!station.IsBusy && this.ConveyorBelt.Count > 0)
                {
                    Parcel parcelToProcess = ConveyorBelt.Dequeue();
                    station.Assign(parcelToProcess);
                }
                station.Tick();
            }
            */
        }
        public void RecordTruckArrival(int currentTick) // Used to record the minimum interval between truck arrivals
        {
            this.TotalTrucks++;

            if (this.lastArrivalTick != -1)
            {
                int delta = currentTick - this.lastArrivalTick;
                if (delta < this.MinInterarrivalTicks)
                {
                    this.MinInterarrivalTicks = delta;
                }
            }
            this.lastArrivalTick = currentTick;
        }
    }
}
