using data;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ui.Models.QuestionViewModels
{
    public class CreateViewModel
    {
        [Display(Name = "Шаблон")]
        [Required]
        public string Template { get; set; }

        [Display(Name = "Родительский вопрос")]
        public Guid? ParentQuestionId { get; set; }

        [Display(Name = "Примечание")]
        public string? Description { get; set; }
    }
}
