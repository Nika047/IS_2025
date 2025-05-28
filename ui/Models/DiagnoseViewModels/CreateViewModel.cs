using data;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ui.Models.DiagnoseViewModels
{
    public class CreateViewModel
    {
        [Display(Name = "Страна")]
        [Required]
        public string Country { get; set; }

        [Display(Name = "Количество туристов (млн)")]
        [Required]
        public float TouristsCount { get; set; }

        [Display(Name = "Список признаков")]
        [Required]
        public List<DiagnoseSymptomsViewModel> SymptomsList { get; set; } = new();

        [Display(Name = "Примечание")]
        public string? Description { get; set; }
    }
}
