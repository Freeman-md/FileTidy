namespace FileTidy.Core.Interfaces;

using System.Collections.Generic;

public interface IFileCategoryService
{
    string GetCategory(string extension);
    IEnumerable<string> GetAllCategoryNames();
} 