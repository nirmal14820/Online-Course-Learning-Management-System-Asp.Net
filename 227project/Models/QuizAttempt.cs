using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _227project.Models
{
    public class QuizAttempt
    {
        public int Id { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public int Score { get; set; } = 0;

        public int TotalQuestions { get; set; } = 0;

        public int CorrectAnswers { get; set; } = 0;

        public int AttemptNumber { get; set; } = 1;

        // Foreign Keys
        [Required]
        public int QuizId { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        // Navigation properties
        [ForeignKey("QuizId")]
        public virtual Quiz Quiz { get; set; } = null!;

        [ForeignKey("StudentId")]
        public virtual ApplicationUser Student { get; set; } = null!;
        
        public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }
}
