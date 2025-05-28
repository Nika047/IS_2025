using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Security.Cryptography.Xml;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Web;
using DevExpress.CodeParser;
using DevExpress.Xpo;
using DevExpress.XtraRichEdit.Import.Html;
using data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using DevExpress.Pdf.Native.BouncyCastle.Utilities.Collections;
using DevExpress.Compatibility.System.Web;
using ui.Models.DiagnoseViewModels;
using Microsoft.Extensions.Options;
using ui.Settings;

namespace ui.Controllers
{
    public class DiagnoseController : Controller
    {
        private readonly ILogger logger;
        private readonly UnitOfWork unitOfWork;
        private readonly IWebHostEnvironment hostingEnvironment;
        private readonly IOptions<TouristsSettings> touristsSettings;
        private float touristsTotal;

        public DiagnoseController(
            UnitOfWork uow,
            ILogger<DiagnoseController> logger,
            IWebHostEnvironment hostingEnvironment,
            IOptions<TouristsSettings> touristsSettings)
        {
            this.unitOfWork = uow;
            this.hostingEnvironment = hostingEnvironment;
            this.logger = logger;
            this.touristsSettings = touristsSettings;
            touristsTotal = float.Parse(touristsSettings.Value.Total);
        }

        [TempData]
        public string ErrorMessage { get; set; }

        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult Create() => View(new CreateViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"Validation error: {error.ErrorMessage}");
                }
                return View(model);
            }

            DbDiagnose data = new(unitOfWork)
            {
                Country = model.Country,
                PriorP = model.TouristsCount / touristsTotal,
                TouristsCount = model.TouristsCount,
                Description = model.Description
            };

            data.DiagnoseSymptoms.AddRange(model.SymptomsList.Select(c => new DbDiagnoseSymptoms(unitOfWork)
            {
                Symptom = unitOfWork.GetObjectByKeyAsync<DbSymptom>(c.SymptomId).Result,
                SymptomGivenDiagnoseP = c.SymptomGivenDiagnoseP,
                Description = c.Description
            }));

            try
            {
                var behaviors = DataObjectBehaviorAttribute.GetBhvInstancies(data.GetType());

                foreach (var bhv in behaviors)
                {
                    bhv.OnSaving(data as IDatabaseObject);
                }

                await unitOfWork.CommitChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Ошибка записи данных {data.GetType().Name} {ex.Message}");
                ModelState.AddModelError("", "Ошибка записи данных, обратитесь к администратору!");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            DbDiagnose data = await unitOfWork.GetObjectByKeyAsync<DbDiagnose>(id);
            if (data == null)
            {
                return NotFound();
            }

            EditViewModel model = new()
            {
                Id = data.OID,
                Country = data.Country,
                TouristsCount = data.TouristsCount,
                Description = data.Description,
                SymptomsList = data.DiagnoseSymptoms
                    .Select(c => new DiagnoseSymptomsViewModel 
                    {
                        SymptomId = c.Symptom.OID,
                        SymptomGivenDiagnoseP = c.SymptomGivenDiagnoseP,
                        Description = c.Description
                    })
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            DbDiagnose data = await unitOfWork.GetObjectByKeyAsync<DbDiagnose>(id);
            if (data == null)
            {
                return NotFound();
            }

            data.Country = model.Country;
            data.TouristsCount = model.TouristsCount;
            data.PriorP = model.TouristsCount / touristsTotal;
            data.Description = model.Description;

            while (data.DiagnoseSymptoms.Any())
            {
                data.DiagnoseSymptoms.Remove(data.DiagnoseSymptoms.First());
            }

            data.DiagnoseSymptoms.AddRange(model.SymptomsList.Select(c => new DbDiagnoseSymptoms(unitOfWork)
            {
                Symptom = unitOfWork.GetObjectByKeyAsync<DbSymptom>(c.SymptomId).Result,
                SymptomGivenDiagnoseP = c.SymptomGivenDiagnoseP,
                Description = c.Description
            }));

            try
            {
                var behaviors = DataObjectBehaviorAttribute.GetBhvInstancies(data.GetType());

                foreach (var bhv in behaviors)
                {
                    bhv.OnSaving(data as IDatabaseObject);
                }

                await unitOfWork.CommitChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Ошибка записи {data.GetType().Name} {ex.Message}");
                ModelState.AddModelError("", "Ошибка записи данных, обратитесь к администратору!");
                return View(model);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Remove(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            DbDiagnose data = await unitOfWork.GetObjectByKeyAsync<DbDiagnose>(id);
            if (data == null)
            {
                return NotFound();
            }

            try
            {
                data.Delete();
                await unitOfWork.CommitChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Deleting Error");
                logger.LogError(ex, $"Ошибка удаления данных {data.GetType().Name} {ex.Message}");
                ModelState.AddModelError("", "Ошибка удаления данных, обратитесь к администратору!");
            }

            return RedirectToAction("Index");
        }
    }
}
