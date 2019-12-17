using System;

namespace WebSocketSharpController.Activator
{
    internal delegate object ObjectFactory(IServiceProvider serviceProvider, object[] arguments);
}
