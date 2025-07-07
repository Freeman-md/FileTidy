using FileTidy.Core.Interfaces;
using System.Collections.Generic;

namespace FileTidy.Core.Services;

public class FileCategoryService : IFileCategoryService
{
    public string GetCategory(string extension)
        => throw new System.NotImplementedException();

    public IEnumerable<string> GetAllCategoryNames()
        => throw new System.NotImplementedException();
} 