using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame_console
{
    public class Singleton
    {
        // Static field initializer calls instance constructor.
        private static readonly Singleton _instance = new Singleton();

        private const int _count = 100;

        private Singleton()
        {
            Console.WriteLine("Executes before static constructor.");
            Console.WriteLine($"Singleton Code : {this.GetHashCode()}");
        }

        //public Singleton(int index)
        //{

        //}

        static Singleton()
        {
            // _instance = new Singleton();
            Console.WriteLine("Executes after instance constructor.");
        }

        public static Singleton Instance => _instance;


        // [ModuleInitializer]
        internal static void TestModule()
        {
            Console.WriteLine("try to init TestModule."); 
        }


        /////// <summary>
        /////// 定义为static，可以保证变量为线程安全的，即每个线程一个实例
        /////// </summary>
        //private static Singleton _instance;

        ////public static Singleton Instance => _instance ?? (_instance = new Singleton());

        //private static readonly object Locker = new object();

        //public static Singleton Instance()
        //{
        //    //没有第一重 instance == null 的话，每一次有线程进入 GetInstance()时，均会执行锁定操作来实现线程同步，
        //    //非常耗费性能 增加第一重instance ==null 成立时的情况下执行一次锁定以实现线程同步
        //    if (_instance == null)
        //    {
        //        lock (Locker)
        //        {
        //            //Double-Check Locking 双重检查锁定
        //            if (_instance == null)
        //            {
        //                _instance = new Singleton();
        //            }
        //        }
        //    }

        //    return _instance;
        //}
    }

    /// <summary>
    /// 自己做饭的情况
    /// 没有简单工厂之前，客户想吃什么菜只能自己炒的
    /// </summary>
    public class FoodSimpleFactory
    {
        /// <summary>
        /// 烧菜方法
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Food Cook(string type)
        {
            Food food = null;
            // 客户A说：我想吃西红柿炒蛋怎么办？
            // 客户B说：那你就自己烧啊
            // 客户A说： 好吧，那就自己做吧
            if (type.Equals("西红柿炒蛋"))
            {
                food = new TomatoScrambledEggs();
            }
            // 我又想吃土豆肉丝, 这个还是得自己做
            // 我觉得自己做好累哦，如果能有人帮我做就好了？
            else if (type.Equals("土豆肉丝"))
            {
                food = new ShreddedPorkWithPotatoes();
            }
            return food;
        }

        //static void Main(string[] args)
        //{
        //    // 做西红柿炒蛋
        //    Food food1 = Cook("西红柿炒蛋");
        //    food1.Print();

        //    Food food2 = Cook("土豆肉丝");
        //    food2.Print();

        //    Console.Read();
        //}
    }

    public abstract class FoodFactory
    {
        public abstract Food CreateFoddFactory(ICreator factory);
    }

    public class BaseFoodFactory : FoodFactory
    {
        public override Food CreateFoddFactory(ICreator factory)
        {
            return factory.CreateFoddFactory();
        }
    }

    public abstract class EatService()
    {
        public abstract void CreateEatService(BaseMuitlCreator creator);
    }

    public class BaseEatService : EatService
    {
        public override void CreateEatService(BaseMuitlCreator creator)
        {
            creator.CreateShreddedPorkWithPotatoesFactory();
        }
    }

    public interface ICreator
    {
        Food CreateFoddFactory();
    }

    public interface IMuitlCreator
    {
        Food CreateTomatoScrambledEggsFactory();
        Food CreateShreddedPorkWithPotatoesFactory();
        Food MincedMeatEggplantFactory();
    }

    public abstract partial class BaseMuitlCreator
    {
        public abstract Food CreateTomatoScrambledEggsFactory();
        public abstract Food CreateShreddedPorkWithPotatoesFactory();
        public abstract Food CreateMincedMeatEggplantFactory();
    }

    public class NomalFoodFactory : BaseMuitlCreator
    {
        public override Food CreateShreddedPorkWithPotatoesFactory()
        {
            return new ShreddedPorkWithPotatoes();
            
        }

        public override Food CreateTomatoScrambledEggsFactory()
        {
            return new TomatoScrambledEggs();
        }

        public override Food CreateMincedMeatEggplantFactory()
        {
            return new MincedMeatEggplant();
        }
    }

    public class NanJingFoodFactory : BaseMuitlCreator
    {
        public override Food CreateShreddedPorkWithPotatoesFactory()
        {
            return new NanJingShreddedPorkWithPotatoes();

        }

        public override Food CreateTomatoScrambledEggsFactory()
        {
            return new NanJingTomatoScrambledEggs();
        }

        public override Food CreateMincedMeatEggplantFactory()
        {
            return new NanJingMincedMeatEggplant();
        }

        public override Food CreateMeatFactory()
        {
            return base.CreateMeatFactory();
        }
    }

    public class TomatoScrambledEggsFactory : ICreator
    {
        public Food CreateFoddFactory()
        {
            return new TomatoScrambledEggs();
        }
    }

    public class ShreddedPorkWithPotatoesFactory : ICreator
    {
        public Food CreateFoddFactory()
        {
            return new ShreddedPorkWithPotatoes();
        }
    }

    /// <summary>
    /// 肉末茄子工厂类，负责创建肉末茄子这道菜
    /// </summary>
    public class MincedMeatEggplantFactory : ICreator
    {
        /// <summary>
        /// 负责创建肉末茄子这道菜
        /// </summary>
        /// <returns></returns>
        public Food CreateFoddFactory()
        {
            return new MincedMeatEggplant();
        }
    }

    /// <summary>
    /// 菜抽象类
    /// </summary>
    public abstract class Food
    {
        // 输出点了什么菜
        public abstract void Print();
    }

    /// <summary>
    /// 西红柿炒鸡蛋这道菜
    /// </summary>
    public class TomatoScrambledEggs : Food
    {
        public override void Print()
        {
            Console.WriteLine("一份西红柿炒蛋！");
        }
    }

    /// <summary>
    /// 土豆肉丝这道菜
    /// </summary>
    public class ShreddedPorkWithPotatoes : Food
    {
        public override void Print()
        {
            Console.WriteLine("一份土豆肉丝");
        }
    }

    /// <summary>
    /// 肉末茄子这道菜
    /// </summary>
    public class MincedMeatEggplant : Food
    {
        /// <summary>
        /// 重写抽象类中的方法
        /// </summary>
        public override void Print()
        {
            Console.WriteLine("一份肉末茄子好了");
        }
    }

    /// <summary>
    /// 西红柿炒鸡蛋这道菜
    /// </summary>
    public class NanJingTomatoScrambledEggs : Food
    {
        public override void Print()
        {
            Console.WriteLine("一份NanJing西红柿炒蛋！");
        }
    }

    /// <summary>
    /// 土豆肉丝这道菜
    /// </summary>
    public class NanJingShreddedPorkWithPotatoes : Food
    {
        public override void Print()
        {
            Console.WriteLine("一份NanJing土豆肉丝");
        }
    }

    /// <summary>
    /// 肉末茄子这道菜
    /// </summary>
    public class NanJingMincedMeatEggplant : Food
    {
        /// <summary>
        /// 重写抽象类中的方法
        /// </summary>
        public override void Print()
        {
            Console.WriteLine("一份NanJing肉末茄子好了");
        }
    }

}
