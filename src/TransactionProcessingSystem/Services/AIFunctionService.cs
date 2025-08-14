using System;
using Microsoft.Extensions.AI;

namespace TransactionProcessingSystem.Services;

public class AIFunctionService(ICategoriesService categoriesService)
{
    public AIFunction GetSubCategories()
    {
        // See if this picks the name correctly
        var get_sub_categories = AIFunctionFactory.Create(categoriesService.GetSubCategories, "get_sub_categories");

        return get_sub_categories;
    }

    public AIFunction GetMainCategories()
    {
        // See if this picks the name correctly
        var get_main_categories = AIFunctionFactory.Create(categoriesService.GetMainCategories, "get_main_categories");

        return get_main_categories;
    }
}

