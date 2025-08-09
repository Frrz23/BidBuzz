using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BidBuzz.Hubs
{
    public class BidHub : Hub
    {
        public async Task SendBidUpdate(int itemId)
        {
            await Clients.Group($"item-{itemId}").SendAsync("ReceiveBidUpdate", itemId);
        }

        public async Task SendAutoBidUpdate(int itemId)
        {

            await Clients.Group($"item-{itemId}").SendAsync("ReceiveAutoBidUpdate", itemId);
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var itemId = httpContext?.Request.Query["itemId"];
            if (int.TryParse(itemId, out int id))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"item-{id}");
            }
            await base.OnConnectedAsync();
        }
    }
}