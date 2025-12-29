using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Spectre.Console;
using SortationEngine;
using System.Reflection.Emit;
using Spectre.Console.Rendering;

namespace SortationEngine
{
    public class Program
    {
        static void Main(string[] args)
        {
            RunVisualMode();
            //RunStressTest();
        }

        static void RunVisualMode()
        {
            // 1. Initialize the Simulation Engine
            SortationHub hub = new SortationHub();
            int currentTick = 0;

            // 2. Initialize the Visualization Grid
            Grid layoutGrid = new Grid();

            // 3. Start the Live Visualization Loop
            AnsiConsole.Live(layoutGrid)
                .AutoClear(false)
                .Start(ctx =>
                {
                    while (!hub.SystemJam && currentTick < SimulationConfig.ShiftLengthSeconds)
                    {
                        currentTick++;
                        Thread.Sleep(200);

                        // Input: Generates random arrival
                        if (Random.Shared.NextDouble() < SimulationConfig.AvgTruckArrivalRate)
                        {
                            List<Parcel> batch = BatchGenerator.GenerateBatch(currentTick);
                            hub.RecordTruckArrival(currentTick);
                            foreach (Parcel parcel in batch)
                            {
                                hub.Ingest(parcel);
                            }
                        }

                        hub.RunTick();

                        Grid hudGrid = new Grid();
                        hudGrid.AddColumn(new GridColumn().Centered());
                        hudGrid.AddColumn(new GridColumn().Centered());
                        hudGrid.AddColumn(new GridColumn().Centered());

                        Panel timePanel = new Panel($"{currentTick}").Header("TIME");
                        Panel beltCountPanel = new Panel($"{hub.ConveyorBelt.Count}").Header("QUEUE DEPTH");
                        Panel statusPanel = new Panel(hub.SystemJam ? "[red]CRITICAL JAM[/]" : "[green]RUNNING[/]").Header("STATUS");
                        timePanel.Width = 10;
                        beltCountPanel.Width = 17;
                        statusPanel.Width = 20;
                        hudGrid.AddRow(
                            timePanel,
                            beltCountPanel,
                            statusPanel
                        );

                        Grid stationGrid = new Grid();
                        for (int i=0; i<5; i++)
                        {
                            stationGrid.AddColumn(new GridColumn().Centered());
                        }

                        for (int i=0; i<4; i++)
                        {
                            var rowRenderables = new List<IRenderable>();
                            for (int j = 0; j < 5; j++)
                            {
                                int stationIndex = (i * 5) + j;
                                Station station = hub.Stations[stationIndex];

                                string status = station.IsBusy ? $"[red]BUSY ({station.TicksRemaining})[/]" : "[green]IDLE[/]";
                                string queue = (station.QueueCount >= 49 ? "[blue]" : "[white]") + $"Queue({station.QueueCount}/50)[/]";
                                Panel stationPanel = new Panel(status + queue).Header($"Stn. {stationIndex + 1:00}");
                                stationPanel.Width = 16;
                                stationPanel.Height = 5;
                                rowRenderables.Add(stationPanel);
                            }
                            stationGrid.AddRow(rowRenderables.ToArray());
                        }

                        layoutGrid = new Grid(); // .NET garbage collector will handle discarded objects
                        layoutGrid.AddColumn();
                        layoutGrid.AddEmptyRow();
                        layoutGrid.AddRow(new Rule("[bold]SORTATION HUB[/]").LeftJustified());
                        layoutGrid.AddEmptyRow();
                        layoutGrid.AddRow(hudGrid);
                        layoutGrid.AddEmptyRow();
                        layoutGrid.AddRow(new Rule("[bold]STATIONS[/]").LeftJustified());
                        layoutGrid.AddEmptyRow();
                        layoutGrid.AddRow(stationGrid);

                        // Update the screen
                        ctx.UpdateTarget(layoutGrid);
                    }
                });

            // 4. Results
            AnsiConsole.MarkupLine($"[bold red]SIMULATION ENDED.[/] Survived for {currentTick} ticks.\n");
            SimulationResult result = ResultFactory.Create(0, hub, currentTick >= SimulationConfig.ShiftLengthSeconds, currentTick);
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
            //CSVLogger.Log("simulation_data.csv", result);
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
                //CSVLogger.Log("simulation_data.csv", result);
                
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