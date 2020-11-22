using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace bit23
{
    public abstract class DynamicInterfaceObject
    {
        protected DynamicInterfaceObject(Type interfaceType, IDictionary<string, object> source)
        {
            var thisType = this.GetType();
            foreach (var entry in source)
            {
                var interfacePropertyInfo = interfaceType.GetProperty(entry.Key, BindingFlags.Instance | BindingFlags.Public);
                if (interfacePropertyInfo == null)
                {

                }
                else
                {
                    var thisPropertyInfo = thisType.GetProperty(entry.Key, BindingFlags.Instance | BindingFlags.Public);
                    thisPropertyInfo.GetSetMethod(true).Invoke(this, new[] { entry.Value });
                }
            }
        }
    }
}
