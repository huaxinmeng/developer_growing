using System.Linq.Expressions;

namespace t1_frame_console
{
    public class ExpressionTest
    {
        public void Test()
        {
            //Expression<Func<int, bool>> lambda = num => num < 5;

            //Func<int, bool> lambda1 = num => num < 5;
            //var mark1 = lambda1(4);

            //Expression<Func<int, int>> addFive = (num) => num + 5;

            //if (addFive is LambdaExpression lambdaExp)
            //{
            //    var parameter = lambdaExp.Parameters[0]; //--first

            //    Console.WriteLine(parameter.Name);
            //    Console.WriteLine(parameter.Type);
            //}

            //ExpressTest<TestEntity>(t => new TestEntity
            //{
            //    bag_count = 1,
            //    board_number = "456"
            //});

            //ExpressTest<TestEntity>(t => t.bag_count == 1 && t.board_number == "123" && t.box_url == "1254");

            string name = "Alice";
            int age = 30;

            FormattableString message = $"Hello, {name}! You are {age} years old.";

            string formattedMessage = message.ToString();

            Console.WriteLine(formattedMessage);
        }

        public void ExpressTest<T>(Expression<Func<T, T>> updateExpression)// where T : class
        {
            var memberInitExpression = updateExpression.Body as MemberInitExpression;
            foreach (MemberBinding binding in memberInitExpression.Bindings)
            {
                string propertyName = binding.Member.Name;
                var memberAssignment = binding as MemberAssignment;
            }
        }

        public void ExpressTest<T>(Expression<Func<T, bool>> updateExpression)// where T : class
        {
            var memberInitExpression = updateExpression.Body as MemberInitExpression;
            foreach (MemberBinding binding in memberInitExpression.Bindings)
            {
                string propertyName = binding.Member.Name;
                var memberAssignment = binding as MemberAssignment;
            }
        }
    }
}