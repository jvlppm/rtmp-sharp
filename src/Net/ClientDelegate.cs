using System;
using System.Linq;
using System.Reflection;
using Konseki;

namespace RtmpSharp.Net
{
    public interface IClientDelegate
    {
        void Invoke(string method, object[] args);
    }

    public static class ClientDelegate
    {
        public static IClientDelegate UseReflection(object clientDelegate)
        {
            if (clientDelegate == null)
                throw new System.ArgumentNullException(nameof(clientDelegate));
            
            return new ReflectionDelegate(clientDelegate);
        }

        class ReflectionDelegate : IClientDelegate
        {
            readonly object clientDelegate;

            public ReflectionDelegate(object clientDelegate)
            {
                this.clientDelegate = clientDelegate;
            }

            public void Invoke(string method, object[] args)
            {
                var argTypes = args.Select(arg => arg == null ? typeof(object) : arg.GetType()).ToArray();
                try
                {
                    var clientMethod = clientDelegate.GetType().GetMethod(method, argTypes);
                    if (clientMethod == null)
                    {
                        Kon.DebugWarn($"Public method not found at ClientDelegate: {method} ({string.Join(", ", argTypes.Select(t => t.Name))})");
                    }
                    else
                    {
                        clientMethod.Invoke(clientDelegate, args);
                    }
                }
                catch (Exception ex)
                {
                    Kon.Error($"Error invoking dynamic method: {method} ({string.Join(", ", argTypes.Select(t => t.Name))})", ex);
                }
            }
        }
    }
}