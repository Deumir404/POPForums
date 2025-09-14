using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PopForums.Services;

public interface IImageService
{
	Task<bool?> IsUserImageApproved(int userImageID);
	[Obsolete("Use GetAvatarImageStream(int) instead.")]
	Task<byte[]> GetAvatarImageData(int userAvatarID);
	Task<IStreamResponse> GetAvatarImageStream(int userAvatarID);
	[Obsolete("Use GetUserImageStream(int) instead.")]
	Task<byte[]> GetUserImageData(int userImageID);
	Task<IStreamResponse> GetUserImageStream(int userImageID);
	Task<DateTime?> GetAvatarImageLastModification(int userAvatarID);
	Task<DateTime?> GetUserImageLastModifcation(int userImageID);
	byte[] ConstrainResize(byte[] bytes, int maxWidth, int maxHeight, int qualityLevel, bool cropInsteadOfConstrain);
	Task<List<UserImage>> GetUnapprovedUserImages();
	Task ApproveUserImage(int userImageID);
	Task DeleteUserImage(int userImageID);
	Task<UserImage> GetUserImage(int userImageID);
	Task<UserImageApprovalContainer> GetUnapprovedUserImageContainer();
}

public class ImageService : IImageService
{
	public ImageService(IUserAvatarRepository userAvatarRepository, IUserImageRepository userImageRepository, IProfileService profileService, IUserRepository userRepository, ISettingsManager settingsManager)
	{
		_userAvatarRepository = userAvatarRepository;
		_userImageRepository = userImageRepository;
		_profileService = profileService;
		_userRepository = userRepository;
		_settingsManager = settingsManager;
	}

	private readonly IUserAvatarRepository _userAvatarRepository;
	private readonly IUserImageRepository _userImageRepository;
	private readonly IProfileService _profileService;
	private readonly IUserRepository _userRepository;
	private readonly ISettingsManager _settingsManager;

	//Изображение подтверждено
	public async Task<bool?> IsUserImageApproved(int userImageID)
	{
		return await _userImageRepository.IsUserImageApproved(userImageID);
	}
	//Получить аву пользователя
	public async Task<UserImage> GetUserImage(int userImageID)
	{
		return await _userImageRepository.Get(userImageID);
	}
	//Одобрить аву пользователя
	public async Task ApproveUserImage(int userImageID)
	{
		await _userImageRepository.ApproveUserImage(userImageID);
	}
	//Удалить аву пользователя
	public async Task DeleteUserImage(int userImageID)
	{
		var userImage = await _userImageRepository.Get(userImageID);
		if (userImage != null)
			await _profileService.SetCurrentImageIDToNull(userImage.UserID);
		await _userImageRepository.DeleteUserImage(userImageID);
	}

	[Obsolete("Use GetAvatarImageStream(int) instead.")]
	public async Task<byte[]> GetAvatarImageData(int userAvatarID)
	{
		return await _userAvatarRepository.GetImageData(userAvatarID);
	}
	//Получить аву потоково
	public async Task<IStreamResponse> GetAvatarImageStream(int userAvatarID)
	{
		return await _userAvatarRepository.GetImageStream(userAvatarID);
	}

	[Obsolete("Use GetUserImageStream(int) instead.")]
	public async Task<byte[]> GetUserImageData(int userImageID)
	{
		return await _userImageRepository.GetImageData(userImageID);
	}
	//ПОлучить фото пользователя
	public async Task<IStreamResponse> GetUserImageStream(int userImageID)
	{
		return await _userImageRepository.GetImageStream(userImageID);
	}
	//Получить список неодобренных изображений
	public async Task<List<UserImage>> GetUnapprovedUserImages()
	{
		return await _userImageRepository.GetUnapprovedUserImages();
	}
	//Получить последние изменения авы
	public async Task<DateTime?> GetAvatarImageLastModification(int userAvatarID)
	{
		return await _userAvatarRepository.GetLastModificationDate(userAvatarID);
	}
	//Получить последние изменения фото
	public async Task<DateTime?> GetUserImageLastModifcation(int userImageID)
	{
		return await _userImageRepository.GetLastModificationDate(userImageID);
	}
	//Изменения размеров
	public byte[] ConstrainResize(byte[] bytes, int maxWidth, int maxHeight, int qualityLevel, bool cropInsteadOfConstrain)
	{
		if (bytes == null)
			throw new Exception("Bytes parameter is null.");
		using (var stream = new MemoryStream(bytes))
		using (var image = Image.Load<Rgba32>(stream))
		using (var output = new MemoryStream())
		{
			if (image.Height <= maxHeight && image.Width <= maxWidth)
				return bytes;
			var options = new ResizeOptions
			{
				Size = new Size(maxWidth, maxHeight),
				Mode = cropInsteadOfConstrain ? ResizeMode.Crop : ResizeMode.Max
			};
			image.Mutate(x => x
				.Resize(options)
				.GaussianSharpen(0.5f));
			image.Save(output, new JpegEncoder { Quality = qualityLevel });
			return output.ToArray();
		}
	}
	//Получить контейнер всех неодобренных фото
	public async Task<UserImageApprovalContainer> GetUnapprovedUserImageContainer()
	{
		var isNewUserImageApproved = _settingsManager.Current.IsNewUserImageApproved;
		var dictionary = new Dictionary<UserImage, User>();
		var unapprovedImages = await GetUnapprovedUserImages();
		var users = await _userRepository.GetUsersFromIDs(unapprovedImages.Select(i => i.UserID).ToList());
		var container = new UserImageApprovalContainer { Unapproved = new List<UserImagePair>(), IsNewUserImageApproved = isNewUserImageApproved };
		foreach (var image in unapprovedImages)
		{
			container.Unapproved.Add(new UserImagePair { User = users.Single(u => u.UserID == image.UserID), UserImage = image });
			dictionary.Add(image, users.Single(u => u.UserID == image.UserID));
		}
		return container;
	}
}