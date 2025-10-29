using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _227project.Models
{
    public class Answer
    {
        public int Id { get; set; }

        [Required]
        [StringLength(1)]
        public string SelectedAnswer { get; set; } = string.Empty; // A, B, C, or D

        public bool IsCorrect { get; set; } = false;

        public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        [Required]
        public int QuestionId { get; set; }

        [Required]
        public int QuizAttemptId { get; set; }

        // Navigation properties
        [ForeignKey("QuestionId")]
        public virtual Question Question { get; set; } = null!;

        [ForeignKey("QuizAttemptId")]
        public virtual QuizAttempt QuizAttempt { get; set; } = null!;
    }
}
