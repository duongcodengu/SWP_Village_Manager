using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;
using Village_Manager.Models;

public class CategoryMenuViewComponent : ViewComponent
{
    private readonly AppDbContext _context;

    public CategoryMenuViewComponent(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var categories = await _context.ProductCategories.ToListAsync();
        return View(categories);
    }
}
