using data;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ui.Models.ParentSymptomViewModels
{
    public class CreateViewModel
    {
        [Display(Name = "Наименование")]
        [Required]
        public string Name { get; set; }

        [Display(Name = "Примечание")]
        public string? Description { get; set; }
    }
}
