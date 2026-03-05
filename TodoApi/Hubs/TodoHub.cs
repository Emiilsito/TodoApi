using Microsoft.AspNetCore.SignalR;

namespace TodoApi.Hubs
{
    public class TodoHub : Hub
    {
        public async Task ModifyChanges()
        {
            await Clients.All.SendAsync("ReceiveRefresh4");
        }
    }
}
