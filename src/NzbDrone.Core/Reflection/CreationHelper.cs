using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NzbDrone.Core.Reflection;

public static class CreationHelper
{
    public static T Create<T>(IServiceProvider provider, params object[] preferredArgs)
    {
        var type = typeof(T);
        var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var constructor = constructors.First();
        var parameterInfos = constructor.GetParameters();
        var parameters = new List<object>();

        foreach (var parameterInfo in parameterInfos)
        {
            var parameterType = parameterInfo.ParameterType;
            var parameter = preferredArgs.FirstOrDefault(a => parameterType.IsInstanceOfType(a));

            if (parameter == null)
            {
                parameter = provider.GetService(parameterType);
            }

            parameters.Add(parameter);
        }

        return (T)constructor.Invoke(BindingFlags.DoNotWrapExceptions,
            binder: null,
            parameters: parameters.ToArray(),
            culture: null);
    }
}
