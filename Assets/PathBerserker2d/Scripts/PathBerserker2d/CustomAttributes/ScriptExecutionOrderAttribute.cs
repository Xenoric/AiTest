using System;

namespace PathBerserker2d
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class ScriptExecutionOrderAttribute : Attribute
    {
        private int order = 0;

        public ScriptExecutionOrderAttribute(int order)
        {
            this.order = order;
        }

        public int GetOrder()
        {
            return order;
        }
    }
}
