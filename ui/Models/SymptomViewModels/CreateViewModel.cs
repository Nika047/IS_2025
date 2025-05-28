using data;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ui.Models.SymptomViewModels
{
    public class CreateViewModel
    {
        [Display(Name = "Наименование")]
        [Required]
        public string Name { get; set; }

        [Display(Name = "Родительский признак")]
        [Required]
        public Guid ParentSymptomId { get; set; }

        [Display(Name = "Примечание")]
        public string? Description { get; set; }

        [Display(Name = "Вероятность наличия признака для страны")]
        [Required]
        public float SymptomGivenNotDiagnoseP { get; set; }
        
        [Display(Name = "Вопрос")]
        [Required]
        public string Question { get; set; }
    }
}
