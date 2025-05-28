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
using ui.Models.ParentSymptomViewModels;

namespace ui.Controllers
{
    public class ParentSymptomController : Controller
    {
        private readonly ILogger logger;
        private readonly UnitOfWork unitOfWork;
        private readonly IWebHostEnvironment hostingEnvironment;

        public ParentSymptomController(
            UnitOfWork uow,
            ILogger<ParentSymptomController> logger,
            IWebHostEnvironment hostingEnvironment)
        {
            this.unitOfWork = uow;
            this.hostingEnvironment = hostingEnvironment;
            this.logger = logger;
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
                return View(model);
            }

            DbParentSymptom data = new(unitOfWork)
            {
                Name = model.Name,
                Description = model.Description
            };

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

            DbParentSymptom data = await unitOfWork.GetObjectByKeyAsync<DbParentSymptom>(id);
            if (data == null)
            {
                return NotFound();
            }

            EditViewModel model = new EditViewModel
            {
                Id = data.OID,
                Name = data.Name,
                Description = data.Description
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

            DbParentSymptom data = await unitOfWork.GetObjectByKeyAsync<DbParentSymptom>(id);
            if (data == null)
            {
                return NotFound();
            }

            data.Name = model.Name;
            data.Description = model.Description;

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

            DbParentSymptom data = await unitOfWork.GetObjectByKeyAsync<DbParentSymptom>(id);
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
