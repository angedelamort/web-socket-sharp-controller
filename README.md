# WebSocketSharpController
Add-on for WebSocketSharp. Use Web Sockets like .NetCore Controllers.

## Requirements
* Visual Studio (Code or any other)
* .Net Standard ^2

## Usage
TODO

### Controllers & Handlers
- explain the naming convention (Controller, Handler)
  - Microsoft.AspNetCore.Mvc.Route
- explain on how to use each of the type and what is included.

#### WebSocketController vs MessageController
- explain the main roles of each.
- It's like glbal vs local.


## TODO
- [ ] WebSocketManagerMiddlewareExtensions.AddWebSocket
  - [ ] Have a way to override the services.AddTransient\<WebSocketDictionary>(); Probably by having a default parameter to null.
  - [ ] It's possible to have multiple web socket per client. Depending on the endpoint
- [ ] Make it possible to switch between `string` and `binary`.
- [ ] Add the `ILogger` properly (hackish right now).
- [ ] Make appropriate uinit tests.