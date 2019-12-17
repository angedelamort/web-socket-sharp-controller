using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharpController.Activator;
using WebSocketSharpController.Serializer;

namespace WebSocketSharpController
{
    public static class MessageControllerFactory
    {
        private static readonly Dictionary<Type, Type> RequestMap = new Dictionary<Type, Type>();
        private static readonly Dictionary<Type, Type> ResponseMap = new Dictionary<Type, Type>();
        private static readonly Dictionary<uint, ClassHashItem> ClassHashMap = new Dictionary<uint, ClassHashItem>();

        private static IServiceProvider serviceProvider;

        public static void SetServiceProvider(IServiceProvider provider) => serviceProvider = provider;

        static MessageControllerFactory()
        {
            var currentAssembly = Assembly.GetAssembly(typeof(MessageControllerFactory));

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        using (var crc32 = new CRC32())
                        {
                            
                            var bytes = crc32.ComputeHash(Encoding.UTF8.GetBytes(type.FullName));
                            var hash = BitConverter.ToUInt32(bytes, 0);
                            ClassHashMap.Add(hash, new ClassHashItem(type));
                        }

                        if (typeof(IMessageController).IsAssignableFrom(type))
                        {
                            if (assembly == currentAssembly)
                                continue;

                            var (messageRequestType, messageResponseType) = GetMessageTypes(type);
                            RequestMap.Add(messageRequestType, type);
                            ResponseMap.Add(messageResponseType, type);
                        }

                        // TODO: SHOULD ONLY CHECK FOR IMessage*
                        // TODO: SHOULD CHECK FOR A SPECIAL ATTRIBUTE
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        public static ClassHashItem FindClassByHash(uint crc32)
        {
            return ClassHashMap.TryGetValue(crc32, out var item) ? item : null;
        }

        /// <summary>
        /// Process a generic request.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>
        /// Might return a response if the data is a request, but null otherwise.
        /// </returns>
        public static async Task<IMessageResponse> ProcessAsync(object item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            
            if (item is IMessageRequest request)
            {
                if (RequestMap.TryGetValue(request.GetType(), out var t))
                {
                    var instance = CreateInstance(t);
                    return await instance.ProcessRequestAsync(request);
                }
            }
            else if (item is IMessageResponse response)
            {
                if (ResponseMap.TryGetValue(response.GetType(), out var t))
                {
                    var instance = CreateInstance(t);
                    await instance.ProcessResponseAsync(response);
                    return null;
                }
            }

            throw new Exception("Object type is not part opf the factory.");
        }

        private static (Type, Type) GetMessageTypes(Type type)
        {
            while (type.BaseType != null)
            {
                type = type.BaseType;
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(MessageController<,>))
                {
                    var args = type.GetGenericArguments();
                    return (args[0], args[1]);
                }
            }

            return (null, null);
        }

        private static IMessageController CreateInstance(Type type)
        {
            if (serviceProvider != null)
                return (IMessageController) ActivatorUtilities.CreateInstance(serviceProvider, type);

            throw new ApplicationException("Call SetServiceProvider() at the beginning of your application.");
        }

        public static uint GetClassHash(Type objectType)
        {
            using (var hash = new CRC32())
            {
                var result = hash.ComputeHash(Encoding.UTF8.GetBytes(objectType.FullName));
                return BitConverter.ToUInt32(result, 0);
            }
        }

        public static ushort GetPropertyHash(PropertyInfo propertyInfo)
        {
            using (var hash = new CRC16())
            {
                var result = hash.ComputeHash(Encoding.UTF8.GetBytes(propertyInfo.Name));
                return BitConverter.ToUInt16(result, 0);
            }
                
        }
    }

    public class ClassHashItem
    {
        public ClassHashItem(Type classType)
        {
            ClassType = classType;
        }

        private Dictionary<ushort, PropertyInfo> functionMap;

        public Type ClassType { get; }

        public PropertyInfo FindByHash(ushort hash)
        {
            if (functionMap == null)
            {
                functionMap = new Dictionary<ushort, PropertyInfo>();
                foreach (var propertyInfo in ClassType.GetProperties())
                {
                    var h = MessageControllerFactory.GetPropertyHash(propertyInfo);
                    functionMap.Add(h, propertyInfo);
                }
            }

            return functionMap.TryGetValue(hash, out var item) ? item : null;
        }
    }
}
