using System;
using System.Linq;
using System.Reflection;
using Konseki;
using System.Collections.Generic;
using System.Collections;

namespace RtmpSharp.Net
{
    public interface IClientDelegate
    {
        void Invoke(string method, object[] args);
    }

    public static class ClientDelegate
    {
        public static IClientDelegate UseReflection(object clientDelegate, bool ignoreCase = true, string tag = null)
        {
            if (clientDelegate == null)
                throw new System.ArgumentNullException(nameof(clientDelegate));

            return new ReflectionDelegate(clientDelegate, ignoreCase, tag);
        }

        class ReflectionDelegate : IClientDelegate
        {
            readonly object clientDelegate;
            readonly StringComparison comparison;
            readonly string tag;

            public ReflectionDelegate(object clientDelegate, bool ignoreCase, string tag)
            {
                this.clientDelegate = clientDelegate;
                this.comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                this.tag = tag;
            }

            bool CanUseParameter(ParameterInfo parameter, object value, bool specified)
            {
                if (parameter.IsOut)
                    return false;

                if (!specified)
                {
                    return parameter.IsOptional;
                }

                if (value == null)
                {
                    return parameter.ParameterType.IsByRef || Nullable.GetUnderlyingType(parameter.ParameterType) != null;
                }

                return parameter.ParameterType.IsAssignableFrom(value.GetType());
            }

            public void Invoke(string method, object[] args)
            {
                var clientMethod = (
                        from m in clientDelegate.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                        where string.Equals(m.Name, method, comparison)
                        let mP = m.GetParameters()
                        where mP.Length >= args.Length
                        orderby mP.Length
                        where mP.Select((p, i) => CanUseParameter(p, i >= args.Length ? null : args[i], i < args.Length)).All(v => v)
                        select ((MethodInfo method, ParameterInfo[] parameters)?)(m, mP)
                    ).FirstOrDefault();

                if (clientMethod == null)
                {
                    var argTypes = args.Select(arg => arg == null ? typeof(object) : arg.GetType()).ToArray();
                    var tagPrefix = tag == null && clientDelegate == null ? "" : tag == null ? $"[{clientDelegate.GetType().Name}]" : $"[{clientDelegate.GetType().Name}: {tag}] ";
                    Kon.DebugWarn($"{tagPrefix}Method not found at ClientDelegate: {method} ({string.Join(", ", Combine(argTypes, args).Select(t => $"{t.Item1.Name} {t.Item2}"))})");
                    return;
                }

                try
                {
                    var best = clientMethod.Value;
                    var mParams = best.parameters;
                    if (mParams.Length > args.Length)
                    {
                        args = args.Concat(mParams.Skip(args.Length).Select(p => p.DefaultValue)).ToArray();
                    }
                    best.method.Invoke(clientDelegate, args);
                }
                catch (Exception ex)
                {
                    Kon.Error($"Error invoking dynamic method: {clientMethod}", ex);
                }
            }

            IEnumerable<(T1,T2)> Combine<T1, T2>(IEnumerable<T1> t1, IEnumerable<T2> t2)
            {
                var en1 = t1.GetEnumerator();
                var en2 = t2.GetEnumerator();

                while (en1.MoveNext() && en2.MoveNext())
                    yield return (en1.Current, en2.Current);
            }
        }
    }
}