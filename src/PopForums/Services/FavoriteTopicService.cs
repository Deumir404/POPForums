namespace PopForums.Services;

public interface IFavoriteTopicService
{
	Task<Tuple<List<Topic>, PagerContext>> GetTopics(User user, int pageIndex);
	Task<bool> IsTopicFavorite(int userID, int topicID);
	Task AddFavoriteTopic(User user, Topic topic);
	Task RemoveFavoriteTopic(User user, Topic topic);
}

public class FavoriteTopicService : IFavoriteTopicService
{
	public FavoriteTopicService(ISettingsManager settingsManager, IFavoriteTopicsRepository favoriteTopicRepository)
	{
		_settingsManager = settingsManager;
		_favoriteTopicRepository = favoriteTopicRepository;
	}

	private readonly ISettingsManager _settingsManager;
	private readonly IFavoriteTopicsRepository _favoriteTopicRepository;

	//Получить список любимых категорий пользователя по страницам
	public async Task<Tuple<List<Topic>, PagerContext>> GetTopics(User user, int pageIndex)
	{
		var pageSize = _settingsManager.Current.TopicsPerPage;
		var startRow = ((pageIndex - 1) * pageSize) + 1;
		var topics = await _favoriteTopicRepository.GetFavoriteTopics(user.UserID, startRow, pageSize);
		var topicCount = await _favoriteTopicRepository.GetFavoriteTopicCount(user.UserID);
		var totalPages = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(topicCount) / Convert.ToDouble(pageSize)));
		var pagerContext = new PagerContext { PageCount = totalPages, PageIndex = pageIndex, PageSize = pageSize };
		return Tuple.Create(topics, pagerContext);
	}

	//Проверить является тема избранной у пользователя
	public async Task<bool> IsTopicFavorite(int userID, int topicID)
	{
		return await _favoriteTopicRepository.IsTopicFavorite(userID, topicID);
	}

	//Добавить тему в избранное
	public async Task AddFavoriteTopic(User user, Topic topic)
	{
		await _favoriteTopicRepository.AddFavoriteTopic(user.UserID, topic.TopicID);
	}

	//Удалить из избранного
	public async Task RemoveFavoriteTopic(User user, Topic topic)
	{
		await _favoriteTopicRepository.RemoveFavoriteTopic(user.UserID, topic.TopicID);
	}
}