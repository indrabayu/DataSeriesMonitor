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

namespace CONSUMER
{
    class CONSUMER
    {
        #region SETTINGS
        public const bool DEBUG = false, IsLinux = false, Beautify = true;

        public const GC_Possition Possition_GC = GC_Possition.After_Gen2;
        public const GC_Threading Thread_GC = GC_Threading.SameThread;
        /* 
         * [IF ProcessOnlyGen2 == TRUE]
         *      [Task2's ThreadCycleTimeSpan] == 
         *          [ConsiderOutdated <- true]
         *              + [StopwatchForRawInputs <- TimeConsideration.ComputationAndWait]
         * [END IF]
         */
        public const bool ProcessOnlyGen2 = true;
        #endregion

        #region SETTINGS FOR EXPERIMENTS
        /* BLOCKING */
        const bool Blocking_GC = true;

        /* NON */
        //const bool Blocking_GC = false;

        /* EFFECTIVE */
        const bool ConsiderOutdated = false; const Timing StopwatchForRawInputs = Timing.None;

        /* OVERALL   */
        //const bool ConsiderOutdated = true; const Timing StopwatchForRawInputs = Timing.ComputationAndWait;
        #endregion

        #region GC
        static Stopwatch sw_gc = new Stopwatch();
        static void gc()
        {
            sw_gc.Restart();
            GC.Collect(2, GCCollectionMode.Forced, Blocking_GC, Blocking_GC);
            GC.WaitForPendingFinalizers();
            gc_time = sw_gc.Elapsed;
            sw_gc.Stop();
        }

        static TimeSpan gc_time = TimeSpan.Zero;
        #endregion

        #region COLOURING
        public static bool Print_Task_1 = false;
        public static bool Color_Print_Task_1 = false;

        public static bool Post_Print_Task_1 = false;
        public static bool Color_Post_Print_Task_1 = false;

        public static bool Print_Task_2 = true;
        public static bool Color_Print_Task_2 = false;
        #endregion

        static void Main(string[] args)
        {
            Console.Title = "CONSUMER";
            if (!IsLinux) { Utils.MaximizeConsole(); /*Console.WindowWidth = 140; Console.SetWindowPosition(0, 0);*/ }

            try
            {
                BeginSimulation();
            }
            catch { }
        }

        static void BeginSimulation()
        {
            #region Inter-Process Communication: DO NOT CHANGE
            var SERVER = new NamedPipeServerStream("DataSeriesMonitor");
            SERVER.WaitForConnection();
            StreamReader reader = new StreamReader(SERVER);
            StreamWriter writer = new StreamWriter(SERVER);
            #endregion

            #region Inter-Process Communication: receiving the Producer's ProcessID
            int Producer_ProcessID = Convert.ToInt32(reader.ReadLine());
            writer.Flush();
            #endregion

            TimeSpan ThreadCycleTimeSpan = TimeSpan.FromMilliseconds(1000);

            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            const int InvalidAfterSeconds = 10;
            TimeSpan InvalidAfterTimeSpan = TimeSpan.FromSeconds(InvalidAfterSeconds);

            var concurrentQueueOfBridgeWith_1 = new ConcurrentQueue<KeyValuePair<TimeSpan, int>>();
            var concurrentQueueOfBridgeWith_2 = new ConcurrentQueue<KeyValuePair<TimeSpan, int>>();

            SemaphoreSlim semaBridgeWith_1 = new SemaphoreSlim(0);
            SemaphoreSlim semaBridgeWith_2 = new SemaphoreSlim(0);

            var GENESIS = Stopwatch.StartNew();
            double latency = 0;

            Action bridging = () =>
            {
                #region Inter-Process Communication: DO NOT CHANGE
                while (true)
                {
                    #endregion

                    #region Inter-Process Communication: DO NOT CHANGE
                    var inputString = reader.ReadLine();
                    writer.Flush();
                    #endregion

                    var timeStamp = GENESIS.Elapsed;
                    var input = new KeyValuePair<TimeSpan, int>(timeStamp, Convert.ToInt32(inputString));

                    concurrentQueueOfBridgeWith_1.Enqueue(input);
                    concurrentQueueOfBridgeWith_2.Enqueue(input);

                    semaBridgeWith_1.Release();
                    semaBridgeWith_2.Release();

                    #region Inter-Process Communication: DO NOT CHANGE
                }
                #endregion
            };

            var task1 = Task.Run(() =>
            {
                do
                {
                    semaBridgeWith_1.Wait();
                    KeyValuePair<TimeSpan, int> input;
                    concurrentQueueOfBridgeWith_1.TryDequeue(out input);

                    if (DEBUG && Print_Task_1)
                    {
                        if (!IsLinux && Color_Print_Task_1)
                        {
                            Console.BackgroundColor = ConsoleColor.Blue;
                            Console.ForegroundColor = ConsoleColor.Black;
                        }
                        Console.WriteLine("((( 1 )))  Receives [{0}:{1}:{2}:{3}] value [{4}]",
                            input.Key.Hours.ToString("00"),
                            input.Key.Minutes.ToString("00"),
                            input.Key.Seconds.ToString("00"),
                            input.Key.Milliseconds.ToString("000"),
                            input.Value.ToString("0000"));
                        if (!IsLinux && Color_Print_Task_1)
                        {
                            Console.ResetColor();
                        }
                    }

                    if (false == DEBUG)
                    {
                        Console.Out.WriteLine(input.Value);
                    }

                    /*  Latency should be put here, right after the input is 
                     *  flushed to stream 1. 
                     */
                    latency = (GENESIS.Elapsed - input.Key).TotalSeconds;

                    if (DEBUG && Post_Print_Task_1) /* LOG */
                    {
                        if (!IsLinux && Color_Post_Print_Task_1)
                        {
                            Console.BackgroundColor = ConsoleColor.Cyan;
                            Console.ForegroundColor = ConsoleColor.Black;
                        }
                        Console.WriteLine("<<< 1 >>>  Latest latency is {0} seconds",
                            latency.ToString(".000000"));
                        if (!IsLinux && Color_Post_Print_Task_1)
                        {
                            Console.ResetColor();
                        }
                    }
                } while (true);
            });

            Task task2 = Task.Run(() =>
            {
                /* @totalTime
                 * @successfulIterations
                 * These two variables should be declared here instead at the beginning of the loop,
                 * because they have nothing to do with the 1-second-cycle thing,
                 * but instead with the performance between cycles. */
                TimeSpan totalTimeForComputation = TimeSpan.Zero;
                TimeSpan totalTimeForCompleteCycle = TimeSpan.Zero;
                int successfulIterations = 0;

                /* List won't stand against incoming inputs as frequent as 1 milliseconds
                 * since it would resize & copy to a new array whenever its array is full.
                 * Therefore, I decided to move to LinkedList. */
                //var gen2 = new List<KeyValuePair<TimeSpan, int>>(); 
                var gen2 = new LinkedList<KeyValuePair<TimeSpan, int>>();

                TimeSpan duration_outdating = TimeSpan.Zero;
                TimeSpan duration_gen2 = TimeSpan.Zero;
                Stopwatch sw_raw = new Stopwatch();

                TimeSpan hogging_gc_time = TimeSpan.FromMilliseconds(Blocking_GC ? 300 : 1);
                TimeSpan longest_gc_time = TimeSpan.Zero;
                int freq_hogging_gc = 0;

                do
                {
                    /*  To avoid inconsistency while determining the age,  
                     *  I use the simulated end time instead of keep checking current time. 
                     *  EndTime = StartTime + 1s */

                    var THREAD_START_TIME = GENESIS.Elapsed;
                    var TIME_AFTER_THREAD_ABORTION = THREAD_START_TIME.Add(ThreadCycleTimeSpan);
                    duration_outdating = TimeSpan.Zero;
                    duration_gen2 = TimeSpan.Zero;
                    latency = 0; //Do not forget to reset, otherwise the old value will keeping popping out when there's actually no input.

                    Thread oneSecondDurationThread = new Thread(() =>
                    {
                        double _Average = 0.0;
                        double _QuantizedTimeIntegral = 0.0;
                        TimeSpan _AgeOfOldestInput = TimeSpan.Zero;
                        TimeSpan _AgeOfYoungestInput = TimeSpan.Zero;

                        ulong total = 0;
                        ulong itemsCount = 0;

                        var back_QueueIter = new KeyValuePair<TimeSpan, int>();
                        var front_QueueIter = new KeyValuePair<TimeSpan, int>();

                        int removedItems = 0;
                        bool doneProcessingGen2 = false;

                        TimeSpan start_outdating = GENESIS.Elapsed;

                        try
                        {
                            for (int gen2count = gen2.Count - 1; gen2count >= 0; gen2count--)
                            {
                                //if ((TIME_AFTER_THREAD_ABORTION - gen2[0].Key) > InvalidAfterTimeSpan)
                                if ((TIME_AFTER_THREAD_ABORTION - gen2.First.Value.Key) > InvalidAfterTimeSpan)
                                {
                                    //gen2.RemoveAt(0);
                                    gen2.RemoveFirst();
                                    removedItems++;
                                }
                                else
                                {
                                    break;
                                }
                            }

                            duration_outdating = GENESIS.Elapsed - start_outdating;

                            ///////////////////////////////////////////////////////////////////////////////////
                            if (Possition_GC == GC_Possition.Before_Gen2)
                            {
                                if (Thread_GC == GC_Threading.SameThread)
                                    gc();
                                else if (Thread_GC == GC_Threading.ExtraThread)
                                    new Thread(() => gc()).Start();
                                else
                                    Task.Factory.StartNew(gc, TaskCreationOptions.AttachedToParent);
                            }
                            ///////////////////////////////////////////////////////////////////////////////////

                            var start_gen2 = GENESIS.Elapsed;
                            var back_Gen2Iter = gen2.GetEnumerator();
                            var front_Gen2Iter = gen2.GetEnumerator();

                            if (gen2.Count != 0)
                            {
                                front_Gen2Iter.MoveNext();
                                total = (ulong)front_Gen2Iter.Current.Value;
                                _AgeOfOldestInput = TIME_AFTER_THREAD_ABORTION - front_Gen2Iter.Current.Key;
                                _AgeOfYoungestInput = _AgeOfOldestInput;
                                itemsCount = 1;

                                while (front_Gen2Iter.MoveNext())
                                {
                                    back_Gen2Iter.MoveNext(); //i'm right behind the front iterator!

                                    _QuantizedTimeIntegral +=
                                        (front_Gen2Iter.Current.Key - back_Gen2Iter.Current.Key).TotalSeconds
                                            * front_Gen2Iter.Current.Value;

                                    total += (ulong)front_Gen2Iter.Current.Value;
                                    _AgeOfYoungestInput = TIME_AFTER_THREAD_ABORTION - front_Gen2Iter.Current.Key;
                                    itemsCount++;
                                }

                                if (gen2.Count != 0)
                                {
                                    back_Gen2Iter.MoveNext(); //points to the last element of Gen2
                                }
                                _AgeOfYoungestInput = TIME_AFTER_THREAD_ABORTION - back_Gen2Iter.Current.Key;
                            }

                            duration_gen2 = GENESIS.Elapsed - start_gen2;

                            doneProcessingGen2 = true;

                            ///////////////////////////////////////////////////////////////////////////////////
                            if (Possition_GC == GC_Possition.After_Gen2)
                            {
                                if (Thread_GC == GC_Threading.SameThread)
                                    gc();
                                else if (Thread_GC == GC_Threading.ExtraThread)
                                    new Thread(() => gc()).Start();
                                else
                                    Task.Factory.StartNew(gc, TaskCreationOptions.AttachedToParent);
                            }
                            ///////////////////////////////////////////////////////////////////////////////////

                            /* when ProcessOnlyGen2 = true, ideal: ConsiderOnlyWaitTimeForRawInputs = true */
                            if (ProcessOnlyGen2 == true)
                            {
                                if (StopwatchForRawInputs == Timing.ComputationAndWait)
                                {
                                    sw_raw.Restart();
                                }
                                else if (StopwatchForRawInputs == Timing.Computation)
                                {
                                    sw_raw.Reset();
                                }

                                do
                                {
                                    semaBridgeWith_2.Wait();
                                    if (StopwatchForRawInputs == Timing.Computation)
                                    {
                                        sw_raw.Start();
                                    }
                                    concurrentQueueOfBridgeWith_2.TryDequeue(out front_QueueIter);
                                    //gen2.Add(front_QueueIter);
                                    gen2.AddLast(front_QueueIter);
                                    if (StopwatchForRawInputs == Timing.Computation)
                                    {
                                        sw_raw.Stop();
                                    }
                                } while (true);
                            }

                            if (ProcessOnlyGen2 == false)
                            {
                                /* Activate this one line below, and comment out all lines within this IF statement
                                 * that uses the stopwatch for raw elements (sw_raw). */
                                if (StopwatchForRawInputs == Timing.ComputationAndWait)
                                {
                                    sw_raw.Restart();
                                }

                                semaBridgeWith_2.Wait();
                                if (StopwatchForRawInputs == Timing.Computation)
                                {
                                    sw_raw.Start();
                                }
                                concurrentQueueOfBridgeWith_2.TryDequeue(out front_QueueIter);

                                total += (ulong)front_QueueIter.Value;
                                if (gen2.Count == 0)
                                {
                                    _AgeOfOldestInput = TIME_AFTER_THREAD_ABORTION - front_QueueIter.Key;

                                    //the code for youngest is at the catch section
                                }
                                else
                                {
                                    //continuation from the last element of Gen2
                                    _QuantizedTimeIntegral +=
                                        (front_QueueIter.Key - back_Gen2Iter.Current.Key).TotalSeconds
                                            * front_QueueIter.Value;
                                }
                                itemsCount++;
                                //gen2.Add(front_QueueIter);
                                gen2.AddLast(front_QueueIter);
                                if (StopwatchForRawInputs == Timing.Computation)
                                {
                                    sw_raw.Stop();
                                }

                                do
                                {
                                    back_QueueIter = front_QueueIter;

                                    semaBridgeWith_2.Wait();
                                    if (StopwatchForRawInputs == Timing.Computation)
                                    {
                                        sw_raw.Start();
                                    }
                                    concurrentQueueOfBridgeWith_2.TryDequeue(out front_QueueIter);

                                    _QuantizedTimeIntegral +=
                                        (front_QueueIter.Key - back_QueueIter.Key).TotalSeconds
                                            * front_QueueIter.Value;
                                    total += (ulong)front_QueueIter.Value;
                                    itemsCount++;
                                    //gen2.Add(front_QueueIter);
                                    gen2.AddLast(front_QueueIter);
                                    if (StopwatchForRawInputs == Timing.Computation)
                                    {
                                        sw_raw.Stop();
                                    }
                                } while (true);
                            }
                        }
                        catch (ThreadAbortException)
                        {
                            double jitter = 0;
                            TimeSpan computationTimeOfCycle = TimeSpan.Zero;

                            #region stuffs that need to be present when printing
                            sw_raw.Stop();
                            TimeSpan timeTakenForThisCycle = GENESIS.Elapsed - THREAD_START_TIME;
                            if (StopwatchForRawInputs == Timing.None)
                            {
                                computationTimeOfCycle =
                                    (ConsiderOutdated ? duration_outdating : TimeSpan.Zero)
                                        + duration_gen2;
                            }
                            else
                            {
                                computationTimeOfCycle =
                                    (ConsiderOutdated ? duration_outdating : TimeSpan.Zero)
                                        + duration_gen2 + sw_raw.Elapsed;
                            }
                            totalTimeForCompleteCycle += timeTakenForThisCycle;
                            if ((ProcessOnlyGen2 == false) && (back_QueueIter.Key != TimeSpan.Zero)) //if we are done with Gen2 and have new items in the 10 seconds window...
                            {
                                _AgeOfYoungestInput = TIME_AFTER_THREAD_ABORTION - back_QueueIter.Key;
                            }
                            if (itemsCount != 0)
                            {
                                _Average = total / (double) itemsCount;
                                successfulIterations++;
                                totalTimeForComputation += computationTimeOfCycle;
                                jitter = totalTimeForComputation.TotalSeconds / successfulIterations;
                            }
                            #endregion

                            #region printing time...
                            if (DEBUG && !IsLinux && Color_Print_Task_2)
                            {
                                Console.BackgroundColor = ConsoleColor.DarkYellow;
                                Console.ForegroundColor = ConsoleColor.Cyan;
                            }
                            if (doneProcessingGen2) //something needs to be printed out regardless whether this cycle was done or not
                            {
                                if (DEBUG && Print_Task_2)
                                {
                                    Console.WriteLine("[avg {0}] [qti {1}] [old {2}s] [you {3}s] [lat {4}s] [jit {5}s] [{6} MB] [~ {7}ms] [{8} i/s] [gc {9}ms] [{10}]{11}",
                                            /*  0 */ _Average.ToString("000,000").Beautify(),
                                            /*  1 */ _QuantizedTimeIntegral.ToString("000,000").Beautify(),
                                            /*  2 */ _AgeOfOldestInput.TotalSeconds.ToString("00.000000").Beautify(),
                                            /*  3 */ _AgeOfYoungestInput.TotalSeconds.ToString("00.000000").Beautify(),
                                            /*  4 */ latency.ToString("00.0000000").Beautify(),
                                            /*  5 */ jitter.ToString("00.000000").Beautify(),
                                            /*  6 */ ((Process.GetCurrentProcess().WorkingSet64 + Process.GetProcessById(Producer_ProcessID).WorkingSet64) / 1048576 /* convert to Mega Byte */).ToString("000").Beautify(),
                                            /*  7 */ computationTimeOfCycle.TotalMilliseconds.ToString("0000.000").Beautify(),
                                            /*  8 */ itemsCount.ToString("0,000,000").Beautify(),
                                            /*  9 */ gc_time.TotalMilliseconds.ToString("0000.0000").Beautify(),
                                            /* 10 */ GENESIS.Elapsed.ToString("h'h 'm'm 's's'"),
                                            /* 11 */ doneProcessingGen2 ? "" : "<<"
                                        );
                                }
                                else
                                {
                                    Console.Out.WriteLine("{0} {1} {2} {3}",
                                            /*  0 */ _Average,
                                            /*  1 */ _QuantizedTimeIntegral,
                                            /*  2 */ _AgeOfOldestInput.TotalSeconds.ToString("00.000000"),
                                            /*  3 */ _AgeOfYoungestInput.TotalSeconds.ToString("00.000000")
                                        );
                                }
                            }
                            else
                            {
                                if (DEBUG && Print_Task_2)
                                {
                                    Console.WriteLine("Abandoning the data due to incomplete processing.");
                                }
                                else
                                {
                                    Console.Out.WriteLine("Abandoning the data due to incomplete processing.");
                                }
                            }

                            if (DEBUG)
                            {
                                #region GC related
                                var overallDuration = GENESIS.Elapsed;
                                if (gc_time > longest_gc_time)
                                {
                                    longest_gc_time = gc_time;
                                }
                                if (gc_time >= hogging_gc_time)
                                {
                                    freq_hogging_gc++;
                                }
                                #endregion

                                Console.Title = string.Format(
                                    "Time taken by Task2 outside the 1 second loop is {0}. Longest GC is {1}ms. GC >= {2}ms occurs {3}x. {4}",
                                                    (overallDuration - totalTimeForCompleteCycle).ToString(),
                                                    longest_gc_time.TotalMilliseconds,
                                                    hogging_gc_time.TotalMilliseconds,
                                                    freq_hogging_gc,
                                                    !ConsiderOutdated && StopwatchForRawInputs == Timing.None ?
                                                    "" : string.Format("Everything + GC = {0}ms", (int)(duration_outdating + duration_gen2 + sw_raw.Elapsed + gc_time).TotalMilliseconds));
                            }
                            if (DEBUG && !IsLinux && Color_Print_Task_2)
                            {
                                Console.ResetColor();
                            }
                            #endregion
                        }
                        catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                    });
                    oneSecondDurationThread.Priority = ThreadPriority.Normal;

                    oneSecondDurationThread.Start();
                    Thread.Sleep(ThreadCycleTimeSpan);

                    oneSecondDurationThread.Abort();
                    oneSecondDurationThread.Join();
                } while (true);
            });

            /* The bridging between the producer process and the consumers (task1 and task2)
             * is done in the main thread. */
            bridging();
        }
    }

    public enum Timing { None, Computation, ComputationAndWait };
    public enum GC_Possition { Before_Gen2, After_Gen2 };
    public enum GC_Threading { SameThread, ExtraThread, ExtraTask }
}