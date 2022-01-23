using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace common
{
    public class ReflectionHelper
    {
        public static IEnumerable<Type> GetInterfaceSchieves(Type type)
        {
            return GetInterfaceSchieves(AppDomain.CurrentDomain.GetAssemblies(), type);
        }
        public static IEnumerable<Type> GetInterfaceSchieves(Assembly[] assemblys, Type type)
        {
            return assemblys.SelectMany(c => c.GetTypes())
               .Where(c => !c.IsAbstract).Where(c => c.GetInterfaces().Contains(type));
        }
    }
}
