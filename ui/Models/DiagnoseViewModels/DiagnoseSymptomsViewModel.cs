using data;
using System.ComponentModel.DataAnnotations;

namespace ui.Models.DiagnoseViewModels
{
    public class DiagnoseSymptomsViewModel
    {
        [Display(Name = "Признак")]
        [Required]
        public Guid SymptomId { get; set; }

        [Display(Name = "Вероятность наличия признака для данной страны")]
        [Required]
        public float SymptomGivenDiagnoseP { get; set; }
        
        [Display(Name = "Примечание")]
        public string? Description { get; set; }
    }
    
    public class SymptomsViewModel
    {
        public DbDiagnose Diagnose { get; set; }
        public DbSymptom Symptom { get; set; }

        [Display(Name = "Вероятность наличия признака для данной страны")]
        public float SymptomGivenDiagnoseP { get; set; }

        [Display(Name = "Примечание")]
        public string? Description { get; set; }



        [Display(Name = "Признак")]
        public string SymptomName => Symptom.Name;
        
        [Display(Name = "Родительский признак")]
        public string ParentSymptomName => Symptom.ParentSymptom.Name;
    }
}
