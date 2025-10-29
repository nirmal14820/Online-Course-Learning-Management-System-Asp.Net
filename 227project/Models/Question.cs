using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _227project.Models
{
    public class Question
    {
        public int Id { get; set; }

        [Required]
        public string QuestionText { get; set; } = string.Empty;

        [Required]
        public string OptionA { get; set; } = string.Empty;

        [Required]
        public string OptionB { get; set; } = string.Empty;

        [Required]
        public string OptionC { get; set; } = string.Empty;

        [Required]
        public string OptionD { get; set; } = string.Empty;

        [Required]
        [StringLength(1)]
        public string CorrectAnswer { get; set; } = string.Empty; // A, B, C, or D

        public int Points { get; set; } = 1;

        public int Order { get; set; }

        // Foreign Keys
        [Required]
        public int QuizId { get; set; }

        // Navigation properties
        [ForeignKey("QuizId")]
        public virtual Quiz Quiz { get; set; } = null!;
        
        public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }
}
