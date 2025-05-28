using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ui.Helper;
using WebApp.Extensions;

namespace ui.Controllers
{
    public class ExpertController : Controller
    {
        private readonly ExpertEngine _engine;
        private const string SessionKey = "STATE";
        private IConfiguration _configuration;

        public ExpertController(ExpertEngine engine, IConfiguration configuration)
        {
            _engine = engine;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var state = new SessionState();
            _engine.Initialize(state);
            HttpContext.Session.Set(SessionKey, state); 

            return RedirectToAction(nameof(Question));
        }

        public IActionResult Question()
        {
            var state = HttpContext.Session.Get<SessionState>(SessionKey)!;
            if (state.Finished) 
                return RedirectToAction(nameof(Result));

            var next = _engine.PickNextQuestion(state);
            if (next is null) 
                return RedirectToAction(nameof(Result));

            ViewData["SymptomId"] = next.OID;
            ViewData["SymptomName"] = next.Name;
            ViewData["Asked"] = state.AskedSymptomIds.Count + 1;
            ViewData["Total"] = _engine.TotalSymptoms; 
            ViewData["Question"] = next.Question;

            return View();
        }

        [HttpPost]
        public IActionResult Answer(Guid symptomId, bool answerYes)
        {
            var state = HttpContext.Session.Get<SessionState>(SessionKey)!;
            _engine.Update(state, symptomId, answerYes);
            HttpContext.Session.Set(SessionKey, state);

            return RedirectToAction(nameof(Question));
        }

        public IActionResult Result()
        {
            var state = HttpContext.Session.Get<SessionState>(SessionKey)!;

            if (!state.CurrentPosteriors.Any())
            {
                ViewBag.Message = "Не удалось определить подходящую страну";
                return View("NoResult");
            }

            var model = state.CurrentPosteriors
                .OrderByDescending(p => p.Value)
                .Select(p => new
                {
                    Diagnosis = _engine.DiagnosisName(p.Key),
                    Probability = p.Value
                });
            
            return View(model);
        }
    }
}
