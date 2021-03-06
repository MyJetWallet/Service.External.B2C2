using System.Threading.Tasks;
using MyJetWallet.Domain.ExternalMarketApi;
using MyJetWallet.Domain.ExternalMarketApi.Dto;

namespace Service.External.B2C2.Services
{
    public class OrderBookSourceGrpc : IOrderBookSource
    {
        private readonly OrderBookManager _manager;

        public OrderBookSourceGrpc(OrderBookManager manager)
        {
            _manager = manager;
        }

        public Task<GetNameResult> GetNameAsync(GetOrderBookNameRequest request)
        {
            return Task.FromResult(new GetNameResult {Name = B2C2Const.Name});
        }

        public Task<GetSymbolResponse> GetSymbolsAsync(GetSymbolsRequest request)
        {
            return Task.FromResult(new GetSymbolResponse {Symbols = _manager.GetSymbols()});
        }

        public Task<HasSymbolResponse> HasSymbolAsync(MarketRequest request)
        {
            return Task.FromResult(new HasSymbolResponse {Result = _manager.HasSymbol(request.Market)});
        }

        public Task<GetOrderBookResponse> GetOrderBookAsync(MarketRequest request)
        {
            var result = _manager.GetOrderBook(request.Market);

            return Task.FromResult(new GetOrderBookResponse {OrderBook = result});
        }
    }
}