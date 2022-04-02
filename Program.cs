using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpOSC;
using System.Threading;
namespace TulaOSC
{
    class Program
    {
        private static DateTime time;
        private static float Minutos;
        private static float Horas;
        
        static void Main(string[] args)
        {
            getTime();
        }

        private static void getTime() {

            while (true) {
            Thread.Sleep(10000);
            time = DateTime.Now;
            Minutos = time.Minute;
            Horas = time.Hour;
            var message = new SharpOSC.OscMessage("/avatar/parameters/timeH",Horas/200);
            var sender = new SharpOSC.UDPSender("127.0.0.1", 9000);
            sender.Send(message);
            message = new SharpOSC.OscMessage("/avatar/parameters/timeM",Minutos/200);
            sender.Send(message);
            Console.WriteLine("Enviados");

            }
        }
    }
}
