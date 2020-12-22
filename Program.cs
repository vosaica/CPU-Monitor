using Spectre.Console;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Timers;
using System.Threading;
using System.Management;


namespace CPUMonitor
{
    class Program
    {
        static float CPU = 0f;
        static float RAM = 0f;
        static float TotalRAM = 0f;

        static List<float> AvailableCPU = new List<float>();
        static List<float> AvailableRAM = new List<float>();

        protected static PerformanceCounter cpuCounter;
        protected static PerformanceCounter ramCounter;
        static List<PerformanceCounter> cpuCounters = new List<PerformanceCounter>();
        static int cores = 0;


        private static float RAMCapacity()
        {
            string Query = "SELECT Capacity FROM Win32_PhysicalMemory";
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(Query);

            UInt64 Capacity = 0;
            foreach (ManagementObject WniPART in searcher.Get())
            {
                Capacity += Convert.ToUInt64(WniPART.Properties["Capacity"].Value);
            }
            return (float)(Capacity / 1024 / 1024);
        }


        private static void UpdateCPU(object source, ElapsedEventArgs e)
        {
            float cpu = cpuCounter.NextValue();
            float sum = 0;
            foreach(PerformanceCounter c in cpuCounters)
            {
                sum = sum + c.NextValue();
            }
            sum = sum / (cores);
            float ram = ramCounter.NextValue();
            //Console.WriteLine(string.Format("CPU Value 1: {0}, cpu value 2: {1} ,ram value: {2}", sum, cpu, ram));
            CPU = sum;
            RAM = ram;
            AvailableCPU.Add(sum);
            AvailableRAM.Add(ram);
        }


        private static void Update()
        {
            try
            {
                Console.WriteLine("Measuring, please wait.");
                System.Timers.Timer t = new System.Timers.Timer(1200);
                t.Elapsed += new ElapsedEventHandler(UpdateCPU);
                t.Start();
                Thread.Sleep(1150);
                Console.Clear();
            }
            catch (Exception e)
            {
                Console.WriteLine("catched exception: " + e.Message);
            }
        }


        private static void Bar()
        {
            Thread.Sleep(1300);
            AnsiConsole.Progress().Start
            (
                ctx => 
                {
                    // Define tasks
                    var task1 = ctx.AddTask("[blue]CPU[/]");
                    var task2 = ctx.AddTask("[red]RAM[/]");
                    float LastCPU = 0f;
                    float LastRAM = 0f, PercentRAM = 0f;
                    while(true)
                    {
                        //Console.WriteLine("CPU: {0}, LastCPU: {1}", CPU, LastCPU);
                        PercentRAM = 100f - (RAM / TotalRAM * 100);
                        task1.Increment(CPU - LastCPU);
                        task2.Increment(PercentRAM - LastRAM);
                        LastCPU = CPU;
                        LastRAM = PercentRAM;
                        //Console.WriteLine("RAM: {2}, Percent: {0}, Last: {1}", PercentRAM, LastRAM, RAM);
                        Thread.Sleep(1000);
                    }
                }
            );
        }


        public static void ConsumeCPU()
        {
            int percentage = 60;
            if (percentage < 0 || percentage > 100)
                throw new ArgumentException("percentage");
            Stopwatch watch = new Stopwatch();
            watch.Start();
            while (true)
            {
                // Make the loop go on for "percentage" milliseconds then sleep the 
                // remaining percentage milliseconds. So 40% utilization means work 40ms and sleep 60ms
                if (watch.ElapsedMilliseconds > percentage)
                {
                    Thread.Sleep(100 - percentage);
                    watch.Reset();
                    watch.Start();
                }
            }
        }


        static void Main(string[] args)
        {
            cpuCounter = new PerformanceCounter();
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");

            foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
            {
                cores = cores + int.Parse(item["NumberOfCores"].ToString());
            }

            int procCount = System.Environment.ProcessorCount;
            for(int i = 0; i < procCount; i++)
            {
                System.Diagnostics.PerformanceCounter pc = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", i.ToString());
                cpuCounters.Add(pc);
            }

            TotalRAM = RAMCapacity();
#if false
            Thread c = new Thread(ConsumeCPU);
            c.IsBackground = true;
            c.Start();
#endif

            Thread u = new Thread(Update);
            u.Start();

            Thread b = new Thread(Bar);
            b.Start();
        }
    }
}
