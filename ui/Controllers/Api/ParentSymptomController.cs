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
using ui.Models.ParentSymptomViewModels;

namespace ui.Controllers.Api
{
    [Route("api/[controller]")]
    public class ParentSymptomController : BaseApiDataAccessController<DbParentSymptom>
    {
        private readonly ILogger logger;

        public ParentSymptomController(
            UnitOfWork unitOfWork,
            ILogger<ParentSymptomController> logger)
        {
            this.unitOfWork = unitOfWork;
            this.logger = logger;
        }
    }
}