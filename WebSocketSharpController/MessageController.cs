using System.Threading.Tasks;

namespace WebSocketSharpController
{
    public abstract class MessageController<TMessageRequest, TMessageResponse> : IMessageController
        where TMessageRequest : IMessageRequest
        where TMessageResponse : IMessageResponse
    {
        public async Task<IMessageResponse> ProcessRequestAsync(IMessageRequest messageRequest) 
            => await ProcessRequest((TMessageRequest)messageRequest);

        public async Task ProcessResponseAsync(IMessageResponse messageResponse) 
            => await ProcessResponse((TMessageResponse)messageResponse);

        protected abstract Task<TMessageResponse> ProcessRequest(TMessageRequest messageRequest);

        protected abstract Task ProcessResponse(TMessageResponse messageRequest);
    }
}
