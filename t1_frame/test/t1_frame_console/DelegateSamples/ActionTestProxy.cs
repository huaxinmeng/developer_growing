using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using RabbitMQ.Client;

namespace t1_frame_console.DelegateSamples
{
    public delegate string MyAction(params string[] items);
    public delegate string MyActionExt(ActionTest source, params string[] items);
    public delegate string MyAction6<T>(int value, params T[] items);
    public delegate string MyActionExt6<T>(ActionTest source, int value, params T[] items);
    public class ActionTestProxy
    {
        private ActionTest _actionTest;
        public ActionTestProxy(ActionTest actionTest)
        {
            _actionTest = actionTest;
        }
        public string GetStrByNormal(string value1, string value2)
        {
            //var obj = new ActionTest();
            //return obj.GetStr1("hello", "world!");
            //return obj.GetStr2<string,string>(value1, value2);

            return _actionTest.GetStr3<string>(-1, value1, value2);
        }

        public string GetStrByReflection(string value1, string value2)
        {
            //var obj = new ActionTest();
            //var methods = typeof(ActionTest).GetMethod("GetStr").CreateDelegate(typeof(Func<string, string, string>), obj);
            //if (methods is Func<string, string, string> atc)
            //{
            //    return atc("hello", "world!");
            //}

            //var methods = typeof(ActionTest).GetMethod("GetStr1").CreateDelegate(typeof(MyAction), obj);
            //if (methods is MyAction atc)
            //{
            //    return atc("hello", "world!");
            //}


            //var gMethd = typeof(ActionTest).GetMethod("GetStr2");
            ////var array = gMethd.GetParameters().Select(p => p.ParameterType).ToArray();
            //var methods = gMethd.MakeGenericMethod(value1.GetType(), value2.GetType()).CreateDelegate(typeof(Func<,,>).MakeGenericType(value1.GetType(), value2.GetType(), typeof(string)), obj);

            //return methods.DynamicInvoke(value1, value2).ToString();

            var gMethd = typeof(ActionTest).GetMethod("GetStr3");
            var parameterType = typeof(string);
            var methods = gMethd.MakeGenericMethod(parameterType).CreateDelegate(typeof(MyAction6<>).MakeGenericType(parameterType), _actionTest);

            if (methods is MyAction6<string> atc)
            {
                return atc(-1, value1, value2);
            }

            //return methods.DynamicInvoke([value1, value2]).ToString();
            return default;
        }

        public string GetStrByDelegate(string value1, string value2)
        {
            //var obj = new ActionTest();
            //var methods = Delegate.CreateDelegate(typeof(Func<string, string, string>), obj, "GetStr", true);
            //if (methods is Func<string, string, string> atc)
            //{
            //    return atc("hello", "world!");
            //}

            //var methods = Delegate.CreateDelegate(typeof(MyAction), obj, "GetStr1", true);
            //if (methods is MyAction atc)
            //{
            //    return atc("hello", "world!");
            //}

            //var gMethd = typeof(ActionTest).GetMethod("GetStr2");
            //var methods = Delegate.CreateDelegate(typeof(Func<,,>).MakeGenericType(value1.GetType(), value2.GetType(), typeof(string)), obj, gMethd.MakeGenericMethod(value1.GetType(), value2.GetType()));
            //return methods.DynamicInvoke(value1, value2).ToString();

            var gMethd = typeof(ActionTest).GetMethod("GetStr3");
            var parameterType = typeof(string);
            var methods = Delegate.CreateDelegate(typeof(MyAction6<>).MakeGenericType(parameterType), _actionTest, gMethd.MakeGenericMethod(parameterType));

            if (methods is MyAction6<string> atc)
            {
                return atc(-1, value1, value2);
            }

            return default;
        }

        public string GetStrByEmit(string value1, string value2)
        {
            //var obj = new ActionTest();
            //var dynamicMethod = new DynamicMethod("GetStr", typeof(string), new[] { typeof(ActionTest), typeof(string), typeof(string) });
            //dynamicMethod.DefineParameter(1, ParameterAttributes.None, "source");
            //dynamicMethod.DefineParameter(2, ParameterAttributes.In, "value1");
            //dynamicMethod.DefineParameter(3, ParameterAttributes.In, "value2");
            //var ilGenerator = dynamicMethod.GetILGenerator();
            //ilGenerator.Emit(OpCodes.Ldarg_0);
            //ilGenerator.Emit(OpCodes.Ldarg_1);
            //ilGenerator.Emit(OpCodes.Ldarg_2);
            //ilGenerator.Emit(OpCodes.Callvirt, typeof(ActionTest).GetMethod("GetStr"));
            //ilGenerator.Emit(OpCodes.Ret);

            //var getStrDelegate = dynamicMethod.CreateDelegate(typeof(Func<ActionTest, string, string, string>));
            //if (getStrDelegate is Func<ActionTest, string, string, string> atc1)
            //{
            //    return atc1(obj, "hello", "world!");
            //}

            //var dynamicMethod = new DynamicMethod("GetStr1", typeof(string), new[] { typeof(ActionTest), typeof(string[]) });
            //dynamicMethod.DefineParameter(1, ParameterAttributes.None, "source");
            //dynamicMethod.DefineParameter(2, ParameterAttributes.In, "items");
            //var ilGenerator = dynamicMethod.GetILGenerator();
            //ilGenerator.Emit(OpCodes.Ldarg_0);
            //ilGenerator.Emit(OpCodes.Ldarg_1);
            //ilGenerator.Emit(OpCodes.Callvirt, typeof(ActionTest).GetMethod("GetStr1"));
            //ilGenerator.Emit(OpCodes.Ret);

            //var getStrDelegate = dynamicMethod.CreateDelegate(typeof(MyActionExt));
            //if (getStrDelegate is MyActionExt atc1)
            //{
            //    return atc1(obj, "hello", "world!");
            //}

            //var dynamicMethod = new DynamicMethod("GetStr2", typeof(string), new[] { typeof(ActionTest), value1.GetType(), value2.GetType() });
            //dynamicMethod.DefineParameter(1, ParameterAttributes.None, "source");
            //dynamicMethod.DefineParameter(2, ParameterAttributes.In, "value1");
            //dynamicMethod.DefineParameter(3, ParameterAttributes.In, "value2");
            //var ilGenerator = dynamicMethod.GetILGenerator();
            //ilGenerator.Emit(OpCodes.Ldarg_0);
            //ilGenerator.Emit(OpCodes.Ldarg_1);
            //ilGenerator.Emit(OpCodes.Ldarg_2);
            //ilGenerator.Emit(OpCodes.Callvirt, typeof(ActionTest).GetMethod("GetStr2").MakeGenericMethod(value1.GetType(), value2.GetType()));
            //ilGenerator.Emit(OpCodes.Ret);

            //var getStrDelegate = dynamicMethod.CreateDelegate(typeof(Func<,,,>).MakeGenericType(typeof(ActionTest), value1.GetType(), value2.GetType(), typeof(string)));
            //return getStrDelegate.DynamicInvoke(obj, value1, value2).ToString();
            var parameterType = typeof(string);
            var dynamicMethod = new DynamicMethod("GetStr3", typeof(string), new[] { typeof(ActionTest), typeof(int), typeof(string[]) });
            dynamicMethod.DefineParameter(1, ParameterAttributes.None, "source");
            dynamicMethod.DefineParameter(2, ParameterAttributes.In, "value");
            dynamicMethod.DefineParameter(3, ParameterAttributes.In, "items");
            var ilGenerator = dynamicMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Ldarg_2);
            ilGenerator.Emit(OpCodes.Callvirt, typeof(ActionTest).GetMethod("GetStr3").MakeGenericMethod(parameterType));
            ilGenerator.Emit(OpCodes.Ret);

            var getStrDelegate = dynamicMethod.CreateDelegate(typeof(MyActionExt6<>).MakeGenericType(parameterType));
            if (getStrDelegate is MyActionExt6<string> atc)
            {
                return atc(_actionTest, -1, value1, value2);
            }

            return default;
        }

        public string GetStrByExpression(string value1Str, string value2Str)
        {
            //var obj = new ActionTest();
            //var value1 = Expression.Parameter(typeof(string), "value1");
            //var value2 = Expression.Parameter(typeof(string), "value2");
            //var esObj = Expression.Constant(obj);
            //var methodCall = Expression.Call(esObj, typeof(ActionTest).GetMethod("GetStr"), value1, value2);
            //var expressionMethod = Expression.Lambda<Func<string, string, string>>(methodCall, value1, value2).Compile();

            //if (expressionMethod is Func<string, string, string> atc2)
            //{
            //    return atc2("hello", "world!");
            //}

            //var value1 = Expression.Parameter(typeof(string[]), "items");
            //var esObj = Expression.Constant(obj);
            //var methodCall = Expression.Call(esObj, typeof(ActionTest).GetMethod("GetStr1"), value1);
            //var expressionMethod = Expression.Lambda<MyAction>(methodCall, value1).Compile();

            //if (expressionMethod is MyAction atc2)
            //{
            //    return atc2("hello", "world!");
            //}


            //var value1 = Expression.Parameter(value1Str.GetType(), "value1");
            //var value2 = Expression.Parameter(value2Str.GetType(), "value2");
            //var esObj = Expression.Constant(obj);
            //var methodCall = Expression.Call(esObj, typeof(ActionTest).GetMethod("GetStr2").MakeGenericMethod(value1Str.GetType(), value2Str.GetType()), value1, value2);
            //var expressionMethod = Expression.Lambda(typeof(Func<,,>).MakeGenericType(value1Str.GetType(), value2Str.GetType(), typeof(string)),methodCall, value1, value2).Compile();
            //return expressionMethod.DynamicInvoke(value1Str, value2Str).ToString();

            var parameterType = typeof(string);
            var value = Expression.Parameter(typeof(int), "value");
            var value1 = Expression.Parameter(typeof(string[]), "items");
            var esObj = Expression.Constant(_actionTest);
            var methodCall = Expression.Call(esObj, typeof(ActionTest).GetMethod("GetStr3").MakeGenericMethod(parameterType), value, value1);
            var expressionMethod = Expression.Lambda(typeof(MyAction6<>).MakeGenericType(parameterType), methodCall, value, value1).Compile();

            if (expressionMethod is MyAction6<string> atc2)
            {
                return atc2(-1, "hello", "world!");
            }

            return default;
        }
    }
}
