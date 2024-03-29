﻿using System;
using System.Collections.Generic;
using System.Linq;
using SharpOSC;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using WebSocketSharp;
using System.Diagnostics;


namespace TulaOSC
{
    class Program
    {

        private static PerformanceCounter perfCPUCounter;
        private static PerformanceCounter perfMemCounter;
        private static DateTime time;
        private static float minutes;
        private static float hours;
        private static string localhost;
        private static int port;
        private static string wsUrl;
        private static System.Timers.Timer aTimer;
        private static float CPU;
        private static float RAM;

        static void Main(string[] args)
        {
            StreamReader r = new StreamReader("./config.json");
            string jsonString = r.ReadToEnd();
            jsonConfig m = JsonConvert.DeserializeObject<jsonConfig>(jsonString);
            localhost = m.localhost;
            port = m.port;
            wsUrl = m.wsUrl;

            GetHR();
            GetTime();

        }

        private static void GetTime()
        {

            while (true)
            {

                /*  TESTING GPU USAGE
                   
                  perfGPUCounter = new PerformanceCounter("GPU Engine", "Utilization Percentage","pid_25376_luid_0x00000000_0x0000F44A_phys_0_eng_0_engtype_3D");
                  perfGPUCounter.NextValue();
                 perfGPUMemoryCounter = new PerformanceCounter("GPU Adapter Memory", "Dedicated Usage", "luid_0x00000000_0x0000F44A_phys_0");
                 Console.WriteLine("Sent GPUMemory On usage :" + (ulong)perfGPUMemoryCounter.NextValue()/1000000 + " MB");
                 Console.WriteLine("Sent GPU Usage % :" + (int)perfGPUCounter.NextValue());*/
                //perfCPUCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                perfCPUCounter = new PerformanceCounter("Processor", "% Idle Time", "_Total");
                perfCPUCounter.NextValue();
                Thread.Sleep(1000);
                CPU = 100-(int)perfCPUCounter.NextValue();
                perfMemCounter = new PerformanceCounter("Memory", "Available MBytes");
                time = DateTime.Now;
                minutes = time.Minute;
                hours = time.Hour;

                var message = new OscMessage("/avatar/parameters/timeH", hours / 25);
                var sender = new UDPSender(localhost, port);
                sender.Send(message);

                var gpuCounters = GetGPUCounters();
                var gpuUsage = GetGPUUsage(gpuCounters);
                var gpuMEMCounters = GetGPUMEMCounters();
                var gpuMEMUsage = GetGPUMEMUsage(gpuMEMCounters);
                
                message = new OscMessage("/avatar/parameters/timeM", minutes / 200);
                sender.Send(message);
                Console.WriteLine("Sent: " + hours + ":" + minutes);


                Console.WriteLine("Sent Cpu Usage % :" + CPU + " %");
                message = new OscMessage("/avatar/parameters/CpuUsage", CPU/100 );
                sender.Send(message);
                Console.WriteLine(CPU/100);
                

                RAM = (int)perfMemCounter.NextValue();
                Console.WriteLine("Sent Memory Free :" + Math.Round(RAM*.001,3) + " GB");
                message = new OscMessage("/avatar/parameters/MemoryFree", (float)Math.Round(RAM* .001,3)/64);
                sender.Send(message);
                Console.WriteLine(Math.Round(RAM* .001, 3) / 64);

                Console.WriteLine("Sent GPU usage : " + Math.Round(gpuUsage,0)+ " %");
                message = new OscMessage("/avatar/parameters/GpuUsage", (float)Math.Round(gpuUsage, 0)/100);
                sender.Send(message);
                Console.WriteLine(Math.Round(gpuUsage, 0) / 100);
                
                Console.WriteLine("Sent GPU MEM : " + Math.Round(gpuMEMUsage*.000000001,2)+" GB");
                message = new OscMessage("/avatar/parameters/Vram", (float)Math.Round(gpuMEMUsage * .000000001, 2) / 16);
                sender.Send(message);
                Console.WriteLine(Math.Round(gpuMEMUsage * .000000001, 2) / 16);
                



                Thread.Sleep(10000);
            }
        }
        private static void GetHR()
        {
            try
            {
                var ws = new WebSocket(wsUrl);
                ws.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                ws.OnMessage += OnMessageWS;

                ws.Connect();

            }
            catch (Exception ex)
            { Console.WriteLine(ex.ToString()); }


        }

        private static void OnMessageWS(object sender, MessageEventArgs e)
        {
            if (aTimer != null)
            {
                aTimer.Stop();
                aTimer.Dispose();
            }
            jsonStromno m = JsonConvert.DeserializeObject<jsonStromno>(e.Data.ToString());
            Console.WriteLine("Heart Rate : " + m.data.heartRate);

            var message = new OscMessage("/avatar/parameters/hr_connected", true);
            var s = new UDPSender(localhost, port);
            s.Send(message);
            message = new OscMessage("/avatar/parameters/hr_percent", m.data.heartRate / 200);
            s.Send(message);

            SetTimer();


        }

        private static void SetTimer()
        {
            // Create a timer with a two second interval.
            aTimer = new System.Timers.Timer(10000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
        }
        private static void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            var message = new OscMessage("/avatar/parameters/hr_connected", false);
            var s = new UDPSender(localhost, port);
            s.Send(message);
        }

        public static List<PerformanceCounter> GetGPUCounters()
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var counterNames = category.GetInstanceNames();

            var gpuCounters = counterNames
                                .Where(counterName => counterName.EndsWith("engtype_3D"))
                                .SelectMany(counterName => category.GetCounters(counterName))
                                .Where(counter => counter.CounterName.Equals("Utilization Percentage"))
                                .ToList();

            return gpuCounters;
        }
        public static float GetGPUUsage(List<PerformanceCounter> gpuCounters)
        {
            gpuCounters.ForEach(x => x.NextValue());
            Thread.Sleep(1000);
            var result = gpuCounters.Sum(x => x.NextValue());

            return result;
        }

        public static List<PerformanceCounter> GetGPUMEMCounters()
        {
            var category = new PerformanceCounterCategory("GPU Adapter Memory");
            var counterNames = category.GetInstanceNames();

            var gpuCounters = counterNames
                                .Where(counterName => counterName.EndsWith("phys_0"))
                                .SelectMany(counterName => category.GetCounters(counterName))
                                .Where(counter => counter.CounterName.Equals("Dedicated Usage"))
                                .ToList();

            return gpuCounters;
        }
        public static float GetGPUMEMUsage(List<PerformanceCounter> gpuCounters)
        {
            gpuCounters.ForEach(x => x.NextValue());
            Thread.Sleep(1000);
            var result = gpuCounters.Sum(x => x.NextValue());

            return result;
        }
    }

    public class jsonConfig
    {
        public string localhost { get; set; }
        public string widgetId { get; set; }
        public int port { get; set; }
        public string wsUrl { get; set; }
    }
    public class jsonStromno
    {
        public string timestamp { get; set; }
        public jsonHR data { get; set; }
    }
    public class jsonHR
    {
        public float heartRate { get; set; }
    }

}
