using System.Collections;
using System.Collections.Concurrent;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using t1_frame.task.Utils;

namespace t1_frame.task
{
    internal class Program
    {
        //private static AutoResetEvent event_1 = new AutoResetEvent(false);
        private static volatile bool isComplete = true;
        static async Task Main(string[] args)
        {
            #region 历史记录
            //Console.WriteLine("Hello, World!");
            //using CancellationTokenSource source = new CancellationTokenSource();
            //CancellationToken token = source.Token;

            //var bufferBlock = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = 2});

            // Post several messages to the block.
            //for (int i = 0; i < 3; i++)
            //{
            //    var bu = await bufferBlock.SendAsync(i);
            //}

            //for (int i = 0; i < 10000; i++)
            //{
            //    ActionParallelAsync(dataList, GetNetherlandsInvoiceData);
            //}

            //foreach (var index in Enumerable.Range(0, 1))
            //{
            //    await ActionAsync(dataList, index, GetNetherlandsInvoiceData, 10, 3);
            //}
            //try
            //{
            //    await TestTask();
            //}
            //catch (Exception ex)
            //{

            //}
            
            //Console.WriteLine("task done!");
            #endregion

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var exception = (Exception)e.ExceptionObject;
                Console.WriteLine($"Unhandled exception: {exception.Message}");
                // 你可以在这里进行日志记录、资源清理等操作
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                // 处理未观察到的任务异常
                Console.WriteLine($"Unobserved exception: {e.Exception.Message}");
                e.SetObserved();  // 标记异常为已观察，防止应用崩溃
            };

            #region BlockingCollection
            //var del3 = delegate (string name, int callDuration)
            //{
            //    Console.WriteLine("Test method begins.");
            //    Thread.Sleep(callDuration);
            //    Console.WriteLine($"Notification received for: {name}");
            //    return "0";
            //};
            //var objStr = del3.DynamicInvoke("zz", 3000);
            //var tsk = del3.BeginInvoke("zz", 3000, null, null);
            //try
            //{
            //    TestException();
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //    throw;
            //}
            //var source = Enumerable.Range(0, 100).ToArray();

            //// Partition the entire source array.
            //var rangePartitioner = Partitioner.Create(0, source.Length, 10);

            //double[] results = new double[source.Length];

            //// Loop over the partitions in parallel.
            //Parallel.ForEach(rangePartitioner, (range, loopState) =>
            //{
            //    // Loop over each range element without a delegate invocation.
            //    for (int i = range.Item1; i < range.Item2; i++)
            //    {
            //        Console.WriteLine(source[i]);//results[i] = source[i] * Math.PI;
            //    }
            //});

            //using BlockingCollection<string> bc = new BlockingCollection<string>() ;
            //var bc = Channel.CreateBounded<string>(10000);
            //var bc = new BufferBlock<string>();
            //Task t2 = Task.Run(async () =>
            //{
            //    try
            //    {
            //        // Consume the BlockingCollection
            //        while (true)
            //        {
            //            //if (await bc.Reader.WaitToReadAsync())
            //            //{
            //            //    var str = await bc.Reader.ReadAsync();
            //            //    //if (bc.Reader.TryPeek(out var str))
            //            //        Console.WriteLine($"输出：{str}");
            //            //}
            //            //else
            //            //{
            //            //    Console.WriteLine("That's All!");
            //            //    break;
            //            //}
            //            if(await bc.OutputAvailableAsync())
            //            {
            //                var str = await bc.ReceiveAsync();
            //                Console.WriteLine($"输出：{str}");
            //            }
            //            else
            //            {
            //                Console.WriteLine("That's All!");
            //                break;
            //            }

            //        }
            //    }
            //    catch (InvalidOperationException)
            //    {
            //        // An InvalidOperationException means that Take() was called on a completed collection
            //        Console.WriteLine("InvalidOperationException That's All!");
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine("Exception That's All!");
            //    }
            //});

            #endregion

            var sources = Enumerable.Range(0, 100).ToArray();

            var printResult = new ActionBlock<string[]>(str =>
            {
                Console.WriteLine($"输出1：{string.Join(',', str)}");
            });

            var errorResult = new ActionBlock<string[]>(str =>
            {
                Console.WriteLine($"异常输出1：{string.Join(',', str)}");
            });

            var bc = new BatchBlock<string>(3);
            Task t2 = Task.Run(async () =>
            {
                try
                {
                    // Consume the BlockingCollection
                    while (true)
                    {
                        if (await bc.OutputAvailableAsync())
                        {
                            var str = await bc.ReceiveAsync();
                            Console.WriteLine($"输出0：{string.Join(',', str)}");
                        }
                        else
                        {
                            Console.WriteLine("That's All!");
                            break;
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    // An InvalidOperationException means that Take() was called on a completed collection
                    Console.WriteLine("InvalidOperationException That's All!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception That's All!");
                }
            });

            //bc.LinkTo(printResult, t => t.Length > 2);
            //bc.LinkTo(errorResult);
            Console.WriteLine("等待输入...");
            var flag = true;
            while (flag)
            {
                string str = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(str)) continue;
                if (str.ToLower() == "exit")
                {
                    flag = false;
                    //bc.Writer.TryComplete();
                    bc.Complete();
                    //tsk = bc.Completion.ContinueWith(async t => { printResult.Complete(); await Task.CompletedTask; }).Unwrap();
                    //await printResult.Completion;
                    break;
                }

                if (str.ToLower() == "clear")
                {
                    Console.Clear();
                    continue;
                }

                if (!isComplete)
                {
                    Console.WriteLine("上一次任务未处理完，请等待...");
                    continue;
                }

                isComplete = false;
                //await Task.Run(async () =>
                //{

                //}).ContinueWith(t => {
                //    isComplete = true;
                //});
                if (await bc.SendAsync(str))
                    isComplete = true;
                //if (await bc.Writer.WaitToWriteAsync())
                //{
                //    await bc.Writer.WriteAsync(str);
                //    isComplete = true;
                //}                               
            }

            //printResult.Completion.Wait();
            await t2;
            Console.WriteLine("Test method end.");

            #region Thread Task 
            // string returnValue = del3.EndInvoke(tsk);
            //var th = new Thread(() =>
            //{
            //    Thread.Sleep(5000);
            //    Console.WriteLine("this is a thread test");
            //});
            //th.Start();
            //Thread.Sleep(1000);
            //th.Interrupt();
            //th.Join();
            //Console.WriteLine("this is a thread test a");

            //var tsk = DoAsync();
            //var checkTsk = CheckState(tsk);
            //await Task.WhenAll(tsk, checkTsk);
            //Console.WriteLine($"{DateTimeOffset.Now} {tsk.Status} | {tsk.AsyncState}");
            //await tsk;
            //Console.WriteLine($"{DateTimeOffset.Now} {tsk.Status} | {tsk.AsyncState}");
            //var source1 = new CancellationTokenSource(1000);
            ////source1.CancelAfter(1000);
            //Console.WriteLine($"{DateTimeOffset.Now} task:{Task.CurrentId} thread:{Thread.CurrentThread.ManagedThreadId}");
            //Action<object> action = (object obj) =>
            //{
            //    Console.WriteLine($"{DateTimeOffset.Now}" + "Task={0}, obj={1}, Thread={2}",
            //    Task.CurrentId, obj,
            //    Thread.CurrentThread.ManagedThreadId);
            //    //throw new NotImplementedException();

            //    Thread.Sleep(2000);
            //    if (source1.Token.IsCancellationRequested)
            //    {
            //        source1.Token.ThrowIfCancellationRequested();
            //    }
            //    Console.WriteLine($"{DateTimeOffset.Now}" + "game over Task={0}, obj={1}, Thread={2}",
            //  Task.CurrentId, obj,
            //  Thread.CurrentThread.ManagedThreadId); 
            //};
            ////Task tsk = new Task(action, "alpha");
            //Task tsk = Task.Factory.StartNew(action, "beta", source1.Token);//
            //var ctsk = tsk.ContinueWith(t1 =>
            //{
            //    if(t1.Exception != null)
            //    {
            //        Console.WriteLine($"{DateTimeOffset.Now} {t1.Exception.Message}");
            //    }
            //});

            //Console.WriteLine($"{DateTimeOffset.Now} task:{Task.CurrentId} thread:{Thread.CurrentThread.ManagedThreadId} {tsk.Status} | {tsk.AsyncState}");
            ////tsk.Start();
            //Console.WriteLine($"{DateTimeOffset.Now} task:{Task.CurrentId} thread:{Thread.CurrentThread.ManagedThreadId} {tsk.Status} | {tsk.AsyncState}");
            //var checkTsk = CheckState(tsk);

            ////Console.WriteLine($"{DateTimeOffset.Now} {tsk.Status} | {tsk.AsyncState}");
            ////try
            ////{
            //    //tsk.Wait(500);
            //    //bool completed = tsk.IsCompleted;
            //    //if (!completed)
            //    //    Console.WriteLine("Timed out before task A completed.");
            //    await Task.WhenAll(tsk, checkTsk, ctsk);
            ////}
            ////catch (Exception ex)
            ////{
            ////    throw;
            ////}

            #endregion

            Console.ReadKey();
        }

        static void TestException()
        {
            try
            {
                Console.WriteLine("TestException try");
                throw new NotSupportedException();
            }
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //    throw ex;
            //}
            finally
            {
                Console.WriteLine("TestException finally ");
                throw new NotImplementedException();
            }
        }

        static async Task CheckState(Task tsk)
        {
            while (true)
            {
                Console.WriteLine($"{DateTimeOffset.Now} task:{Task.CurrentId} thread:{Thread.CurrentThread.ManagedThreadId} {tsk.Status} | {tsk.AsyncState}");
                if (tsk.Status == TaskStatus.RanToCompletion || tsk.Status == TaskStatus.Canceled || tsk.Status == TaskStatus.Faulted)
                {
                    Console.WriteLine($"{DateTimeOffset.Now} task:{Task.CurrentId} thread:{Thread.CurrentThread.ManagedThreadId} {tsk.Status} | {tsk.AsyncState}");
                    break;
                }
            }
        }

        static async Task<int> DoAsync()
        {
            await Task.Delay(2000);
            var isThreadPoolThread = Thread.CurrentThread.IsThreadPoolThread;
            Console.WriteLine($"[{DateTimeOffset.Now}]Is thread pool thread: {isThreadPoolThread}");
            //throw new NotImplementedException();
            Task txj = null;
            await txj;
            return 0;
        }

        static async Task TestTask()
        {
            var core = Environment.ProcessorCount;
            var dataList = Enumerable.Range(1, 10);
            int step = 1;
            int taskCount = 3;
            List<Task<int>> tasks = new List<Task<int>>();
            int startIndex = 0;
            var currEnumerable = dataList.Skip(startIndex).Take(step);
            while (currEnumerable.Any())
            {
                //tasks.Add(DoAsync());
                var tmpTask = Task.Factory.StartNew(async model =>
                {
                    return await DoAsync();
                }, null).Unwrap();
                tasks.Add(tmpTask);
                if (tasks.Count > taskCount)
                {
                    var resultList = await Task.WhenAll(tasks);
                    tasks.Clear();
                }

                startIndex += step;
                currEnumerable = dataList.Skip(startIndex).Take(step);
            }
            if (!tasks.IsNullOrEmpty())
            {
                await Task.WhenAll(tasks);
                tasks.Clear();
            }
        }

        static int CountBytes(string path)
        {
            byte[] buffer = new byte[1024];
            int totalZeroBytesRead = 0;
            using (var fileStream = File.OpenRead(path))
            {
                int bytesRead = 0;
                do
                {
                    bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                    totalZeroBytesRead += buffer.Count(b => b == 0);
                } while (bytesRead > 0);
            }

            return totalZeroBytesRead;
        }

        static void Produce(ITargetBlock<byte[]> target)
        {
            var rand = new Random();

            for (int i = 0; i < 100; ++i)
            {
                var buffer = new byte[1024];
                rand.NextBytes(buffer);
                target.Post(buffer);
            }

            target.Complete();
        }

        static async Task<int> ConsumeAsync(ISourceBlock<byte[]> source)
        {
            int bytesProcessed = 0;

            while (await source.OutputAvailableAsync())
            {
                byte[] data = await source.ReceiveAsync();
                bytesProcessed += data.Length;
            }

            return bytesProcessed;
        }

        private static async Task<int> GetNetherlandsInvoiceData(IEnumerable<int> sources, int startIndex)
        {
            //await Task.CompletedTask;
            
            foreach (int source in sources)
            {
                await Task.Delay(3);
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - {source} - {startIndex}");
                throw new NotImplementedException();
            }

            return startIndex;
        }

        private static async Task ActionParallelAsync<T>(IEnumerable<T> dataList, int index, Delegate aciton, int step = 10, int taskCount = 3)
        {
            if (dataList.IsNullOrEmpty()) return;
            //using AutoResetEvent event_1 = new AutoResetEvent(false);
            //using SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
            using CancellationTokenSource source = new CancellationTokenSource();
            int lockValue = 1;
            //ConcurrentBag<Task> tasks = new ConcurrentBag<Task>();
            ConcurrentDictionary<Task, int> tasks = new ConcurrentDictionary<Task, int>();
            int startIndex = 0;
            try
            {
                var currEnumerable = dataList.Skip(startIndex).Take(step);
                var tsk = Task.Run(async () =>
                {
                    await TaskExcuteAsync(tasks, source.Token);
                });

                while (currEnumerable.Any())
                {
                    if (tasks.Count >= taskCount)
                    {
                        if (Interlocked.CompareExchange(ref lockValue, 1, 0) == 1)
                        {
                            continue;
                        }

                        //Console.WriteLine($"task uncompleted count {tasks.Count(t => !t.IsCompleted)}");
                        //event_1.WaitOne();                  
                        //semaphore.Wait();
                        //Console.WriteLine($"task count {tasks.Count}");
                    }

                    //tasks.Add(GetNetherlandsInvoiceData(currEnumerable, startIndex));
                    //tasks.Add(aciton.Invoke(currEnumerable, startIndex));
                    //tasks.Add(Task.Run( async () =>
                    //{
                    //    var finishedTask = aciton.DynamicInvoke(currEnumerable, startIndex);
                    //    if (finishedTask is Task<int> tsk)
                    //    {
                    //        await tsk;
                    //    }
                    //}));

                    var obj = new { DataList = currEnumerable, Index = index };
                    var tmpTask = Task.Factory.StartNew(async model =>
                    {
                        var sourceList = model.CastAnonymous(obj);
                        var finishedTask = aciton.DynamicInvoke(sourceList.DataList, sourceList.Index);
                        if (finishedTask is Task<int> tsk)
                        {
                            await tsk;
                        }
                    }, obj).Unwrap();

                    if (tmpTask != null)
                    {
                        tasks.TryAdd(tmpTask, 0);
                    }

                    //tasks.Add(GetNetherlandsInvoiceData(currEnumerable, index));

                    startIndex += step;
                    currEnumerable = dataList.Skip(startIndex).Take(step);
                }

                if (!source.IsCancellationRequested)
                {
                    Console.WriteLine("try to cancel task");
                    await source.CancelAsync();
                }

                await tsk;
            }
            catch(Exception ex)
            {

            }
            finally
            {
                await source.CancelAsync();
            }

            async Task TaskExcuteAsync(ConcurrentDictionary<Task, int> tasks, CancellationToken stoppingToken)
            {
                while (!stoppingToken.IsCancellationRequested || tasks.Count > 0)
                {
                    if (tasks.Count <= 0) continue;
                    Task finishedTask = await Task.WhenAny(tasks.Keys);
                    //if (finishedTask is Task<int> tsk)
                    //{
                    //    var value = await tsk;
                    //    Console.WriteLine($"task remove {value}");
                    //}
                    //else
                    //{
                    //    await finishedTask;
                    //}
                    await finishedTask;

                    if(tasks.TryRemove(finishedTask, out _))
                    {
                        _ = Interlocked.Exchange(ref lockValue, 0);
                        //var mark = event_1.Set();                 
                        //semaphore.Release();
                    }

                    //Console.WriteLine($"task remove {mark}");
                }

                Console.WriteLine("jump out while");
            }
        }

        //private static volatile bool isComplete = true;
        private static async Task ActionAsync<T>(IEnumerable<T> dataList, int index, Delegate aciton, int step = 10, int taskCount = 3)
        {
            if (dataList.IsNullOrEmpty()) return;
            //using AutoResetEvent event_1 = new AutoResetEvent(false);
            //using SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
            using CancellationTokenSource source = new CancellationTokenSource();
            int lockValue = 1;
            //ConcurrentBag<Task> tasks = new ConcurrentBag<Task>();
            //ConcurrentDictionary<Task, int> tasks = new ConcurrentDictionary<Task, int>();
            var tasks = new BufferBlock<int>(new DataflowBlockOptions {BoundedCapacity = taskCount, MaxMessagesPerTask = taskCount });
            int startIndex = 0;
            try
            {
                var currEnumerable = dataList.Skip(startIndex).Take(step);
                var tsk = Task.Run(async () =>
                {
                    await TaskExcuteAsync(tasks, source.Token, 0);
                });
                var tsk1 = Task.Run(async () =>
                {
                    await TaskExcuteAsync(tasks, source.Token, 1);
                });

                //while (currEnumerable.Any())
                //{
                //    //if (tasks.Count >= taskCount)
                //    //{
                //    //    if (Interlocked.CompareExchange(ref lockValue, 1, 0) == 1)
                //    //    {
                //    //        continue;
                //    //    }

                //    //    //Console.WriteLine($"task uncompleted count {tasks.Count(t => !t.IsCompleted)}");
                //    //    //event_1.WaitOne();                  
                //    //    //semaphore.Wait();
                //    //    //Console.WriteLine($"task count {tasks.Count}");
                //    //}

                //    //tasks.Add(GetNetherlandsInvoiceData(currEnumerable, startIndex));
                //    //tasks.Add(aciton.Invoke(currEnumerable, startIndex));
                //    //tasks.Add(Task.Run( async () =>
                //    //{
                //    //    var finishedTask = aciton.DynamicInvoke(currEnumerable, startIndex);
                //    //    if (finishedTask is Task<int> tsk)
                //    //    {
                //    //        await tsk;
                //    //    }
                //    //}));

                //    var obj = new { DataList = currEnumerable, Index = index };
                //    var tmpTask = Task.Factory.StartNew(async model =>
                //    {
                //        var sourceList = model.CastAnonymous(obj);
                //        var finishedTask = aciton.DynamicInvoke(sourceList.DataList, sourceList.Index);
                //        if (finishedTask is Task<int> tsk)
                //        {
                //            await tsk;
                //        }
                //    }, obj).Unwrap();

                //    if (tmpTask != null)
                //    {
                //        var mark = await tasks.SendAsync(tmpTask);
                //        //Console.WriteLine($"{mark}");
                //        //tasks.TryAdd(tmpTask, 0);
                //    }

                //    //tasks.Add(GetNetherlandsInvoiceData(currEnumerable, index));

                //    startIndex += step;
                //    currEnumerable = dataList.Skip(startIndex).Take(step);
                //}

                var flag = true;
                while (flag)
                {
                    string str = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(str)) continue;
                    if (str.ToLower() == "exit")
                    {
                        flag = false;
                        break;
                    }

                    if (str.ToLower() == "clear")
                    {
                        Console.Clear();
                        continue;
                    }

                    //if (!isComplete)
                    //{
                    //    Console.WriteLine("上一次任务未处理完，请等待...");
                    //    continue;
                    //}

                    //isComplete = false;
                    //await Task.Run(async () =>
                    //{
                    //    var message = str;

                    //    var obj = new { DataList = new List<int> { int.Parse(message)}, Index = index };
                    //    var tmpTask = Task.Factory.StartNew(async model =>
                    //    {
                    //        var sourceList = model.CastAnonymous(obj);
                    //        var finishedTask = aciton.DynamicInvoke(sourceList.DataList, sourceList.Index);
                    //        if (finishedTask is Task<int> tsk)
                    //        {
                    //            await tsk;
                    //        }
                    //    }, obj).Unwrap();

                    //    if (tmpTask != null)
                    //    {
                    //        var mark = await tasks.SendAsync(tmpTask);
                    //        //Console.WriteLine($"{mark}");
                    //        //tasks.TryAdd(tmpTask, 0);
                    //    }

                    //}).ContinueWith(t => {
                    //    isComplete = true;
                    //});
                    var mark = await tasks.SendAsync(int.Parse(str));
                    Console.WriteLine($"send:{str} - {mark}");
                }

                //tasks.Complete();
                if (!source.IsCancellationRequested)
                {
                    Console.WriteLine("try to cancel task");
                    await source.CancelAsync();
                }
                //await tasks.Completion.ContinueWith(t => { 

                //});


               await tsk;
                await tsk1;   
                

            }
            catch(Exception ex)
            {

            }
            finally
            {
                await source.CancelAsync();

                //var enty = await tasks.ReceiveAsync();
                //await enty;
            }

            async Task TaskExcuteAsync(BufferBlock<int> tasks, CancellationToken stoppingToken, int index = 0)
            {
                while (!stoppingToken.IsCancellationRequested || await tasks.OutputAvailableAsync(stoppingToken))
                {
                    //Console.WriteLine($"task count {tasks.Count}");
                    //if (tasks.Count <= 0) continue;
                    await Task.Delay(1000);
                    var enty = await tasks.ReceiveAsync();
                    Console.WriteLine( $"{index} receive:"+ enty);
                    //await enty;

                    //Task finishedTask = await Task.WhenAny(tasks.Keys);
                    //if (finishedTask is Task<int> tsk)
                    //{
                    //    var value = await tsk;
                    //    Console.WriteLine($"task remove {value}");
                    //}
                    //else
                    //{
                    //    await finishedTask;
                    //}
                    //await finishedTask;

                    //if (tasks.TryRemove(finishedTask, out _))
                    //{
                    //    _ = Interlocked.Exchange(ref lockValue, 0);
                    //    //var mark = event_1.Set();                 
                    //    //semaphore.Release();
                    //}

                    //Console.WriteLine($"task remove {mark}");
                }

                Console.WriteLine("jump out while");
            }
        }

    }
}
