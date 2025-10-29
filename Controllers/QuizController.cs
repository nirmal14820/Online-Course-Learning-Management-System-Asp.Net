// ... existing code ...

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Submit(QuizSubmission model)
{
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    try
    {
        model.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        model.SubmissionDate = DateTime.Now;
        
        _context.QuizSubmissions.Add(model);
        await _context.SaveChangesAsync();
        
        return RedirectToAction("Results", new { id = model.QuizId });
    }
    catch (Exception ex)
    {
        // Log error
        ModelState.AddModelError("", "Error submitting quiz. Please try again.");
        return View(model);
    }
}