namespace FileTidy.Core.Interfaces;

public interface IFileCategoryMapper
{
    string GetCategory(string extension);
    IEnumerable<string> GetAllCategoryNames();
}