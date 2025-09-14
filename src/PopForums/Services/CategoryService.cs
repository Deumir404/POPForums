namespace PopForums.Services;

public interface ICategoryService
{
	Task<Category> Get(int categoryID);
	Task<List<Category>> GetAll();
	Task<Category> Create(string title);
	Task Delete(int categoryID);
	Task Delete(Category category);
	Task UpdateTitle(int categoryID, string newTitle);
	Task UpdateTitle(Category category, string newTitle);
	Task MoveCategoryUp(int categoryID);
	Task MoveCategoryDown(int categoryID);
	Task MoveCategoryUp(Category category);
	Task MoveCategoryDown(Category category);
}

public class CategoryService : ICategoryService
{
	public CategoryService(ICategoryRepository categoryRepository, IForumRepository forumRepository)
	{
		_categoryRepository = categoryRepository;
		_forumRepository = forumRepository;
	}

	private readonly ICategoryRepository _categoryRepository;
	private readonly IForumRepository _forumRepository;

	//Получить категорию по ИД
	public async Task<Category> Get(int categoryID)
	{
		return await _categoryRepository.Get(categoryID);
	}

	//Получить все категории
	public async Task<List<Category>> GetAll()
	{
		return await _categoryRepository.GetAll();
	}

	//Создать категорию
	public async Task<Category> Create(string title)
	{
		var newCategory = await _categoryRepository.Create(title, -2);
		await ChangeCategorySortOrder(null, 0);
		newCategory.SortOrder = 0;
		return newCategory;
	}
	
	//Удалить категорию по ИД
	public async Task Delete(int categoryID)
	{
		var category = await _categoryRepository.Get(categoryID);
		if (category == null)
			throw new Exception($"Category with ID {categoryID} does not exist.");
		await Delete(category);
	}

	//Удалить категорию по объекту
	public async Task Delete(Category category)
	{
		var forumResult = await _forumRepository.GetAll();
		var forums = forumResult.Where(f => f.CategoryID == category.CategoryID);
		foreach (var forum in forums)
			await _forumRepository.UpdateCategoryAssociation(forum.ForumID, null);
		await _categoryRepository.Delete(category.CategoryID);
	}

	//Перемименновать категорию по ИД
	public async Task UpdateTitle(int categoryID, string newTitle)
	{
		var category = await _categoryRepository.Get(categoryID);
		if (category == null)
			throw new Exception($"Category with ID {categoryID} does not exist.");
		await UpdateTitle(category, newTitle);
	}

	//Переименновать категорию по объекту
	public async Task UpdateTitle(Category category, string newTitle)
	{
		category.Title = newTitle;
		await _categoryRepository.Update(category);
	}

	//Отсортировать категории
	private async Task ChangeCategorySortOrder(Category category, int change)
	{
		var categories = await GetAll();
		if (category != null)
			categories.Single(c => c.CategoryID == category.CategoryID).SortOrder += change;
		var sorted = categories.OrderBy(c => c.SortOrder).ToList();
		for (var i = 0; i < sorted.Count; i++)
		{
			sorted[i].SortOrder = i * 2;
			await _categoryRepository.Update(sorted[i]);
		}
	}

	//Поднять категорию в списке
	public async Task MoveCategoryUp(int categoryID)
	{
		var category = await _categoryRepository.Get(categoryID);
		if (category == null)
			throw new Exception($"Can't move CategoryID {categoryID} up because it does not exist.");
		await MoveCategoryUp(category);
	}

	//Опустить категорию
	public async Task MoveCategoryDown(int categoryID)
	{
		var category = await _categoryRepository.Get(categoryID);
		if (category == null)
			throw new Exception($"Can't move CategoryID {categoryID} down because it does not exist.");
		await MoveCategoryDown(category);
	}
	
	//Увеличить значение для порядка сортировки
	public async Task MoveCategoryUp(Category category)
	{
		const int change = -3;
		await ChangeCategorySortOrder(category, change);
	}

	//Уменьшить значение для порядка сортировки
	public async Task MoveCategoryDown(Category category)
	{
		const int change = 3;
		await ChangeCategorySortOrder(category, change);
	}
}