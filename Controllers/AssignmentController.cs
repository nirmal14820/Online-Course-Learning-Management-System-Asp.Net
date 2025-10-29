// ... existing code ...

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Submit(AssignmentSubmissionViewModel model, IFormFile file)
{
    if (!ModelState.IsValid)
    {
        return View("Assignment", model);
    }

    try
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        // Handle file upload if present
        string filePath = null;
        if (file != null && file.Length > 0)
        {
            var uploads = Path.Combine(_env.WebRootPath, "assignments");
            if (!Directory.Exists(uploads))
            {
                Directory.CreateDirectory(uploads);
            }
            
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            filePath = Path.Combine(uploads, fileName);
            
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            filePath = fileName; // Store relative path
        }

        // Check for existing submission
        var existingSubmission = _context.AssignmentSubmissions
            .FirstOrDefault(s => s.AssignmentId == model.AssignmentId && s.UserId == userId);
            
        if (existingSubmission != null)
        {
            // Update existing submission
            existingSubmission.Content = model.Content;
            existingSubmission.SubmissionDate = DateTime.Now;
            existingSubmission.Status = SubmissionStatus.Submitted;
            existingSubmission.FilePath = filePath ?? existingSubmission.FilePath;
            
            _context.AssignmentSubmissions.Update(existingSubmission);
        }
        else
        {
            // Create new submission
            var submission = new AssignmentSubmission
            {
                AssignmentId = model.AssignmentId,
                UserId = userId,
                Content = model.Content,
                SubmissionDate = DateTime.Now,
                Status = SubmissionStatus.Submitted,
                FilePath = filePath
            };
            
            _context.AssignmentSubmissions.Add(submission);
        }
        
        await _context.SaveChangesAsync();
        
        return RedirectToAction("Details", "Course", new { id = model.CourseId });
    }
    catch (Exception ex)
    {
        // Log the error
        ModelState.AddModelError("", "An error occurred while submitting the assignment. Please try again.");
        return View("Assignment", model);
    }
}