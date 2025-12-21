using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SortationEngine;

namespace SortationEngine
{
    public class Station
    {
        // Class Variables
        private int id;                         // Represents parcel destination... Used to assign parcels by hub
        private Parcel? currentParcel = null;   // If the value is null, the station is idle
        private int ticksRemaining = 0;         // The number of seconds left until the job is done
        private bool isBusy = false;            // If CurrentParcel is NOT null, the station is busy
        private int capacity = -1;              // Capacity for each parcel
        private Queue<Parcel> queue;

        // Gets and Sets
        public int Id { get; }
        public Parcel? CurrentParcel { get; private set; }       
        public int TicksRemaining { get; private set; }
        public bool IsBusy => CurrentParcel != null;
        public int Capacity { get; private set; }
        public Queue<Parcel> Queue { get; private set; }

        public int TotalParcelsAssigned { get; private set; } = 0; // For stddev calculation
        public int TotalParcelsProcessed { get; private set; } = 0; // For average processing speed
        public int MaxQueueDepth { get; private set; } = 0;      // For peak stress
        public long TotalProcessingTicks { get; private set; } = 0; // For average processing speed

        // Constructor
        public Station(int anId)
        {
            this.Id = anId;
            this.CurrentParcel = null;
            this.TicksRemaining = 0;
            this.Capacity = SimulationConfig.StationCapacity;
            this.Queue = new Queue<Parcel>();
            
            this.TotalParcelsAssigned = 0;
            this.TotalParcelsProcessed = 0;
            this.MaxQueueDepth = 0;
            this.TotalProcessingTicks = 0;
        }

        // Class Methods
        public bool IsFull => this.QueueCount >= this.Capacity;
        public int QueueCount => this.Queue.Count;
        public void Add(Parcel aParcel)
        {
            if (!this.IsFull)
            {
                this.Queue.Enqueue(aParcel);
                this.TotalParcelsAssigned++;
                if (this.QueueCount > this.MaxQueueDepth)
                {
                    this.MaxQueueDepth = this.QueueCount;
                }
            }
        }
        public void Assign(Parcel aParcel)  // Starts working on a new parcel
        {
            this.CurrentParcel = aParcel;   // Accept a parcel

            // Randomly sets processing time (follows a normal distribution)
            double randomTime = StatUtils.NextGaussian(SimulationConfig.ProcessingTimeMean, SimulationConfig.ProcessingTimeStdDev);
            this.TicksRemaining = Math.Max(1, (int)Math.Round(randomTime));
        }
        public void Tick()  // Advances time by 1 second
        {
            if (!this.IsBusy) return;
            
            this.TicksRemaining--;
            this.TotalProcessingTicks++;

            if (this.TicksRemaining <= 0)
            {
                // Release the station
                this.CurrentParcel = null;
                this.TotalParcelsProcessed++;
            }
        }
    }
}
