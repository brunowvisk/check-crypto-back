using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace check_crypto.Hubs
{
    [Authorize]
    public class CryptoHub : Hub
    {
        public async Task JoinSymbolGroup(string symbol)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"crypto_{symbol.ToUpper()}");
        }

        public async Task LeaveSymbolGroup(string symbol)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"crypto_{symbol.ToUpper()}");
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}