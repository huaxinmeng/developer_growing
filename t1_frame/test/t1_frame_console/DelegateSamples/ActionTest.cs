using BenchmarkDotNet.Jobs;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace t1_frame_console.DelegateSamples
{
    public class ActionTest
    {
        public string GetStr1(params string[] items)
        {
            return string.Join(" ", items);
        }

        public string GetStr(string value1, string value2)
        {
            return $"{value1} {value2}";
        }

        public string GetStr2<T1, T2>(T1 value1, T2 value2)
        {
            return $"{value1} {value2}";
        }

        public string GetStr3<T>(int value, params T[] items)
        {
            //foreach (var item in BlackList)
            //{

            //}
            return string.Join(" ", items);
        }

        //public static IEnumerable<List<string>> BlackList
        //{
        //    get
        //    {
        //        {
        //            yield return new List<string>() { "UnityEngine.RenderTexture", "GetTemporary" };
        //            yield return new List<string>() { "UnityEngine.RenderTexture", "vrUsage" };
        //        }
        //    }
        //}
    }

    public static class Indicator
    {
        public static bool Injected { get; set; }

        public static async IAsyncEnumerable<int> GetDataAsync()
        {
            for (int i = 0; i < 1000; i++)
            {
                await Task.Delay(10); // 模拟异步操作
                yield return i;
            }
        }
    }

    public class Utils
    {
        public List<T> Translate<T>(string content)
        {
            Type type = typeof(T);
            var properties = type.GetProperties();
            var constructor = type.GetConstructor(Type.EmptyTypes);
            //var newobj = (T)constructor.Invoke(null);
            JArray sourceList = JArray.Parse(content);
            var dataList = sourceList.Select(c => Translate<T>(constructor, properties, c)).ToList();
            return dataList;
        }


        public T Translate<T>(ConstructorInfo constructor, PropertyInfo[] properties, JToken jToken)
        {
            var newobj = (T)constructor.Invoke(null);
            foreach (var property in properties)
            {
                SetPropertyValueViaExpression(newobj, property, ConvertType(Type.GetTypeCode(property.PropertyType), jToken[property.Name].ToString()));
            }
            return newobj;
        }

        private static object ConvertType(TypeCode typeCode, string value)
        {
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    return Convert.ToBoolean(value);
                case TypeCode.Byte:
                    return Convert.ToByte(value);
                case TypeCode.DateTime:
                    return Convert.ToDateTime(value);
                case TypeCode.Decimal:
                    return Convert.ToDecimal(value);
                case TypeCode.Double:
                    return Convert.ToDouble(value);
                case TypeCode.Int16:
                    return Convert.ToInt16(value);
                case TypeCode.Int32:
                    return Convert.ToInt32(value);
                case TypeCode.Int64:
                    return Convert.ToInt64(value);
                case TypeCode.SByte:
                    return Convert.ToSByte(value);
                case TypeCode.Single:
                    return Convert.ToSingle(value);
                case TypeCode.String:
                    return value;
                case TypeCode.UInt16:
                    return Convert.ToUInt16(value);
                case TypeCode.UInt32:
                    return Convert.ToUInt32(value);
                case TypeCode.UInt64:
                    return Convert.ToUInt64(value);
                default:
                    return null;
            }
        }

        public void SetPropertyValueViaExpression<T>(T foo, PropertyInfo property, object value)
        {
            //var property = typeof(T).GetProperty(propertyName);
            var target = Expression.Parameter(typeof(T));
            var propertyValue = Expression.Parameter(typeof(object)); // 原始值类型是 object

            // 将 object 转换为属性的实际类型
            var convertedValue = Expression.Convert(propertyValue, property.PropertyType);
            var setPropertyValue = Expression.Call(target, property.GetSetMethod(), convertedValue);
            var setAction = Expression.Lambda<Action<T, object>>(setPropertyValue, target, propertyValue).Compile();
            setAction(foo, value);
        }

        //       public static void SetPropertyValueViaEmit(IFoo foo, Bar bar)
        //{
        //    var property = typeof(IFoo).GetProperty("Bar");
        //    DynamicMethod method = new DynamicMethod("SetValue", null, new Type[] { typeof(IFoo), typeof(Bar) });
        //    ILGenerator ilGenerator = method.GetILGenerator();
        //     ilGenerator.Emit(OpCodes.Ldarg_0);
        //     ilGenerator.Emit(OpCodes.Ldarg_1);
        //     ilGenerator.EmitCall(OpCodes.Callvirt, property.GetSetMethod(), null);
        //     ilGenerator.Emit(OpCodes.Ret);

        //    method.DefineParameter(1, ParameterAttributes.In, "obj");
        //    method.DefineParameter(2, ParameterAttributes.In, "value");
        //   var setAction = (Action<IFoo, Bar>)method.CreateDelegate(typeof(Action<IFoo, Bar>));
        //    setAction(foo, bar);
        //}

        public void Test()
        {
            var index = Expression.Parameter(typeof(int), "i");
            //var init = Expression.Assign(index, Expression.Constant(0));
            LabelTarget label = Expression.Label();
            BlockExpression block = Expression.Block(
                Expression.Call(
                null,
                typeof(Console).GetMethod("WriteLine", new Type[] { typeof(String) }),
                Expression.Call(index, typeof(int).GetMethod("ToString", Type.EmptyTypes))
               ),
                Expression.PostIncrementAssign(index));
            BlockExpression loopBody = Expression.Block(
                //new[] { index },
                //Expression.Assign(index, Expression.Constant(0)),
                Expression.Loop(
                Expression.IfThenElse(
                    Expression.LessThan(index, Expression.Constant(3)),
                    block,
                    Expression.Break(label)),
                label));
            // Wrap the loop inside a loop expression
            //var loopExpression = Expression.Loop(loopBody, label);

            // Create and compile the expression
            Expression.Lambda<Action<int>>(
                loopBody, index).Compile()(2);

        }

        public string ResolveUpdateExpress<T>(Expression<Func<T>> updateExpression)
        {
            var body = updateExpression.Body as MemberInitExpression;
            if (body == null || body.Bindings.IsNullOrEmpty()) return default;
            var sb = new StringBuilder();
            object? value;
            foreach (MemberAssignment g in body.Bindings)
            {
                value = g.Expression is ConstantExpression cexp ? cexp.Value : Expression.Lambda(g.Expression).Compile().DynamicInvoke();
                if (value != null)
                {
                    sb.AppendLine($"{g.Member.Name}={GetValue(value)}");
                }                
            }

            var content = sb.ToString();
            return content;
        }

        private string GetValue(object? value)
        {
            string str = null;
            if(value == null)
            {
                return str;
            }

            if(value is DateTime date)
            {
                str = $"'{date.ToString("yyyy-MM-dd HH:mm:ss")}'";
            }
            else if(value is string content)
            {
                str = $"'{content.Replace("'", "''")}'";
            }
            else if( value is bool bvalue)
            {
                str = $"{Convert.ToSByte(value)}";
            }
            else
            {
                str = $"{value}";
            }

            return str;
        }

        public string ResolveToSql(Expression<Func<TestEntity, bool>> filterExpression)
        {
            var content = Resolve(filterExpression);
            return content;
        }

        private string BinaryExpressionResolve(BinaryExpression bexp)
        {
            if (bexp.Left is UnaryExpression && bexp.Right is UnaryExpression)
            {
                throw new NotSupportedException($"不支持解析 {bexp.ToString()}");
            }

            MethodCallExpression mexp = null;
            object? value = null;
            if (bexp.Left is MethodCallExpression mclexp)
            {
                mexp = mclexp;
                value = NomalExpressionResolve(bexp.Right);
            }
            else if (bexp.Right is MethodCallExpression mcrexp)
            {
                mexp = mcrexp;
                value = NomalExpressionResolve(bexp.Left);
            }

            if (mexp != null)
            {
                return MethodCallExpressionResolve(mexp, value);
            }               

            var filedName = NomalExpressionResolve(bexp.Left);
            if(!(bexp.Left is MemberExpression mlexp && mlexp.Expression is ParameterExpression))
            {
                filedName = GetValue(filedName);
            }
            var filedValue = NomalExpressionResolve(bexp.Right);
            if (!(bexp.Right is MemberExpression mrexp && mrexp.Expression is ParameterExpression))
            {
                filedValue = GetValue(filedValue);
            }
            var opt = GetOperator(bexp.NodeType);
            string Result = string.Format("{0} {1} {2}", filedName, opt, filedValue);
            return Result;
        }

        private object? NomalExpressionResolve(Expression expression)
        {
            if(expression is MemberExpression mexp && mexp.Expression is ParameterExpression)
            {
                return mexp.Member.Name;
            }
            else
            {
                return expression is ConstantExpression cexp ? cexp.Value : Expression.Lambda(expression).Compile().DynamicInvoke();
            }
        }

        private string MethodCallExpressionResolve(MethodCallExpression mcexp, object? targetValue)
        {
            bool isReverse = false;
            if (targetValue is bool val)
            {
                isReverse = val;
            }

            var func = mcexp.Method.Name;

            object? argValue = null;
            if (!mcexp.Arguments.IsNullOrEmpty())
            {
                argValue = NomalExpressionResolve(mcexp.Arguments[0]);
            }

            string fieldName = string.Empty;
            string? value = argValue?.ToString();           

            var opt = string.Empty;
            switch (func)
            {
                case "StartsWith":
                    {
                        if (mcexp.Object is MemberExpression mexp && mexp.Expression is ParameterExpression)
                        {
                            fieldName = mexp.Member.Name;
                        }

                        if (argValue is not string)
                        {
                            return string.Format("{0} unknown {1}", fieldName, value);
                        }

                        value += '%';
                        value = $"'{value?.Replace("'", "''")}'";
                        opt = !isReverse ? "not like" : "like";
                        break;
                    }
                case "Contains":
                    {
                        if (mcexp.Object is MemberExpression mexp && mexp.Expression is ParameterExpression)
                        {
                            fieldName = mexp.Member.Name;
                            if (argValue is not string)
                            {
                                return string.Format("{0} unknown {1}", fieldName, value);
                            }

                            value = $"%{value}%";
                            value = $"'{value?.Replace("'", "''")}'";
                            opt = !isReverse ? "not like" : "like";
                            break;
                        }

                        if (mcexp.Object is MemberExpression mcoexp && mcoexp.Expression is not ParameterExpression)
                        {
                            fieldName = argValue?.ToString() ?? string.Empty;
                            argValue = Expression.Lambda(mcexp.Object).Compile().DynamicInvoke();                            
                        }
                        else if(mcexp.Arguments.Count > 1 && mcexp.Arguments[1] is MemberExpression argexp && argexp.Expression is ParameterExpression)
                        {
                            fieldName = argexp.Member.Name;
                        }

                        if (argValue is IEnumerable<object> aryList)
                        {
                            value = string.Join(",", aryList.Select(t=> GetValue(t)));
                            opt = !isReverse ? "not in" : "in";
                            return $"{fieldName} {opt} ({value})";
                        }
                        else
                        {
                            return string.Format("{0} unknown {1}", fieldName, value);
                        }
                    }
                default:
                    throw new Exception($"不支持{func}方法的查找！");
            }

            return $"{fieldName} {opt} {value}";
        }

        private string UnaryExpressionResolve(UnaryExpression uexp)
        {
            var value = uexp.NodeType == ExpressionType.Not ? false : true;
            if (uexp.Operand is MethodCallExpression mcexp)
            {
                return MethodCallExpressionResolve(mcexp, value);
            }
            else if(uexp.Operand is MemberExpression mexp)
            {
                return MemberExpressionResolve(mexp, value);
            }
            else
            {
                throw new NotSupportedException($"未能解析 {uexp.Operand.ToString()}");
            }
        }

        private string MemberExpressionResolve(MemberExpression mexp, bool isReverse = false)
        {
            var filedName = mexp.Member.Name;
            var opt = GetOperator(isReverse ? ExpressionType.Equal : ExpressionType.NotEqual);
            return string.Format("{0} {1} {2}", filedName, opt, GetValue(isReverse));
        }

        private string Resolve(Expression expression, bool isLeft = false)
        {
            if (expression is LambdaExpression lexp)
            {
                return Resolve(lexp.Body);
            }
            if (expression is BinaryExpression bexp && bexp.Left is not BinaryExpression && bexp.Right is not BinaryExpression)
            {
                //if (bexp.Left is MemberExpression && bexp.Right is ConstantExpression)//解析x=>x.Name=="123" x.Age==123这类
                //    return ResolveFunc(bexp.Left, bexp.Right, bexp.NodeType);
                //if (bexp.Left is MethodCallExpression && bexp.Right is ConstantExpression)//解析x=>x.Name.Contains("xxx")==false这类的
                //{
                //    object value = (bexp.Right as ConstantExpression).Value;
                //    return ResolveLinqToObject(bexp.Left, value, bexp.NodeType);
                //}
                //if (bexp.Left is MemberExpression && bexp.Right is MemberExpression)//解析x=>x.Date==DateTime.Now这种
                //{
                //    LambdaExpression lambda = Expression.Lambda(bexp.Right);
                //    Delegate fn = lambda.Compile();
                //    ConstantExpression value = Expression.Constant(fn.DynamicInvoke(null), bexp.Right.Type);
                //    return ResolveFunc(bexp.Left, value, bexp.NodeType);
                //}

                return BinaryExpressionResolve(bexp);
            }
            if (expression is UnaryExpression uexp)
            {
                //if (unary.Operand is MethodCallExpression)//解析!x=>x.Name.Contains("xxx")或!array.Contains(x.Name)这类
                //    return ResolveLinqToObject(unary.Operand, false);
                //if (unary.Operand is MemberExpression && unary.NodeType == ExpressionType.Not)//解析x=>!x.isDeletion这样的 
                //{
                //    ConstantExpression constant = Expression.Constant(false);
                //    return ResolveFunc(unary.Operand, constant, ExpressionType.Equal);
                //}

                return UnaryExpressionResolve(uexp);
            }
            if (expression is MemberExpression mexp && expression.NodeType == ExpressionType.MemberAccess)//解析x=>x.isDeletion这样的 
            {
                if (isLeft) return mexp.Member.Name;
                //ConstantExpression constant = Expression.Constant(true);
                //return ResolveFunc(member, constant, ExpressionType.Equal);
                return MemberExpressionResolve(mexp, true);
            }
            if (expression is MethodCallExpression mcexp)//x=>x.Name.Contains("xxx")或array.Contains(x.Name)这类
            {
                //return ResolveLinqToObject(methodcall, true);
                return MethodCallExpressionResolve(mcexp, true);
            }

            var body = expression as BinaryExpression;
            if (body == null)
                throw new Exception("无法解析" + expression);
            var opt = GetOperator(body.NodeType);
            var right = Resolve(body.Right);
            var left = Resolve(body.Left, true);           
            return string.Format("{0} {1} {2}", left, opt, right);
        }

        /// <summary>
        /// 根据条件生成对应的sql查询操作符
        /// </summary>
        /// <param name="expressiontype"></param>
        /// <returns></returns>
        private string GetOperator(ExpressionType expressiontype)
        {
            switch (expressiontype)
            {
                case ExpressionType.And:
                    return "and";
                case ExpressionType.AndAlso:
                    return "and";
                case ExpressionType.Or:
                    return "or";
                case ExpressionType.OrElse:
                    return "or";
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.NotEqual:
                    return "<>";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.Subtract:
                    return "-";
                default:
                    throw new Exception(string.Format("不支持{0}此种运算符查找！" + expressiontype));
            }
        }


        private string ResolveFunc(Expression left, Expression right, ExpressionType expressiontype)
        {
            var Name = (left as MemberExpression).Member.Name;
            var Value = (right as ConstantExpression).Value;
            var Operator = GetOperator(expressiontype);
            string CompName = string.Empty;
            //if (Value == null)
            //{
            //    CompName = SetArgument(Name, null);
            //}
            //else
            //{
            //    CompName = SetArgument(Name, Value.ToString());
            //}
            string Result = string.Format("{0} {1} {2}", Name, Operator, GetValue(Value));
            return Result;
        }

        private string ResolveLinqToObject(Expression expression, object value, ExpressionType? expressiontype = null)
        {
            var MethodCall = expression as MethodCallExpression;
            var MethodName = MethodCall.Method.Name;
            switch (MethodName)//这里其实还可以改成反射调用，不用写switch
            {
                case "StartsWith":
                    {
                        var argExp = MethodCall.Arguments[0] as ConstantExpression;
                        object? Temp_Vale = (argExp)?.Value;
                        string Value = Temp_Vale?.ToString();
                        if (Temp_Vale is String content)
                        {
                            content += '%';
                            Value = $"'{content.Replace("'", "''")}'";
                        }
                        //string Value = string.Format("{0}%", GetValue(Temp_Vale));
                        string Name = (MethodCall.Object as MemberExpression).Member.Name;
                        string CompName = string.Empty;//SetArgument(Name, Value);
                        string Result = string.Format("{0} like {1}", Name, Value);
                        return Result;
                    }
                case "Contains":
                    if (MethodCall.Object != null)
                        return Like(MethodCall);
                    return In(MethodCall, value);
                case "Count":
                    return Len(MethodCall, value, expressiontype.Value);
                case "LongCount":
                    return Len(MethodCall, value, expressiontype.Value);
                default:
                    throw new Exception(string.Format("不支持{0}方法的查找！", MethodName));
            }
        }

        //private string SetArgument(string name, string value)
        //{
        //    name = "@" + name;
        //    string temp = name;
        //    while (Argument.ContainsKey(temp))
        //    {
        //        int code = Guid.NewGuid().GetHashCode();
        //        if (code < 0)
        //            code *= -1;
        //        temp = name + code;
        //    }
        //    Argument[temp] = value;
        //    return temp;
        //}

        private string In(MethodCallExpression expression, object isTrue)
        {
            var Argument1 = (expression.Arguments[0] as MemberExpression).Expression as ConstantExpression;
            var Argument2 = expression.Arguments[1] as MemberExpression;
            var Array = new object[0];
            var Field_Array = Argument1.Value.GetType().GetFields().First();
            if (Argument2.Type.Name == "String")
            {

                Array = Field_Array.GetValue(Argument1.Value) as object[];
            }
            else
            {
                Array = (Field_Array.GetValue(Argument1.Value) as int[]).Select(p => (object)p).ToArray();
            }



            List<string> SetInPara = new List<string>();
            for (int i = 0; i < Array.Length; i++)
            {
                string Name_para = "InParameter" + i;
                string Value = Array[i].ToString();
                string Key = string.Empty; //SetArgument(Name_para, Value);
                SetInPara.Add(Key);
            }
            string Name = Argument2.Member.Name;
            string Operator = Convert.ToBoolean(isTrue) ? "in" : " not in";
            string CompName = string.Join(",", SetInPara);
            string Result = string.Format("{0} {1} ({2})", Name, Operator, CompName);
            return Result;
        }

        private string Like(MethodCallExpression expression)
        {
            object Temp_Vale = (expression.Arguments[0] as ConstantExpression).Value;
            string Value = string.Format("%{0}%", Temp_Vale);
            string Name = (expression.Object as MemberExpression).Member.Name;
            string CompName = string.Empty;//SetArgument(Name, Value);
            string Result = string.Format("{0} like {1}", Name, CompName);
            return Result;
        }

        private string Len(MethodCallExpression expression, object value, ExpressionType expressiontype)
        {
            object Name = (expression.Arguments[0] as MemberExpression).Member.Name;
            string Operator = GetOperator(expressiontype);
            string CompName = string.Empty;// SetArgument(Name.ToString(), value.ToString());
            string Result = string.Format("len({0}){1}{2}", Name, Operator, CompName);
            return Result;
        }
    }
}
