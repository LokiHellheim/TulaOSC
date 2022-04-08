using System;
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

                perfCPUCounter = new PerformanceCounter("Processor Information", "% Processor Time", "_Total");
                perfMemCounter = new PerformanceCounter("Memory", "Available MBytes");
                time = DateTime.Now;
                minutes = time.Minute;
                hours = time.Hour;
                var message = new OscMessage("/avatar/parameters/timeH", hours / 25);
                var sender = new UDPSender(localhost, port);
                sender.Send(message);
                message = new OscMessage("/avatar/parameters/timeM", minutes / 200);
                sender.Send(message);
                Console.WriteLine("Sent: " + hours + ":" + minutes);
                Console.WriteLine((int)perfCPUCounter.NextValue());
                Console.WriteLine((int)perfMemCounter.NextValue());
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
                
            }catch (Exception ex)
            { Console.WriteLine(ex.ToString()); }

           
        }
        
        private static void OnMessageWS(object sender, MessageEventArgs e)
        {
            if(aTimer != null){
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
