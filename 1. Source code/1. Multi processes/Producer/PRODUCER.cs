using System;
using System.IO;
using System.IO.Pipes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace PRODUCER
{
    class PRODUCER
    {
        #region SLEEP
        static TimeSpan RandomSleepTime() =>
        TimeSpan.FromMilliseconds(random() * 2000); /*  SCENARIO 1  */
        //TimeSpan.FromSeconds(rnd.Next(5, 15));      /*  SCENARIO 2  */
        //TimeSpan.Zero;                              /*  SCENARIO 3  */
        //TimeSpan.FromSeconds(1);
        //TimeSpan.FromSeconds(random() * 10);
        //TimeSpan.FromSeconds(random() * 2);
        //TimeSpan.FromSeconds(random() * (Math.Pow(2, -2 + random() * 3)););

        //TimeSpan.FromSeconds(random());
        //TimeSpan.FromMilliseconds(1);
        //TimeSpan.FromMilliseconds(random() * 100);
        //TimeSpan.FromMilliseconds(random() * 10);
        //TimeSpan.FromMilliseconds(random());
        //TimeSpan.FromMilliseconds(random() * 0.9);
        //TimeSpan.FromMilliseconds(0.5);
        //TimeSpan.FromMilliseconds(random() * random());

        //TimeSpan.FromMilliseconds(random() * 0.5);
        //TimeSpan.FromMilliseconds(random() * 0.1);
        //TimeSpan.FromMilliseconds(random() * 0.01);
        //TimeSpan.FromMilliseconds(0.001); //0.1 * 0.01 //1 micro second
        #endregion

        #region SETTINGS
        public const bool DEBUG = false, IsLinux = false, Beautify = true;
        #endregion

        #region COLOURING
        public static bool Pre_Print_Task_0 = false;
        public static bool Color_Pre_Print_Task_0 = false;

        public static bool Print_Task_0 = false;
        public static bool Color_Print_Task_0 = false;
        #endregion
        static Random rnd = new Random(); static double random() => rnd.NextDouble();

        static void Main(string[] args)
        {
            Console.Title = "PRODUCER"; //Process.GetCurrentProcess().ProcessName;
            if (!IsLinux) { Console.SetWindowSize(40, 3); Console.WindowTop = 0; }

            try
            {
                BeginSimulation();
            }
            catch { }
        }

        static int GetInput(TimeSpan duration)
        {
            Thread.Sleep(duration);
            int inputValue = rnd.Next(0, 9999);
            return inputValue;
        }

        static void BeginSimulation()
        {
            var GENESIS = Stopwatch.StartNew();

            #region Inter-Process Communication: DO NOT CHANGE
            var CLIENT = new NamedPipeClientStream("DataSeriesMonitor");
            CLIENT.Connect();
            StreamReader reader = new StreamReader(CLIENT);
            StreamWriter writer = new StreamWriter(CLIENT);
            #endregion

            #region Inter-Process Communication: sending our ProcessID to the Consumer
            writer.WriteLine(Process.GetCurrentProcess().Id);
            writer.Flush();
            #endregion

            #region Inter-Process Communication: DO NOT CHANGE
            while (true)
            {
                #endregion

                var sleepDuration = RandomSleepTime();

                if (DEBUG && Pre_Print_Task_0)
                {
                    if (!IsLinux && Color_Pre_Print_Task_0)
                    {
                        Console.BackgroundColor = ConsoleColor.DarkRed;
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    Console.WriteLine("<<< 0 >>>  Is about to sleep for {0} seconds",
                        sleepDuration.TotalSeconds);
                    if (!IsLinux && Color_Pre_Print_Task_0)
                    {
                        Console.ResetColor();
                    }
                }

                #region Inter-Process Communication: DO NOT CHANGE
                var input = GetInput(sleepDuration).ToString();
                writer.WriteLine(input);

                writer.Flush();
                #endregion

                var sendTime = GENESIS.Elapsed;

                if (DEBUG && Print_Task_0)
                {
                    if (!IsLinux && Color_Print_Task_0)
                    {
                        Console.BackgroundColor = ConsoleColor.Yellow;
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    Console.WriteLine("((( 0 )))  Sends at [{0}:{1}:{2}:{3}] value [{4}]",
                            sendTime.Hours.ToString("00"),
                            sendTime.Minutes.ToString("00"),
                            sendTime.Seconds.ToString("00"),
                            sendTime.Milliseconds.ToString("000"),
                            input);
                    if (!IsLinux && Color_Print_Task_0)
                    {
                        Console.ResetColor();
                    }
                }

                #region Inter-Process Communication: DO NOT CHANGE
            }
            #endregion
        }
    }
}