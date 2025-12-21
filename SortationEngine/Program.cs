using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Spectre.Console;
using SortationEngine;
using System.Reflection.Emit;

namespace SortationEngine
{
    public class Program
    {
        static void Main(string[] args)
        {
            //RunVisualMode();
            RunStressTest();
        }

        static void RunVisualMode()
        {
            // 1. Initialize the Simulation Engine
            SortationHub hub = new SortationHub();
            int currentTimeTick = 0;

            // 2. Initialize the Visualization Dashboard
            Table dashboard = new Table();
            dashboard.Border(TableBorder.Rounded);
            dashboard.AddColumn("Time (Tick)");
            dashboard.AddColumn("Queue Depth");
            for (int i = 1; i <= SimulationConfig.TotalStations; i++)
            {
                dashboard.AddColumn($"Station{i}");
            }
            dashboard.AddColumn("Status");

            // 3. Start the Live Visualization Loop
            AnsiConsole.Live(dashboard)
                .AutoClear(false)
                .Start(ctx =>
                {
                    while (!hub.SystemJam && currentTimeTick < SimulationConfig.ShiftLengthSeconds)
                    {
                        currentTimeTick++;
                        //Thread.Sleep(1);

                        // Input: Random Arrival Generator
                        if (Random.Shared.NextDouble() < SimulationConfig.AvgTruckArrivalRate)
                        {
                            List<Parcel> batch = BatchGenerator.GenerateBatch(currentTimeTick);
                            hub.RecordTruckArrival(currentTimeTick);
                            foreach (Parcel parcel in batch)
                            {
                                hub.Ingest(parcel);
                            }
                        }

                        hub.RunTick();

                        // Visualization: Update the Table
                        dashboard.Rows.Clear();
                        // Using ternary operators
                        dashboard.AddRow(
                            currentTimeTick.ToString(),
                            hub.ConveyorBelt.Count >= 45 ? $"[bold red]{hub.ConveyorBelt.Count}[/]" : hub.ConveyorBelt.Count.ToString(), // Turn red if near full
                            hub.Stations[0].IsBusy ? $"[red]Busy ({hub.Stations[0].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.Stations[1].IsBusy ? $"[red]Busy ({hub.Stations[1].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.Stations[2].IsBusy ? $"[red]Busy ({hub.Stations[2].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.Stations[3].IsBusy ? $"[red]Busy ({hub.Stations[3].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.Stations[4].IsBusy ? $"[red]Busy ({hub.Stations[4].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.Stations[5].IsBusy ? $"[red]Busy ({hub.Stations[5].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.Stations[6].IsBusy ? $"[red]Busy ({hub.Stations[6].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.Stations[7].IsBusy ? $"[red]Busy ({hub.Stations[7].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.Stations[8].IsBusy ? $"[red]Busy ({hub.Stations[8].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.Stations[9].IsBusy ? $"[red]Busy ({hub.Stations[9].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.Stations[10].IsBusy ? $"[red]Busy ({hub.Stations[10].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.Stations[11].IsBusy ? $"[red]Busy ({hub.Stations[11].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.Stations[12].IsBusy ? $"[red]Busy ({hub.Stations[12].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.Stations[13].IsBusy ? $"[red]Busy ({hub.Stations[13].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.Stations[14].IsBusy ? $"[red]Busy ({hub.Stations[14].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.Stations[15].IsBusy ? $"[red]Busy ({hub.Stations[15].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.Stations[16].IsBusy ? $"[red]Busy ({hub.Stations[16].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.Stations[17].IsBusy ? $"[red]Busy ({hub.Stations[17].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.Stations[18].IsBusy ? $"[red]Busy ({hub.Stations[18].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.Stations[19].IsBusy ? $"[red]Busy ({hub.Stations[19].TicksRemaining})[/]" : "[green]Idle[/]",
                            hub.SystemJam? "[bold red]CRITICAL JAM[/]" : "[green]Running[/]"
                        );

                        ctx.Refresh();
                    }
                });

            // 4. Results
            AnsiConsole.MarkupLine($"[bold red]SIMULATION ENDED.[/] Survived for {currentTimeTick} ticks.\n");
            SimulationResult result = ResultFactory.Create(0, hub, currentTimeTick >= SimulationConfig.ShiftLengthSeconds, currentTimeTick);
            AnsiConsole.MarkupLine($"--- RESULTS ---\n");
            AnsiConsole.MarkupLine($"Run Id:            {result.RunId}");
            AnsiConsole.MarkupLine($"Success:           {result.Success}");
            AnsiConsole.MarkupLine($"Duration:          {result.DurationTicks}");
            AnsiConsole.MarkupLine($"Total Trucks:      {result.TotalTrucks}");
            AnsiConsole.MarkupLine($"Total Parcels:     {result.TotalParcels}");
            AnsiConsole.MarkupLine($"Min Arrival Inter: {result.MinInterarrivalTicks}");
            AnsiConsole.MarkupLine($"Max Belt Load:     {result.MaxBeltLoad}");
            AnsiConsole.MarkupLine($"Max Station Load:  {result.MaxStationLoad}");
            AnsiConsole.MarkupLine($"Avg Process Time:  {result.AvgProcessingTime}");
            AnsiConsole.MarkupLine($"Station Load StdD: {result.StationLoadStdDev}");

            // 5. Log to csv
            CSVLogger.Log("simulation_data.csv", result);
        }
        static void RunStressTest()
        {
            // 1. Configuration
            const int SimRuns = 1000;   // Length of the stress test
            List<int> survivalTimes = new List<int>();

            Console.WriteLine($"Starting Stress Test - Running {SimRuns} simulations...");
            Console.WriteLine("--------------------------------------------------");

            // 2. Stress Test
            for (int i = 0; i < SimRuns; i++)
            {
                SortationHub hub = new SortationHub();
                int currentTick = 0;

                // Simulation Loop
                while (!hub.SystemJam && currentTick < SimulationConfig.ShiftLengthSeconds)
                {
                    currentTick++;

                    // --- Input: Safety Margin Settings ---
                    if (Random.Shared.NextDouble() < SimulationConfig.AvgTruckArrivalRate)
                    {
                        List<Parcel> batch = BatchGenerator.GenerateBatch(currentTick);
                        hub.RecordTruckArrival(currentTick);
                        foreach (Parcel parcel in batch)
                        {
                            hub.Ingest(parcel);
                        }
                    }

                    // --- Process ---
                    hub.RunTick();
                }
                // Heart Beat: Prints progress every 100 runs
                if (i % 100 == 0) Console.Write(".");

                // 3. Results
                SimulationResult result = ResultFactory.Create(i, hub, currentTick >= SimulationConfig.ShiftLengthSeconds, currentTick);
                CSVLogger.Log("simulation_data.csv", result);
                
                // If jammed before 8 hours, stores current ticks
                // If hub survives, stores 28800
                survivalTimes.Add(currentTick);


            }

            Console.WriteLine("\n\nData Collection Complete.");

            // Analysis
            int failures = survivalTimes.Count(t => t < SimulationConfig.ShiftLengthSeconds);
            double failureRate = (double)failures / SimRuns;
            double averageSurvival = survivalTimes.Average();

            Console.WriteLine("\n[STRESS TEST RESULTS]");
            Console.WriteLine($"Total Runs:       {SimRuns}");
            Console.WriteLine($"Successful Shifts: {SimRuns - failures}");
            Console.WriteLine($"Failed Shifts:     {failures}");
            Console.WriteLine($"FAILURE RATE:      {failureRate:P2}");
            Console.WriteLine($"Avg Survival Time: {averageSurvival:F0} ticks");
            Console.WriteLine("--------------------------------------------------");
        }
    
    }
}