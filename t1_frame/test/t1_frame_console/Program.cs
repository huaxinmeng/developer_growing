using BenchmarkDotNet.Columns;
using Newtonsoft.Json;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using t1_frame.core;
using t1_frame_console.DelegateSamples;
using static System.Net.Mime.MediaTypeNames;

namespace t1_frame_console
{
    internal class Program
    {
        private static Random rnd = new Random();
        static int breakIndex = 0;
        private static void Main(string[] args)
        {
            #region 数据结构

            // 数组
            var aryList = Enumerable.Range(0, 10).ToArray();  // 数组
            aryList[3] = 15;
            var aryList1 = Enumerable.Range(0, 10).ToList();  // 数组
            //aryList1.Add(15);
            aryList1[3] = 15;
            aryList1.AddRange(Enumerable.Range(10, 1));
            // var len = 
            var tempList = new List<int>();

            var aryOrList = Array.CreateInstance(typeof(int), 10);  // 数组
            aryOrList.SetValue(1, 0);

            var list = new ArrayList();  // 数组
            var ary = new int[1]; //Array.Copy()
            var mtlist = Enumerable.Range(0, 10).ToImmutableList(); // 平衡二叉树
            var mtlist1 = Enumerable.Range(0, 10).ToImmutableArray();  // 数组
            // var mtlist2 = Enumerable.Range(0, 10).ToImmutableList();
            var newList = mtlist.Add(15);
            var newList1 = mtlist1.Add(15);
            var valu = mtlist[9];

            var pList = ArrayPool<int>.Shared;//.Create(10,  1); // 资源池
            var rList = pList.Rent(10);
            // rList[0] = 1;
            Span<int> usable = rList.AsSpan(0, 10);
            usable.Fill(3);
            pList.Return(rList);
            var rList1 = pList.Rent(10);
            rList1[0] = 2;
            pList.Return(rList1);

            var bitList = new BitArray(10);  // 数组
            bitList[1] = true;

            using var mList = MemoryPool<int>.Shared;
            var mrList = mList.Rent(10).Memory;
            // Memory<int> memory = mrList.Memory;  // 获取 Memory<T>
            // 切片（安全，不分配新数组）
            Span<int> slice = mrList.Slice(0, 10).Span;
            slice.Fill(3);
            mList.Dispose();
            var mrList1 = mList.Rent(10).Memory;
            var slice1 = mrList1.Slice(0, 1).Span;
            slice1.Fill(2);
            
            var queList = new Queue<int>(); //Queue // 数组
            queList.Enqueue(10);
            queList.Enqueue(11);
            queList.Enqueue(12);

            var mqueList = ImmutableQueue.Create(queList.ToArray());  // 单向链表
            var mqueList1 = mqueList.Enqueue(69);

            var staList = new Stack<int>();  // Stack // 数组
            staList.Push(10);
            staList.Push(11);

            var mstaList = ImmutableStack.Create(staList.ToArray()); // 单向链表
            var mstaList1 = mstaList.Push(69);

            var nodeList = new LinkedList<int>();   // 双向链表
            nodeList.AddLast(10);
            nodeList.AddLast(20);
            nodeList.AddLast(36);
            nodeList.AddLast(10);
            nodeList.AddAfter(nodeList.FindLast(10), 12);

            var kv = new Hashtable(); // Bucket数组
            kv.Add("1", "11");
            kv.Add("2", "22");
            kv.Add("3", "33");
            kv.Add(1, "44");
            Console.WriteLine("Hashtable -------");
            foreach (DictionaryEntry kv1 in kv) // 无序的
            {
                Console.WriteLine($"{kv1.Key} - {kv1.Value}");
            }

            var kvDic = new Dictionary<string, string>(); // Bucket数组 哈希表 + 链表 + 有序
            kvDic.Add("4", "11");
            kvDic.Add("1", "11");
            kvDic.Add("2", "22");
            kvDic.Add("3", "33");
            Console.WriteLine("Dictionary -------");
            foreach (var kv1 in kvDic)
            {
                Console.WriteLine($"{kv1.Key} - {kv1.Value}");
            }
         
            var mkvDic = kvDic.ToImmutableDictionary();   // 哈希表（基于数组+链表/树） ??
            var mkvDic1 = mkvDic.Add("6", "9");

            var sortList = new SortedList();  //SortedList<string, string> // 数组 + 二分查找  + 有序
            sortList.Add("4", "11");
            sortList.Add("2", "22");
            sortList.Add("1", "11");
            sortList.Add("3", "33");
            Console.WriteLine("SortedList -------");
            foreach (DictionaryEntry kv1 in sortList)
            {
                Console.WriteLine($"{kv1.Key} - {kv1.Value}");
            }

            var ordList = new OrderedDictionary(); // 哈希表 + 有序
            ordList.Add("4", "11");
            ordList.Add("2", "22");
            ordList.Add("1", "11");
            ordList.Add("3", "33");

            var sortDic = new SortedDictionary<string, string>();  // 红黑树 平衡二叉树 ??
            sortDic.Add("4", "11");
            sortDic.Add("2", "22");
            sortDic.Add("1", "11");
            sortDic.Add("3", "33");
            var msortDic = sortDic.ToImmutableSortedDictionary();   // 平衡二叉树（AVL树）
            var msortDic1 =  msortDic.Add("6", "9");

            var hasetList = new HashSet<int>();  //  -- Bucket数组 哈希表 + 链表 + 有序
            hasetList.Add(4);
            hasetList.Add(1);
            hasetList.Add(2);
            hasetList.Add(3);
            hasetList.Add(1);

            var mhasetList = hasetList.ToImmutableHashSet();  //哈希表（基于数组+链表/树） ?? 
            var mhasetList1 = mhasetList.Add(9);

            Console.WriteLine("HashSet -------");
            foreach (var kv1 in hasetList)
            {
                Console.WriteLine($"{kv1}");
            }

            var setList = new SortedSet<int>(); // 红黑树 平衡二叉树
            setList.Add(4);
            setList.Add(1);
            setList.Add(2);
            setList.Add(3);
            setList.Add(1);

            var msetList = setList.ToImmutableSortedSet(); //平衡二叉树（AVL树）
            var msetList1 = msetList.Add(9);

            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("---------------------------------------------");
            var cList = new BlockingCollection<string>(5);
            /*
             *  AutoResetEvent的合适应用场景为：一条线程开门，只解除一条线程阻塞的情况。
                ManualResetEvent的合适应用场景为：一条线程开门，可以解除多条线程阻塞的情况。
                CountdownEvent的合适应用场景为：多条线程全部完毕才可以（自动导致）开门，也就是一条线程等待多条线程的情况。
             */
            //var cList = new ConcurrentBag<string>();
            // var _waitHandle = new AutoResetEvent(true); // 一次性只能释放一个阻塞  内核模式
            //var _waitHandle = new ManualResetEvent(false); // 一次性释放所有阻塞   内核模式
            // var _waitHandle = new ManualResetEventSlim(false); // 一次性释放所有阻塞  混合模式
            //var _waitHandle = new Semaphore(0, 1); // 一次性释放可选的阻塞  内核模式
            //var _waitHandle = new SemaphoreSlim(0,1); // 一次性释放可选的阻塞  混合模式
            // var _waitHandle = new Mutex(false, "a"); // 一次性只能释放一个阻塞 (不会自动恢复阻塞)  内核模式
            // var _waitHandle = new CountdownEvent(2);  // 计数多少后一次性释放所有阻塞 混合模式  内部使用了ManualResetEventSlim 适合一条线程等待多条线程的情形
            var lockObj = new object();
            var chanlList = Channel.CreateBounded<string>(10);
            var _waitHandle = new Barrier(3);
            Task.Run(async () =>
            {
                await Task.CompletedTask;
                while (true)
                {
                    await Task.Delay(10000);
                    _waitHandle.SignalAndWait();
                    Console.WriteLine($"[task1] {DateTime.Now.ToLocalTime()} : {_waitHandle.ParticipantCount}");
                    ////_waitHandle.WaitOne();
                    ////await _waitHandle.WaitAsync();
                    ////_waitHandle.Wait();//等待多次会有些问题
                    ////_waitHandle.Wait();
                    //Monitor.Enter(lockObj);
                    //// var value = cList.Take();
                    //// Console.WriteLine($"{DateTime.Now.ToShortTimeString()} : 未获取到数据");
                    ////if (cList.TryTake(out var value))
                    //if (chanlList.Reader.TryRead(out var value))
                    //{
                    //    Console.WriteLine($" [task1] {DateTime.Now.ToLocalTime()} : {value}");
                    //}
                    //else
                    //{
                    //    await Task.Delay(1000);
                    //    // continue;
                    //    Console.WriteLine($"[task1] {DateTime.Now.ToLocalTime()} : 未获取到数据");
                    //}

                    ////_waitHandle.Reset();
                    ////_waitHandle.Release();

                    ////_waitHandle.ReleaseMutex();
                    //Monitor.Exit(lockObj);

                    await DoTask(lockObj, chanlList, "task1");

                    await Task.Delay(1000);
                    //_waitHandle.RemoveParticipant();
                }
            });

            Task.Run(async () =>
            {
                await Task.CompletedTask;
                while (true)
                {
                    await Task.Delay(10000);
                    _waitHandle.SignalAndWait();
                    Console.WriteLine($"[task2] {DateTime.Now.ToLocalTime()} : {_waitHandle.ParticipantCount}");
                    await DoTask(lockObj, chanlList);

                    await Task.Delay(1000);
                    // _waitHandle.RemoveParticipant();
                }
            });

            Task.Run(async () =>
            {
                await Task.CompletedTask;
                while (true)
                {
                    await Task.Delay(10000);
                    _waitHandle.SignalAndWait();
                    Console.WriteLine($"[task3] {DateTime.Now.ToLocalTime()} : {_waitHandle.ParticipantCount}");
                    ////_waitHandle.WaitOne();
                    ////await _waitHandle.WaitAsync();
                    ////_waitHandle.Wait(); //等待多次会有些问题
                    ////_waitHandle.Wait();
                    //Monitor.Enter(lockObj);
                    //// var value = cList.Take();
                    //// Console.WriteLine($"{DateTime.Now.ToShortTimeString()} : 未获取到数据");
                    ////if (cList.TryTake(out var value))
                    //if (chanlList.Reader.TryRead(out var value))
                    //{
                    //    Console.WriteLine($"[task3] {DateTime.Now.ToLocalTime()} : {value}");
                    //}
                    //else
                    //{
                    //    await Task.Delay(1000);
                    //    // continue;
                    //    Console.WriteLine($"[task3] {DateTime.Now.ToLocalTime()} : 未获取到数据");
                    //}

                    ////_waitHandle.Reset();
                    ////_waitHandle.Release();

                    ////_waitHandle.ReleaseMutex();

                    //Monitor.Exit(lockObj);

                    await DoTask(lockObj, chanlList, "task3");  //存在问题
                    //SyncDoTask(lockObj, chanlList, "task3");

                    await Task.Delay(1000);
                    //_waitHandle.RemoveParticipant();
                }
            });

            #endregion

            Console.WriteLine("等待输入...");
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

                if(str.ToLower() == "set")
                {
                    //_waitHandle.Set();
                    //_waitHandle.Release();
                    //_waitHandle.Signal();
                    //Monitor.Pulse(lockObj);
                    continue;
                }

                // cList.Add(str);
                // Console.WriteLine($"{DateTime.Now.ToShortTimeString()} : {cList.Count}");
                if (chanlList.Writer.TryWrite(str))
                {
                    Console.WriteLine($"{DateTime.Now.ToShortTimeString()} : {chanlList.Reader.Count}");
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now.ToShortTimeString()} : 未写进数据");
                }
            }

                Console.ReadKey();
            Console.ReadKey();
            #region 舍弃
            //new ExpressionTest().Test();
            //Random random = new Random(100);
            //Random randomq = new Random(100);

            //for (int i = 0; i < 1000; i++)
            //{
            //    Console.WriteLine($"{random.Next(101).ToString("D2")} - {randomq.Next(101).ToString("D2")}");
            //}
            //var summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());
            //var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
            //
            //
            //var list1 = Enumerable.Range(0, 5).ToList(); //new List<int>() { 1, 2 };
            //Console.WriteLine($"list1 {list1.Capacity}");
            //list1.Add(6);
            //Console.WriteLine($"list1 {list1.Capacity}");
            //var list = new List<FileTestEntity>(100000000);
            //Console.WriteLine($"list {list.Capacity}");
            //list.Insert(0, new FileTestEntity { box_url = "0"});
            //list.Insert(1, new FileTestEntity { box_url = "1" });
            //var varyListq = list.ToArray();
            //var varyList = list.ToImmutableList();
            //var tmpList = varyList.Add(new FileTestEntity { box_url = "300" });
            //list[0].box_url = "3";// = new FileTestEntity { box_url = "3" };
            //Console.WriteLine($"list {list.Capacity}");

            //var dic = new Dictionary<string, string>();

            //dic.Add("jb", "0");
            //dic.Add("nj", "1");
            //dic.Add("cf", "2");
            //dic.Add("op", "3");
            //dic.Add("ju", "4");
            //dic.Add("it", "5");
            //dic.Add("hy", "6");

            //foreach (var item in dic)
            //{
            //    Console.WriteLine($"{item.Key} {item.Key.GetHashCode()} {item.Key.GetHashCode() % 7}");
            //}

            //var st =  new SimpleStartT1Module().GetType().GetCustomAttributes(false);
            //var manager = new NfpModuleManager();
            //manager.Initialize(typeof(StartT1Module));
            //manager.StartModules();

            #endregion she

            #region 集合
            //int val = 0;
            ////box_url = Guid.NewGuid().ToString("N")
            ////*** Add | AddRange ***
            //var dataList = new List<TestEntity>(4);
            //dataList.AddRange(Enumerable.Range(0, 10)
            //    .Select(t =>
            //    {
            //        var url = Guid.NewGuid().ToString("N");
            //        var obj = new TestEntity
            //        {
            //            bag_count = 9 - t,
            //            board_number = $"NO{t}",
            //            box_url = url,
            //            box_info = new FileTestEntity
            //            {
            //                box_url = url
            //            }
            //        };
            //        return obj;
            //    }));
            //int value = dataList.Capacity;
            ////dataList.AddRange(Enumerable.Range(10, 10)
            ////    .Select(t => new TestEntity { bag_count = t % 10, board_number = $"NO{t % 10}", box_url = Guid.NewGuid().ToString("N") }));

            ////var newList = dataList.Distinct(new DynamicEqualityComparer<TestEntity>((x, y) => x.board_number == y.board_number));           
            ////newList = newList.DistinctBy(t => t.board_number);
            ////dataList.Add(null);

            ////*** AsReadOnly ***
            //ICollection<TestEntity> newList = dataList.AsReadOnly();
            ////newList.Add(null);
            ////dataList.Add(null);
            //Console.WriteLine($"{dataList[0].GetStr0()}");
            //Console.WriteLine($"{((ITest)dataList[0]).GetStr("256")}");
            //Console.WriteLine($"{((IService)dataList[0]).GetStr("256")}");

            ////dataList[2].board_number = $"NO{7}";
            ////dataList[7].board_number = $"NO{2}";

            ////*** BinarySearch *** 二分法查找
            //var index = dataList.BinarySearch(0, 6, dataList[9], new TestEntityComparer());
            //index = dataList.BinarySearch(new TestEntity() { board_number = $"NO{2}" }, new TestEntityComparer());

            ////*** Contains ***
            //var isExist = dataList.Contains(new TestEntity());
            //isExist = dataList.Contains(new TestEntity() { board_number = $"NO{2}" }, new DynamicEqualityComparer<TestEntity>((x, y) => x.board_number == y.board_number));

            ////*** ConvertAll ***
            //var tpList = dataList.ConvertAll(t => (t.bag_count, t.board_number));

            ////*** CopyTo ***
            //var copyList = new TestEntity[dataList.Count];
            //dataList.CopyTo(0, copyList, 0, dataList.Count);

            ////*** EnsureCapacity ***
            //value = dataList.EnsureCapacity(5);

            ////*** Equals ***
            //var mark = dataList.Equals(copyList.ToList());
            ////*** Exists ***
            //mark = dataList.Exists(t => t.bag_count == 0);
            ////goto logic;
            ////*** Find ***
            //dataList.Insert(dataList.Count - 2, new TestEntity { bag_count = 3, board_number = $"NO{3}" });            
            //var enity = dataList.Find(t => t.bag_count == 3);
            //enity = dataList.FindLast(t => t.bag_count == 3);
            //value = dataList.FindIndex(4, t => t.bag_count >= 3);
            //value = dataList.IndexOf(new TestEntity { bag_count = 3, board_number = $"NO{3}" }, 0,6);
            //value = dataList.IndexOf(dataList[3], 4, 6);

            //var rangeList = dataList.GetRange(0, 2);
            //rangeList = dataList;
            //dataList[0].box_info.box_urlname = "test";

            //dataList.RemoveAt(8);
            ////dataList.Reverse(0, 5);

            //dataList.Sort(0, 5, new TestEntityComparer());
            //value = dataList.Capacity;
            //dataList.TrimExcess();
            //value = dataList.Capacity;

            //mark = dataList.TrueForAll(t => t.bag_count < 5);

            ////goto logic;
            ////*** Contains ***
            //foreach (var t in dataList)
            //{

            //    if (t.bag_count == 3)
            //    {

            //        continue;//continue,break,return
            //    }

            //    Console.WriteLine($"foreach {t.bag_count}");
            //}
            //dataList.ForEach(t =>
            //{
            //    if (t.bag_count == 3)
            //    {
            //        return;
            //    }

            //    Console.WriteLine($"ForEach {t.bag_count}");
            //});

            ////goto logic;
            ////*** Contains ***

            ////var dictionary = dataList.ToFrozenDictionary(t=> t.bag_count.ToString());
            ////dictionary.TryAdd("one", new TestEntity());

            //var dictionary = dataList.ToFrozenSet(new DynamicEqualityComparer<TestEntity>((x, y) => x.board_number == y.board_number));

            //var chunkList = dataList.Chunk(3);
            //dataList.Clear();
            //var tmpList = dataList.DefaultIfEmpty(new TestEntity() { bag_count = 100});

            ////dataList.GroupJoin()
            //goto logic;

            #endregion

            #region collection
            //    var arry1 = new int[] { 1, 2, 2, 4, 5 }; //2
            //    var arry2 = new int[] { 3, 2, 6, 1, 7 }; //3

            //    var arry1List = arry1
            //        .Select(t =>
            //        {
            //            var url = Guid.NewGuid().ToString("N");
            //            var obj = new TestEntity
            //            {
            //                bag_count = t,
            //                board_number = $"NO{t}",
            //                box_url = url,
            //                box_info = new FileTestEntity
            //                {
            //                    box_url = url
            //                }
            //            };
            //            return obj;
            //        }).ToList();
            //    var arry2List = arry2
            //        .Select(t =>
            //        {
            //            var url = Guid.NewGuid().ToString("N");
            //            var obj = new TestEntity
            //            {
            //                bag_count = t,
            //                board_number = $"NO{t}",
            //                box_url = url,
            //                box_info = new FileTestEntity
            //                {
            //                    box_url = url
            //                }
            //            };
            //            return obj;
            //        }).ToList();

            //    //*** Aggregate ***
            //    var enity = arry1List.Aggregate((x, y) => new TestEntity { bag_count = x.bag_count + y.bag_count });
            //    var valInt = arry1List.Aggregate(0, (x, y) => x + y.bag_count, x => 2 * x);
            //    //var strList = arry1List.Cast<object>().ToList();
            //    var tmpList = arry1List.Concat(arry2List);
            //    tmpList = tmpList.DistinctBy(t => t.bag_count).ToList();
            //    enity = tmpList.ElementAtOrDefault(new Index(8, true));
            //    enity = tmpList.ElementAtOrDefault(new Index(8));
            //    //tmpList = arry1List.Except(arry2List, new DynamicEqualityComparer<TestEntity>((x, y) => x.board_number == y.board_number));
            //    tmpList = arry1List.ExceptBy(arry2, x => x.bag_count);

            //    enity = arry1List.FirstOrDefault(t => t.bag_count == 2);
            //    enity = arry1List.LastOrDefault(t => t.bag_count == 2);
            //    enity = arry1List.SingleOrDefault(t=> t.bag_count == 7);
            //    //tmpList = arry1List.IntersectBy(arry2, x => x.bag_count);
            //    tmpList = arry1List.Intersect(arry2List, new DynamicEqualityComparer<TestEntity>((x, y) => x.board_number == y.board_number));
            //    //tmpList = arry1List.Union(arry2List, new DynamicEqualityComparer<TestEntity>((x, y) => x.board_number == y.board_number));
            //    tmpList = arry1List.UnionBy(arry2List, x => x.bag_count);
            //    tmpList = arry2List.Join(arry1List, t => t.bag_count, v => v.bag_count, (t, v) => t).ToList();

            //    //var groupList = arry1List.GroupBy(t => t.bag_count % 2);
            //    var groupList = arry2List.GroupJoin(arry1List, t => t.bag_count, v => v.bag_count, (t, v) => new
            //    {
            //        t,
            //        v
            //    }).ToList();

            //    tmpList = arry1List.SkipLast(3).Take(3).ToList();
            //    tmpList = arry1List.Skip(3).Take(3).ToList();

            //    tmpList = arry1List.SkipWhile(t => t.bag_count != 4).ToList();

            //    //tmpList

            //    goto logic;
            //    //*** Contains ***
            //    goto logic;
            //    //*** Contains ***
            //    goto logic;
            //    //*** Contains ***
            //    goto logic;
            //    //*** Contains ***
            //    goto logic;
            //    //*** Contains ***
            //    goto logic;
            //    //*** Contains ***
            //    goto logic;
            //    //*** Contains ***
            //    goto logic;
            //    //*** Contains ***
            //    goto logic;
            //    //*** Contains ***
            //    goto logic;
            ////*** Contains ***

            #endregion


            #region 表达式
            //var obj = new ActionTestProxy(new ActionTest());
            //Console.WriteLine($"direct value:{obj.GetStrByNormal("hello", "world!")}");
            //Console.WriteLine($"reflection value:{obj.GetStrByReflection("hello", "world!")}");
            //Console.WriteLine($"delegate value:{obj.GetStrByDelegate("hello", "world!")}");
            //Console.WriteLine($"emit value:{obj.GetStrByEmit("hello", "world!")}");
            //Console.WriteLine($"expression value:{obj.GetStrByExpression("hello", "world!")}");

            //var value1 = 10.3M;
            //var value2 = 3.6d;
            //var i = value1 + (decimal)value2;
            //new ActionTest().GetStr("hello", "world!");
            //   int lockValue = 1;
            //   var value = Interlocked.CompareExchange(ref lockValue, 1, 0);
            //   var value1 = Interlocked.Exchange(ref lockValue, 0);
            //   var valu2 = Interlocked.CompareExchange(ref lockValue, 1, 0);
            //   var value3 = Interlocked.Exchange(ref lockValue, 0);


            //   var aName = new AssemblyName("test");
            //   var ab = AssemblyBuilder.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);
            //   ModuleBuilder mb = ab.DefineDynamicModule(aName.Name ?? "test");
            //   TypeBuilder tb = mb.DefineType("ActionTest1", TypeAttributes.Public | TypeAttributes.Class);
            //   ConstructorBuilder ctor0 = tb.DefineConstructor(
            //MethodAttributes.Public,
            //CallingConventions.Standard,
            //Type.EmptyTypes);
            //   ConstructorInfo? ci = typeof(object).GetConstructor(Type.EmptyTypes);
            //   ILGenerator ctor0IL = ctor0.GetILGenerator();
            //   ctor0IL.Emit(OpCodes.Ldarg_0);
            //   //ctor0IL.Emit(OpCodes.Ldc_I4_S, 42);
            //   ctor0IL.Emit(OpCodes.Call, ci);
            //   ctor0IL.Emit(OpCodes.Ret);

            //   MethodBuilder meth = tb.DefineMethod(
            //   "GetStr",
            //   MethodAttributes.Public,
            //   typeof(string),
            //   new Type[] { typeof(string), typeof(string) });
            //   meth.DefineParameter(1, ParameterAttributes.In, "value1");
            //   meth.DefineParameter(2, ParameterAttributes.In, "value2");
            //   Type[] writeStringArgs = { typeof(string), typeof(string) };
            //   MethodInfo writeString = typeof(String).GetMethod("Concat",BindingFlags.Public | BindingFlags.Static, writeStringArgs);

            //   ILGenerator methIL = meth.GetILGenerator();
            //   methIL.Emit(OpCodes.Ldarg_1);
            //   methIL.Emit(OpCodes.Ldstr, " ");
            //   methIL.Emit(OpCodes.Ldarg_2);
            //   methIL.EmitCall(OpCodes.Call, writeString, null);
            //   methIL.Emit(OpCodes.Stloc_0);
            //   methIL.Emit(OpCodes.Br_S);
            //   methIL.Emit(OpCodes.Ldloc_0);
            //   methIL.Emit(OpCodes.Ret);

            //   // Finish the type.
            //   Type? t = tb.CreateType(); //typeof(ActionTest);

            //   // Because AssemblyBuilderAccess includes Run, the code can be
            //   // executed immediately. Start by getting reflection objects for
            //   // the method and the property.
            //   MethodInfo? mi = t?.GetMethod("GetStr");
            //   //PropertyInfo? pi = t?.GetProperty("Number");

            //   // Create an instance of MyDynamicType using the default
            //   // constructor.
            //   object? o1 = null;
            //   if (t is not null)
            //       o1 = Activator.CreateInstance(t);

            //   var val = o1.GetHashCode();

            //   // Call MyMethod, passing 22, and display the return value, 22
            //   // times 127. Arguments must be passed as an array, even when
            //   // there is only one.
            //   //object[] arguments = { "hello", "world!" };
            //   //Console.WriteLine("o1.GetStr(hello,world!): {0}",
            //   //    mi?.Invoke(o1, arguments));


            //   Dictionary<string, int> testDic = new Dictionary<string, int>(100);
            //   //foreach (var item in testDic.Keys)
            //   //{

            //   //}


            //   foreach (var item in Enumerable.Range(1, 100))
            //   {
            //       testDic.TryAdd($"key{item}", item);
            //   }

            //   var elKey = $"key30";
            //   var elValue = testDic[elKey]; 
            //   var index = elKey.GetHashCode() % 107;
            //   var tmpValue = testDic.ElementAt(index);

            //var val = testDic.Values[index];
            #endregion

            var obj = new ActionTestProxy(new ActionTest());
            var bj = typeof(ActionTest).GetTypeInfo();

            //Console.WriteLine($"direct value:{obj.GetStrByNormal("hello", "world!")}");
            //Console.WriteLine($"reflection value:{obj.GetStrByReflection("hello", "world!")}");
            //Console.WriteLine($"delegate value:{obj.GetStrByDelegate("hello", "world!")}");
            //Console.WriteLine($"emit value:{obj.GetStrByEmit("hello", "world!")}");

            Console.WriteLine($"expression value:{obj.GetStrByExpression("hello", "world!")}");

            //var json = new LinqTest().test();
            //Expression<Func<int, bool>> exprTree = num => num < 5;
            //new Utils().ResolveUpdateExpress(() => new TestEntity
            //{
            //    board_number = "TEST",
            //    bag_count = 100,
            //    //valid_date = DateTime.UtcNow.AddHours(8),
            //    //valid_date = DateTime.Now,
            //    valid_date = new DateTime(1992, 8, 26)
            //});
            // t.bag_count > 10 && t.board_number == "TEST" && t.valid_date > DateTime.Now && t.box_url.StartsWith("tey")
            //&& t.is_valid == true && t.bag_count > 10 && t.board_number == "TEST" && t.valid_date > DateTime.Now && t.box_url.StartsWith("tey") == false

            //List<string> vael = ["sjioid", "455454","568"];
            //new Utils().ResolveToSql(t => t.bag_count > t.bag_count - 1 
            //&& t.is_valid == true 
            //&& 10 < t.bag_count   
            //&& t.board_number == null 
            //&& t.valid_date > DateTime.Now 
            //&& false == t.box_url.StartsWith("tey")  
            //&& t.box_urlname.StartsWith("url") 
            //&& t.is_first
            //&& t.box_urlname.Contains("url")
            //&& vael.Contains(t.box_url)
            //);

            //var objsyt = new TestEntity
            //{
            //    board_number = "TEST",
            //    bag_count = 100,
            //    //valid_date = DateTime.UtcNow.AddHours(8),
            //    //valid_date = DateTime.Now,
            //    valid_date = new DateTime(1992, 8, 26)
            //};
            //var st1 = "apple";
            //var st2 = "oringe";
            //FormattableString strlo = $" this is {objsyt.board_number} or {objsyt.bag_count}"; 

            //var templateText = strlo.Format;
            int coreCount = Environment.ProcessorCount;
            try
            {
                #region 并发
                //List<string> aryList = new List<string>();
                //var str10 = aryList?[0];
                // var parse = s => int.Parse(s);
                //await TestBig(coreCount, 100000);
                //breakIndex = 3;//rnd.Next(1, 11);
                var lenght = 10;
                //var partitioner = Partitioner.Create<int>(Enumerable.Range(0, lenght), EnumerablePartitionerOptions.NoBuffering);

                //var sources = partitioner.GetPartitions(10);
                //var sources = partitioner.GetOrderableDynamicPartitions();
                //foreach (var source in sources)
                //{
                //    Console.WriteLine("----------------------------");
                //    //while (source.MoveNext())
                //    //{
                //    //    Console.WriteLine(source.Current);
                //    //}

                //    Console.WriteLine("----------------------------");
                //}

                //await Parallel.ForEachAsync(sources, async (source, c) =>
                //{
                //    while (source.MoveNext())
                //    {
                //        Console.WriteLine(source.Current);
                //    }

                //    await Task.CompletedTask;
                //});

                //for (var i = 0; i < lenght; i++)
                //{
                //    Method2(i);
                //}

                var result = Parallel.For(0, lenght, new ParallelOptions() { MaxDegreeOfParallelism = 2 * coreCount }, Method3);
                // Parallel.ForEach(partitioner, new ParallelOptions() { MaxDegreeOfParallelism = 2 * coreCount }, Method3);
                // await Parallel.ForAsync(0, lenght, new ParallelOptions() { MaxDegreeOfParallelism = 2 * coreCount }, Method3);
                // await Parallel.ForEachAsync(partitioner.GetDynamicPartitions(), new ParallelOptions() { MaxDegreeOfParallelism = 2 * coreCount }, Method3);

                //if (result.LowestBreakIteration.HasValue)
                //    Console.WriteLine($"\nLowest Break Iteration: {result.LowestBreakIteration}");
                //else
                //    Console.WriteLine($"\nNo lowest break iteration.");

                #endregion

                // var ins = Singleton.Instance;
                // Console.WriteLine($"Parallel index {0} {ins.GetHashCode()}");
            }
            catch (Exception ex)
            {

            }

        logic:
            Console.ReadKey();

        }

        private static async Task DoTask(object lockObj, Channel<string> chanlList, string mark = "task2")
        {
            await Task.CompletedTask;
            //await Task.Delay(10000);
            //_waitHandle.WaitOne();
            //await _waitHandle.WaitAsync();
            //_waitHandle.Wait(); //等待多次会有些问题
            //_waitHandle.Wait();
            //Monitor.Enter(lockObj);
            //Monitor.Wait(lockObj);
            // var value = cList.Take();
            // Console.WriteLine($"{DateTime.Now.ToShortTimeString()} : 未获取到数据");
            //if (cList.TryTake(out var value))
            if (chanlList.Reader.TryRead(out var value))
            {
                Console.WriteLine($"[{mark}] {DateTime.Now.ToLocalTime()} : {value}");
                // await Task.CompletedTask;
            }
            else
            {
                // await Task.Delay(1000);
                // continue;
                Console.WriteLine($"[{mark}] {DateTime.Now.ToLocalTime()} : 未获取到数据");
                // await Task.CompletedTask;
            }

            //_waitHandle.Reset();
            //_waitHandle.Release();

            //_waitHandle.ReleaseMutex();
            // Monitor.Exit(lockObj);
            //Monitor.Pulse(lockObj);
        }

        private static void SyncDoTask(object lockObj, Channel<string> chanlList, string mark = "task2")
        {
            Monitor.Enter(lockObj);
            // var value = cList.Take();
            // Console.WriteLine($"{DateTime.Now.ToShortTimeString()} : 未获取到数据");
            //if (cList.TryTake(out var value))
            if (chanlList.Reader.TryRead(out var value))
            {
                Console.WriteLine($"[{mark}] {DateTime.Now.ToLocalTime()} : {value}");
            }
            else
            {
                // await Task.Delay(1000);
                // continue;
                Console.WriteLine($"[{mark}] {DateTime.Now.ToLocalTime()} : 未获取到数据");
            }

            //_waitHandle.Reset();
            //_waitHandle.Release();

            //_waitHandle.ReleaseMutex();
            Monitor.Exit(lockObj);
        }

        static void Method2(int i)
        {
            Console.WriteLine($"index {i}");
        }
        static void Method3(int i, ParallelLoopState state)
        {
            // Console.WriteLine($"Parallel index {i} {c.LowestBreakIteration}");
            var ins =  Singleton.Instance;
            Console.WriteLine($"Parallel index {i} {ins.GetHashCode()}");
            //Console.WriteLine($"Beginning iteration {i}");
            //int delay;
            //lock (rnd)
            //    delay = rnd.Next(1, 1001);
            //Thread.Sleep(delay);

            //if (state.ShouldExitCurrentIteration)
            //{
            //    if (state.LowestBreakIteration < i)
            //    {
            //        Console.WriteLine($"skip in iteration {i} {state.LowestBreakIteration}");
            //        return;
            //    }
            //}

            //if (state.IsExceptional)
            //{
            //    Console.WriteLine($"skip in iteration {i} {state.LowestBreakIteration}");
            //    return;
            //}

            //if (i == breakIndex)
            //{
            //    Console.WriteLine($"Break in iteration {i}");
            //    // state.Break();

            //    throw new Exception("Break");
            //}

            //Console.WriteLine($"Completed iteration {i}");

        }

        static async ValueTask Method3(int i, CancellationToken token)
        {
            // Console.WriteLine($"Parallel index {i} {c.LowestBreakIteration}");

            Console.WriteLine($"Beginning iteration {i}");
            int delay;
            lock (rnd)
                delay = rnd.Next(1, 1001);
            Thread.Sleep(delay);

            //if (state.ShouldExitCurrentIteration)
            //{
            //    if (state.LowestBreakIteration < i)
            //    {
            //        Console.WriteLine($"skip in iteration {i} {state.LowestBreakIteration}");
            //        return;
            //    }
            //}

            //if (i == breakIndex)
            //{
            //    Console.WriteLine($"Break in iteration {i}");
            //    state.Break();
            //}

            if (token.IsCancellationRequested)
            {

            }

            Console.WriteLine($"Completed iteration {i}");

        }
    }
}