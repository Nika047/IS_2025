using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ui.Controllers
{
    [AllowAnonymous]
    public class LocalizationController : Controller
    {
        public ActionResult CldrData()
        {
            return new CldrDataScriptBuilder()
                .SetCldrPath("~/wwwroot/js/cldr-core")
                .SetInitialLocale("ru")
                .UseLocales("ru")
                .Build();
        }
    }
}
