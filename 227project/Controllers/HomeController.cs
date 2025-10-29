using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using _227project.Models;
using _227project.Data;

namespace _227project.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _logger = logger;
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity!.IsAuthenticated)
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user!);
            
            if (roles.Contains("Admin"))
            {
                return RedirectToAction("Index", "Dashboard");
            }
            else if (roles.Contains("Instructor"))
            {
                return RedirectToAction("Index", "Dashboard");
            }
            else if (roles.Contains("Student"))
            {
                return RedirectToAction("Index", "Dashboard");
            }
        }
        
        return View();
    }

    public async Task<IActionResult> Courses()
    {
        var courses = await _context.Courses
            .Include(c => c.Instructor)
            .Where(c => c.IsActive)
            .ToListAsync();
        
        return View(courses);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
