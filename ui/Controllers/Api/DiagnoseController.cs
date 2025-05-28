using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DevExpress.Xpo;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using ui.Controllers.Api;
using data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ui.Models.DiagnoseViewModels;
using DevExpress.Pdf;
using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;

namespace ui.Controllers.Api
{
    [Route("api/[controller]/[action]")]
    public class DiagnoseController : BaseApiDataAccessController<DbDiagnose>
    {
        private readonly ILogger logger;

        public DiagnoseController(
            UnitOfWork unitOfWork,
            ILogger<DiagnoseController> logger)
        {
            this.unitOfWork = unitOfWork;
            this.logger = logger;
        }

        [HttpGet]
        public object GetSymptoms(DataSourceLoadOptions loadOptions)
        {
            var dataToSend = unitOfWork.Query<DbDiagnoseSymptoms>()
                .Select(s => new SymptomsViewModel
                {
                    Symptom = s.Symptom,
                    Diagnose = s.Diagnose,
                    SymptomGivenDiagnoseP = s.SymptomGivenDiagnoseP,
                    Description = s.Description
                })
                .ToList();

            return DataSourceLoader.Load(dataToSend, loadOptions);
        }

    }
}