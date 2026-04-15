using GameAPIServer.Models.GameDb;

namespace GameAPIServer.Repository.Interfaces;

public interface IItemRepository : IDisposable
{
	public Task<bool> InsertUserItem(UserItem item);
}
