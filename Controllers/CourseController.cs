// ... existing code ...

public async Task<IActionResult> ViewProgress(int id)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId))
    {
        return Challenge();
    }

    var progress = await _context.UserProgress
        .Include(p => p.Lesson)
        .ThenInclude(l => l.Module)
        .Where(p => p.UserId == userId && p.Lesson.Module.CourseId == id)
        .ToListAsync();

    var course = await _context.Courses
        .Include(c => c.Modules)
        .ThenInclude(m => m.Lessons)
        .FirstOrDefaultAsync(c => c.Id == id);

    return View(new CourseProgressVM { Course = course, Progress = progress });
}