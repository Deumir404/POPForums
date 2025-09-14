﻿using PopForums.Models;

namespace PopForums.Services;

public interface ISubscribedTopicsService
{
	Task AddSubscribedTopic(int userID, int topicID);
	Task RemoveSubscribedTopic(User user, Topic topic);
	Task TryRemoveSubscribedTopic(User user, Topic topic);
	Task NotifySubscribers(Topic topic, User postingUser, string tenantID);
	Task<Tuple<List<Topic>, PagerContext>> GetTopics(User user, int pageIndex);
	Task<bool> IsTopicSubscribed(int userID, int topicID);
	Task<List<int>> GetSubscribedUserIDs(int topicID);
}

public class SubscribedTopicsService : ISubscribedTopicsService
{
	public SubscribedTopicsService(ISubscribedTopicsRepository subscribedTopicsRepository, ISettingsManager settingsManager, INotificationAdapter notificationAdapter, ISubscribeNotificationRepository subscribeNotificationRepository)
	{
		_subscribedTopicsRepository = subscribedTopicsRepository;
		_settingsManager = settingsManager;
		_notificationAdapter = notificationAdapter;
		_subscribeNotificationRepository = subscribeNotificationRepository;
	}

	private readonly ISubscribedTopicsRepository _subscribedTopicsRepository;
	private readonly ISettingsManager _settingsManager;
	private readonly INotificationAdapter _notificationAdapter;
	private readonly ISubscribeNotificationRepository _subscribeNotificationRepository;
	//Добавить подписку на тему
	public async Task AddSubscribedTopic(int userID, int topicID)
	{
		var isSubscribed = await _subscribedTopicsRepository.IsTopicSubscribed(userID, topicID);
		if (!isSubscribed)
			await _subscribedTopicsRepository.AddSubscribedTopic(userID, topicID);
	}
	//Удалить подписку на тему
	public async Task RemoveSubscribedTopic(User user, Topic topic)
	{
		await _subscribedTopicsRepository.RemoveSubscribedTopic(user.UserID, topic.TopicID);
	}
	//Попытка удалить подписку на тему
	public async Task TryRemoveSubscribedTopic(User user, Topic topic)
	{
		if (user != null && topic != null)
			await RemoveSubscribedTopic(user, topic);
	}
	//Уведомление подписчиков
	public async Task NotifySubscribers(Topic topic, User postingUser, string tenantID)
	{
		var payload = new SubscribeNotificationPayload
		{
			TopicID = topic.TopicID,
			TopicTitle = topic.Title,
			PostingUserID = postingUser.UserID,
			PostingUserName = postingUser.Name,
			TenantID = tenantID
		};
		await _subscribeNotificationRepository.Enqueue(payload);
	}
	//Получить темы подписанные
	public async Task<Tuple<List<Topic>, PagerContext>> GetTopics(User user, int pageIndex)
	{
		var pageSize = _settingsManager.Current.TopicsPerPage;
		var startRow = ((pageIndex - 1) * pageSize) + 1;
		var topics = await _subscribedTopicsRepository.GetSubscribedTopics(user.UserID, startRow, pageSize);
		var topicCount = await _subscribedTopicsRepository.GetSubscribedTopicCount(user.UserID);
		var totalPages = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(topicCount) / Convert.ToDouble(pageSize)));
		var pagerContext = new PagerContext { PageCount = totalPages, PageIndex = pageIndex, PageSize = pageSize };
		return Tuple.Create(topics, pagerContext);
	}
	//Пользователь подписан на тему
	public async Task<bool> IsTopicSubscribed(int userID, int topicID)
	{
		return await _subscribedTopicsRepository.IsTopicSubscribed(userID, topicID);
	}
	//Получить количество подписчктов на тему
	public async Task<List<int>> GetSubscribedUserIDs(int topicID)
	{
		return await _subscribedTopicsRepository.GetSubscribedUserIDs(topicID);
	}
}