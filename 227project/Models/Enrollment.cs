using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _227project.Models
{
    public class Enrollment
    {
        public int Id { get; set; }

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Foreign Keys
        [Required]
        public int CourseId { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("CourseId")]
        public virtual Course Course { get; set; } = null!;

        [ForeignKey("StudentId")]
        public virtual ApplicationUser Student { get; set; } = null!;
        
        public virtual ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
    }
}
