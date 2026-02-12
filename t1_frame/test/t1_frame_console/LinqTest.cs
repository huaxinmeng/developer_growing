using Newtonsoft.Json;
using System.Data.Common;

namespace t1_frame_console
{
    internal class LinqTest
    {
        public string test()
        {
            #region test

            //Console.WriteLine("Hello, World!");
            //var ty = typeof(T1ApiBaseTest);
            //var aryList = ty.GetProperties();
            //foreach ( var member in aryList )
            //{
            //    Console.WriteLine($"{member.Name} {Type.GetTypeCode(member.PropertyType).ToString()}");
            //}
            //var eventList = ty.GetEvents();
            //foreach (var member in eventList)
            //{
            //    Console.WriteLine($"{member.Name}");
            //}
            //var interfaceList = ty.GetInterfaces();
            //foreach (var member in interfaceList)
            //{
            //    Console.WriteLine($"{member.Name} {Type.GetTypeCode(member).ToString()}");
            //}
            //var sw = Stopwatch.StartNew();
            //List<int> dataList = new List<int>();
            //new List<string>() { "a", "b", "c" };
            //var result = Parallel.ForEach(aryList, t =>
            //{
            //    dataList.Add(GetStringAsync(t).Result);
            //});

            //2837
            //Parallel.ForEachAsync(aryList, async (t, _) =>
            //{
            //    dataList.Add(await GetStringAsync(t));
            //}).Wait();
            //sw.Stop();
            //Console.WriteLine(sw.ElapsedMilliseconds);
            ////while (!result.IsCompleted)
            ////{
            ////}

            //foreach (var value in dataList)
            //{
            //    Console.WriteLine($"index:{value}");
            //}

            //IEnumerator<int> enumerator = aryList.GetEnumerator();
            //try
            //{
            //    while (enumerator.MoveNext())
            //    {
            //        Console.WriteLine($"value:{enumerator.Current}");
            //    }
            //    enumerator.Dispose();
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.ToString());
            //}

            #endregion test

            #region 集合

            //var hashcodeint1 = "zxz".GetHashCode();
            //Console.WriteLine($"HashCode: {1} {hashcodeint1}");
            //var hashcodeint2 = "zxz".GetHashCode();
            //Console.WriteLine($"HashCode: {1} {hashcodeint2}");
            //Console.WriteLine($"hashcodeint1 is equre hashcodeint2 {hashcodeint1 == hashcodeint2}");

            //var hashcodeint3 = (new {t=3 }).GetHashCode();
            //Console.WriteLine($"HashCode: {new { }} {hashcodeint3}");
            //var hashcodeint4 = (new { t = 3}).GetHashCode();
            //Console.WriteLine($"HashCode: {new { }} {hashcodeint4}");
            //Console.WriteLine($"hashcodeint3 is equre hashcodeint4 {hashcodeint3 == hashcodeint4}");

            ////var aryList = Enumerable.Range(1, 10);
            //#region System.Collections
            //Console.WriteLine("********************************");
            //Console.WriteLine("test: System.Collections");
            //Console.WriteLine("********************************");
            //ArrayList aryList = new ArrayList();
            //aryList.Add(1);
            //aryList.Add(3);
            //aryList.Add(2);
            //aryList.Add(4);
            //aryList.Add(3);
            //aryList.Add(5);
            //aryList.Add(7);
            //aryList.Add(6);
            //aryList.Add(null);
            //aryList.Add(DateTime.Now);
            //aryList.Add(new { });
            //foreach (var value in aryList)
            //{
            //    Console.WriteLine($"ArrayList:{value}");
            //}

            //var table = new Hashtable();
            //table.Add(1, "mixi");
            //table.Add("zi", null);
            //table.Add(6, null);
            //table.Add(new { }, "mihu");
            //table.Add(new { t= 12 }, "miha");//--key不可重复
            //Console.WriteLine($"Hashtable:{table[new { t = 12 }]}");
            //Console.WriteLine($"Hashtable:{table[4]}");

            //var sortList = new SortedList();
            //sortList.Add(7, "mixi");
            //sortList.Add(8, null);
            //sortList.Add(6, "tnr a");
            ////sortList.Add(new { }, "mihu");
            ////sortList.Add(new { t = 12 }, "miha");//--key不可重复

            //int index = 0;
            //foreach (var key in sortList.Keys)  //DictionaryEntry
            //{
            //    var actKey = sortList.GetKey(index);
            //    Console.WriteLine($"SortedList:{index} {actKey} {sortList[actKey]}");
            //    Console.WriteLine($"SortedList:{index} {actKey} {sortList.GetByIndex(index)}");
            //    Console.WriteLine($"SortedList:{sortList[key]}");
            //    index++;
            //}

            //Queue myQ = new Queue();
            //myQ.Enqueue(1);
            //myQ.Enqueue("zi");
            //myQ.Enqueue(null);
            //myQ.Enqueue(6);
            //myQ.Enqueue("zi");
            //while (myQ.Count > 0)
            //{
            //    Console.WriteLine($"Queue:{myQ.Dequeue()}");
            //}

            //Stack stack = new Stack();
            //stack.Push(1);
            //stack.Push("zi");
            //stack.Push(null);
            //stack.Push(6);
            //while (stack.Count > 0)
            //{
            //    Console.WriteLine($"Stack:{stack.Pop()}");
            //}

            //#endregion

            //#region  System.Collections.Generic
            //Console.WriteLine("********************************");
            //Console.WriteLine("test: System.Collections.Generic");
            //Console.WriteLine("********************************");

            //Stack<int> stackT = new Stack<int>();
            //stackT.Push(1);
            //stackT.Push(3);
            //stackT.Push(8);
            //stackT.Push(6);
            //for (int i = 0; i < 10; i++)
            //{
            //    Console.WriteLine($"Stack<T>.Peek():{stackT.Peek()}");
            //}
            //while (stackT.Count > 0)
            //{
            //    Console.WriteLine($"Stack<T>:{stackT.Pop()}");
            //}

            //Queue<int> QueueQ = new Queue<int>();
            //QueueQ.Enqueue(1);
            //QueueQ.Enqueue(3);
            //QueueQ.Enqueue(8);
            //QueueQ.Enqueue(6);
            //while (QueueQ.Count > 0)
            //{
            //    Console.WriteLine($"Queue<T>:{QueueQ.Dequeue()}");
            //}

            //PriorityQueue<int, int> priorityQueue = new PriorityQueue<int, int>();
            //priorityQueue.Enqueue(6, 2);
            //priorityQueue.Enqueue(1,0);
            //priorityQueue.Enqueue(3, 1);
            //priorityQueue.Enqueue(8, 0);

            //while (priorityQueue.Count > 0)
            //{
            //    Console.WriteLine($"PriorityQueue<T,V>: {priorityQueue.Dequeue()}");
            //}

            //List<int> aryListT = new List<int>();
            //aryListT.Add(1);
            //aryListT.Add(3);
            //aryListT.Add(2);
            //aryListT.Add(4);
            //aryListT.Add(3);
            //aryListT.Add(5);
            //aryListT.Add(7);
            //aryListT.Add(6);
            //foreach (var value in aryListT)
            //{
            //    Console.WriteLine($"List<T>:{value}");
            //}

            //var tableDic = new Dictionary<int, string>();
            //tableDic.Add(6, null);
            //tableDic.Add(1, "mixi");
            //tableDic.Add(7, "mihu");
            //tableDic.Add(3, null);
            //foreach (var value in tableDic)
            //{
            //    Console.WriteLine($"Dictionary<T,V>:{value.Key}{value.Value}");
            //}

            //var tableDicS = new SortedDictionary<int, string>();
            //tableDicS.Add(6, null);
            //tableDicS.Add(1, "mixi");
            //tableDicS.Add(7, "mihu");
            //tableDicS.Add(3, null);
            //foreach (var value in tableDicS)
            //{
            //    Console.WriteLine($"SortedDictionary<T,V>:{value.Key}{value.Value}");
            //}

            //var sortListT = new SortedList<int, string>();
            //sortListT.Add(7, "mixi");
            //sortListT.Add(8, null);
            //sortListT.Add(6, "tnr a");
            ////sortList.Add(new { }, "mihu");
            ////sortList.Add(new { t = 12 }, "miha");//--key不可重复

            //foreach (var value in sortListT)  //DictionaryEntry
            //{
            //    Console.WriteLine($"SortedList<T,V>:{value.Key}{value.Value}");
            //}
            //#endregion

            #endregion 集合

            int[] numbers = Enumerable.Range(1, 4).ToArray();// { 1, 2, 3, 4 };
            string[] words = { "one", "two", "three" };
            int[] aryList = { 0, 3, 5 };//Enumerable.Range(1, 2).ToArray();
            var random = new Random();
            var randomList = Enumerable.Range(1, 4).Select(x => new TestEntity
            {
                board_number = $"no{x}",
                bag_count = random.Next(1, 100),
                //box_info = new FileTestEntity
                //{
                //    box_url = "http://127.0.0.1:80/test",
                //    box_urlname = $"name{x}"
                //},
                //attachments = new List<FileTestEntity> {
                //    new FileTestEntity
                //    {
                //        box_url = "http://127.0.0.1:4200/file",
                //        box_urlname = $"name{x*x*x}"
                //    }
                //    }
            }).ToList();

           return JsonConvert.SerializeObject(randomList);

            var objList = new List<TestEntity>()
            {
                new TestEntity
                {
                     board_number = $"no{3}",
                     bag_count = random.Next(1, 100),
                //     box_info = new FileTestEntity
                //     {
                //     box_url = "http://127.0.0.1:80/test",
                //     box_urlname = $"name{100}"
                //     }
                //     ,
                //attachments = new List<FileTestEntity> {
                //    new FileTestEntity
                //    {
                //        box_url = "http://127.0.0.1:4200/file",
                //        box_urlname = $"name{100*100*100}"
                //    }
                //    }
                },
                new TestEntity
                {
                     board_number = $"no{6}",
                     bag_count = random.Next(1, 100),
                //     box_info = new FileTestEntity
                //     {
                //     box_url = "http://127.0.0.1:80/test",
                //     box_urlname = $"name{101}"
                //     }
                //     ,
                //attachments = new List<FileTestEntity> {
                //    new FileTestEntity
                //    {
                //        box_url = "http://127.0.0.1:4200/file",
                //        box_urlname = $"name{101*101*101}"
                //    }
                //    }
                },
                new TestEntity
                {
                     board_number = $"no{7}",
                     bag_count = random.Next(1, 100),
                //     box_info = new FileTestEntity
                //     {
                //     box_url = "http://127.0.0.1:80/test",
                //     box_urlname = $"name{7}"
                //     }
                //     ,
                //attachments = new List<FileTestEntity> {
                //    new FileTestEntity
                //    {
                //        box_url = "http://127.0.0.1:4200/file",
                //        box_urlname = $"name{7*7*7}"
                //    }
                //    }
                }
            };

            //var numbersAndWords = numbers.Zip(words, (first, second) => first + " " + second);
            //var tp = numbers.Zip(words, aryList);
            //var filterList = numbers.Where((t,index) => t % 2 == 0 && index == 3);
            //var valueList = numbers.Union(aryList);
            //var valueList = randomList.Union(objList, new DynamicEqualityComparer<TestEntity>((t,v) => t.board_number == v.board_number && t.box_urlname == v.box_urlname));
            //var valueList = randomList.UnionBy(objList, t => t.box_urlname);
            //var valueList = randomList.UnionBy(objList, t => t.box_info, new DynamicEqualityComparer<FileTestEntity>((t,v) => t.box_urlname == v.box_urlname));
            //var valueList = randomList.Union(objList, new DynamicEqualityComparer<TestEntity>((t,v) => t.box_info.box_url == v.box_info.box_url));
            //var valueList = randomList.UnionBy(objList, t => t.box_info);
            //var kvList = valueList.ToLookup(t => t.box_info.box_urlname);
            //var kvList = valueList.ToLookup(t => t.box_info, new DynamicEqualityComparer<FileTestEntity>((t, v) => t.box_urlname == v.box_urlname));
            //var kvList = valueList.ToLookup(t => t.box_info.box_urlname, t=> t.board_number);
            //var kvList = valueList.ToLookup(t => t.box_info, t => t.board_number, new DynamicEqualityComparer<FileTestEntity>((t, v) => t.box_urlname == v.box_urlname));
            //var targList = valueList.ToHashSet();
            //var targList = valueList.ToHashSet(new DynamicEqualityComparer<TestEntity>((t, v) => t.board_number == v.board_number && t.box_info.box_urlname == v.box_info.box_urlname));
            //var dicList = valueList.ToDictionary(t => t.box_info, t => t.board_number);//, new DynamicEqualityComparer<FileTestEntity>((t, v) => t.box_urlname == v.box_urlname));
            //foreach (var item in numbersAndWords)
            //    Console.WriteLine(item);
            //foreach (var item in tp)
            //    Console.WriteLine($"{item.First}-{item.Second}-{item.Third}");
            //foreach (var item in filterList)
            //    Console.WriteLine($"filter {item}");
            //foreach (var item in targList)
            //    Console.WriteLine($"Union {item.board_number}-{item.box_info.box_urlname}");
            //foreach (var item in kvList)
            //    Console.WriteLine($"Union {item.Key}-{item.Count()}");
            //foreach (var item in kvList)
            //{
            //    Console.WriteLine($"Union {item.Key.box_urlname}-{item.Count()}");
            //    foreach (string str in item)
            //        Console.WriteLine("    {0}", str);
            //}
            //
            //foreach (var item in targList)
            //    Console.WriteLine($"Union {item.board_number}-{item.box_info.box_urlname}");
            //foreach (var item in dicList)
            //    Console.WriteLine($"Union {item.Key.box_urlname}-{item.Value}");
            //valueList = valueList.Order(new TestEntityComparer());
            //valueList = valueList.OrderBy(x => x.board_number);
            //valueList = valueList.OrderBy(x => x.box_info, new FileTestEntityComparer() );
            //valueList = valueList.OrderByDescending(x => x.board_number);
            //valueList = valueList.OrderByDescending(x => x.box_info, new FileTestEntityComparer());
            //valueList = valueList.OrderDescending(new TestEntityComparer());
            //words = words.Order().ToArray();
            //valueList = valueList.Order(new TestEntityComparer()).ThenByDescending(x =>x.box_info, new FileTestEntityComparer());
            //foreach (var item in words)
            //{
            //    Console.WriteLine($"Order {item}");
            //}

            //foreach (var item in valueList)
            //    Console.WriteLine($"Order {item.board_number}-{item.box_info.box_urlname}-{item.bag_count}");
            //Console.WriteLine("-----------------------------------------");
            //valueList = valueList.Take(2);
            //valueList = valueList.Take(Range.StartAt(Index.FromEnd(3)));
            //valueList = valueList.TakeLast(2);
            //valueList = valueList.Skip(2).TakeWhile(t=> !t.box_info.box_urlname.Contains("10"));
            //valueList = valueList.SkipLast(2).TakeWhile(t => !t.box_info.box_urlname.Contains("10"));
            //valueList = valueList.SkipWhile(t => !t.box_info.box_urlname.Contains("10"));
            //var enity = valueList.Single(t => t.board_number == $"no{100}");
            //var enity = valueList.SingleOrDefault(t => t.board_number == $"no{3}");
            //var itemList = valueList.Select(t=> t.board_number).ToList();
            //foreach (var item in itemList)
            //    Console.WriteLine($"Select {item}");
            //var itemList = valueList.SelectMany(t => t.attachments).ToList();
            //foreach (var item in itemList)
            //    Console.WriteLine($"SelectMany {item.box_urlname}-{item.box_url}");
            //var itemList = valueList.SelectMany(t => t.attachments, (t,v)=> new {t.board_number, v.box_urlname }).ToList();
            //foreach (var item in itemList)
            //    Console.WriteLine($"SelectMany {item.box_urlname}-{item.board_number}");
            //valueList = valueList.Reverse();
            //foreach (var item in valueList)
            //    Console.WriteLine($"Take {item.board_number}-{item.box_info.box_urlname}-{item.bag_count}");

            //List<Pet> pets2 = new List<Pet>();
            //Pet defaultPet = new Pet { Name = "Default Pet", Age = 0 };
            //foreach (Pet pet in pets2.DefaultIfEmpty(defaultPet))
            //{
            //    Console.WriteLine("\nName: {0}", pet.Name);
            //}

            //Console.WriteLine("-----------------------------------------");
            //Console.WriteLine($"totalCount {valueList.Sum(t => t.bag_count)}");
        }
    }

    internal class Pet
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public abstract partial class BaseMuitlCreator
    {
        public virtual Food CreateMeatFactory()
        {
            return null;
        }
    }
}