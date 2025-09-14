namespace PopForums.Services;

public interface IForumPermissionService
{
	Task<ForumPermissionContext> GetPermissionContext(Forum forum, User user);
	Task<ForumPermissionContext> GetPermissionContext(Forum forum, User user, Topic topic);
}

public class ForumPermissionService : IForumPermissionService
{
	private readonly IForumRepository _forumRepository;

	public ForumPermissionService(IForumRepository forumRepository)
	{
		_forumRepository = forumRepository;
	}

	public async Task<ForumPermissionContext> GetPermissionContext(Forum forum, User user)
	{
		return await GetPermissionContext(forum, user, null);
	}

	public async Task<ForumPermissionContext> GetPermissionContext(Forum forum, User user, Topic topic)
	{
		var context = new ForumPermissionContext { DenialReason = string.Empty };
		var viewRestrictionRoles = await _forumRepository.GetForumViewRoles(forum.ForumID);
		var postRestrictionRoles = await _forumRepository.GetForumPostRoles(forum.ForumID);

		// Если у пользователя нет запрещенных ролей, то он может смотреть посты на форуме
		if (viewRestrictionRoles.Count == 0)
			context.UserCanView = true;
		else
		{
			context.UserCanView = false;
			if (user != null && viewRestrictionRoles.Where(user.IsInRole).Any())
				context.UserCanView = true;
		}

		// Если пользователь неавторизован или пользователю запрещено смотреть(забанен) то просмотр недоступен
		if (user == null || !context.UserCanView)
		{
			context.UserCanPost = false;
			context.DenialReason = Resources.LoginToPost;
		}
		else
		//Если пользователь верифицирован, то он не может постить
		if (!user.IsApproved)
		{
			context.DenialReason += "You can't post until you have verified your account. ";
			context.UserCanPost = false;
		}
		else
		{
			//Если у пользователя есть нет ролей мешающих постить то он может постить
			if (postRestrictionRoles.Count == 0)
				context.UserCanPost = true;
			else
			{
				if (postRestrictionRoles.Where(user.IsInRole).Any())
					context.UserCanPost = true;
				else
				{
					context.DenialReason += Resources.ForumNoPost + ". ";
					context.UserCanPost = false;
				}
			}
		}
		//Если пользователь пытается запостить по теме, но темы не существует или закрыта, то запрет на пост
		if (topic != null && topic.IsClosed)
		{
			context.UserCanPost = false;
			context.DenialReason = Resources.Closed + ". ";
		}
		//Если тема удалена то нельзя постить
		if (topic != null && topic.IsDeleted)
		{
			if (user == null || !user.IsInRole(PermanentRoles.Moderator))
				context.UserCanView = false;
			context.DenialReason += "Topic is deleted. ";
		}

		//Если тема заархирована
		if (forum.IsArchived)
		{
			context.UserCanPost = false;
			context.DenialReason += Resources.Archived + ". ";
		}

		// Модерация 
		context.UserCanModerate = false;
		if (user != null && (user.IsInRole(PermanentRoles.Admin) || user.IsInRole(PermanentRoles.Moderator)))
			context.UserCanModerate = true;

		return context;
	}
}