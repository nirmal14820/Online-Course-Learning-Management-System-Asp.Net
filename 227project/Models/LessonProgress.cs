using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _227project.Models
{
    public class LessonProgress
    {
        public int Id { get; set; }

        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;

        public bool IsCompleted { get; set; } = true;

        // Foreign Keys
        [Required]
        public int LessonId { get; set; }

        [Required]
        public int EnrollmentId { get; set; }

        // Navigation properties
        [ForeignKey("LessonId")]
        public virtual Lesson Lesson { get; set; } = null!;

        [ForeignKey("EnrollmentId")]
        public virtual Enrollment Enrollment { get; set; } = null!;
    }
}
