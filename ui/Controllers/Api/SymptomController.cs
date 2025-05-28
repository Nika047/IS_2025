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
using ui.Models.SymptomViewModels;

namespace ui.Controllers.Api
{
    [Route("api/[controller]")]
    public class SymptomController : BaseApiDataAccessController<DbSymptom>
    {
        private readonly ILogger logger;

        public SymptomController(
            UnitOfWork unitOfWork,
            ILogger<SymptomController> logger)
        {
            this.unitOfWork = unitOfWork;
            this.logger = logger;
        }
    }
}