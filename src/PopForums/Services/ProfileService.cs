namespace PopForums.Services;

public interface IProfileService
{
	Task<Profile> GetProfile(User user);
	Task Create(Profile profile);
	Task Update(Profile profile);
	Task<Profile> GetProfileForEdit(User user, bool forcePlainText = false);
	Task EditUserProfile(User user, UserEditProfile userEditProfile);
	Task<Dictionary<int, string>> GetSignatures(List<Post> posts);
	Task<Dictionary<int, int>> GetAvatars(List<Post> posts);
	Task SetCurrentImageIDToNull(int userID);
	string GetUnsubscribeHash(User user);
	Task<bool> Unsubscribe(User user, string hash);
	Task UpdatePointTotal(User user);
}

public class ProfileService : IProfileService
{
	private readonly IProfileRepository _profileRepository;
	private readonly ITextParsingService _textParsingService;
	private readonly IPointLedgerRepository _pointLedgerRepository;

	public ProfileService(IProfileRepository profileRepository, ITextParsingService textParsingService, IPointLedgerRepository pointLedgerRepository)
	{
		_profileRepository = profileRepository;
		_textParsingService = textParsingService;
		_pointLedgerRepository = pointLedgerRepository;
	}
	//Получить профиль
	public async Task<Profile> GetProfile(User user)
	{
		if (user == null)
			return null;
		return await _profileRepository.GetProfile(user.UserID);
	}
	//Получить профиль для изменений
	public async Task<Profile> GetProfileForEdit(User user, bool forcePlainText = false)
	{
		var profile = await _profileRepository.GetProfile(user.UserID);
		var userEditProfile = new Profile();
		if (string.IsNullOrWhiteSpace(profile.Signature))
			userEditProfile.Signature = string.Empty;
		else
		{
			if (profile.IsPlainText || forcePlainText) 
				userEditProfile.Signature = _textParsingService.HtmlToForumCode(profile.Signature);
			else
				userEditProfile.Signature = _textParsingService.HtmlToClientHtml(profile.Signature);
		}
		userEditProfile.IsSubscribed = profile.IsSubscribed;
		userEditProfile.ShowDetails = profile.ShowDetails;
		userEditProfile.IsPlainText = profile.IsPlainText;
		userEditProfile.HideVanity = profile.HideVanity;
		userEditProfile.Location = profile.Location;
		userEditProfile.Dob = profile.Dob;
		userEditProfile.Web = profile.Web;
		userEditProfile.Instagram = profile.Instagram;
		userEditProfile.Facebook = profile.Facebook;
		userEditProfile.IsAutoFollowOnReply = profile.IsAutoFollowOnReply;
		return userEditProfile;
	}
	//Редактирование профиля
	public async Task EditUserProfile(User user, UserEditProfile userEditProfile)
	{
		var profile = await _profileRepository.GetProfile(user.UserID);
		if (profile == null)
			throw new Exception($"No profile found for UserID {user.UserID}");
		if (profile.IsPlainText)
			profile.Signature = _textParsingService.ForumCodeToHtml(userEditProfile.Signature);
		else
			profile.Signature = _textParsingService.ClientHtmlToHtml(userEditProfile.Signature);
		profile.IsSubscribed = userEditProfile.IsSubscribed;
		profile.ShowDetails = userEditProfile.ShowDetails;
		profile.IsPlainText = userEditProfile.IsPlainText;
		profile.HideVanity = userEditProfile.HideVanity;
		profile.Location = userEditProfile.Location;
		profile.Dob = userEditProfile.Dob;
		profile.Web = userEditProfile.Web;
		profile.Instagram = userEditProfile.Instagram;
		profile.Facebook = userEditProfile.Facebook;
		profile.IsAutoFollowOnReply = userEditProfile.IsAutoFollowOnReply;
		await _profileRepository.Update(profile);
	}
	//Создать профиль
	public async Task Create(Profile profile)
	{
		if (profile.UserID == 0)
			throw new Exception("Can't create a profile not associated with a valid UserID");
		await _profileRepository.Create(profile);
	}
	//Обновить профиль
	public async Task Update(Profile profile)
	{
		profile.Signature = profile.Signature.Trim();
		if (await _profileRepository.Update(profile) == false)
			throw new Exception($"Profile with UserID {profile.UserID} does not exist.");
	}
	//Получить подпись(?)
	public async Task<Dictionary<int, string>> GetSignatures(List<Post> posts)
	{
		var userIDs = posts.Where(p => p.ShowSig).Select(p => p.UserID).Distinct().ToList();
		return await _profileRepository.GetSignatures(userIDs);
	}
	//Получить авы
	public async Task<Dictionary<int, int>> GetAvatars(List<Post> posts)
	{
		var userIDs = posts.Select(p => p.UserID).Distinct().ToList();
		return await _profileRepository.GetAvatars(userIDs);
	}
	//Получить текущее изображения
	public async Task SetCurrentImageIDToNull(int userID)
	{
		await _profileRepository.SetCurrentImageIDToNull(userID);
	}
	//Получить хэш отписки
	public string GetUnsubscribeHash(User user)
	{
		var source = user.Name + user.Email;
		return source.GetSHA256Hash().Replace("+", string.Empty).Replace("=", string.Empty);
	}
	//Отписка
	public async Task<bool> Unsubscribe(User user, string hash)
	{
		var calculatedHash = GetUnsubscribeHash(user);
		if (calculatedHash != hash)
			return false;
		var profile = await GetProfile(user);
		profile.IsSubscribed = false;
		await Update(profile);
		return true;
	}
	
	public async Task UpdatePointTotal(User user)
	{
		var total = await _pointLedgerRepository.GetPointTotal(user.UserID);
		await _profileRepository.UpdatePoints(user.UserID, total);
	}
}