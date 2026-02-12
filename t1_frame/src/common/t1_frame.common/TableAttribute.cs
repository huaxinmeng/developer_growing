namespace t1_frame.common
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        public TableAttribute(string name)
        {
            TableName = name;
        }

        public string TableName { get; private set; }
    }
}