using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Xpo;
using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Data.ResponseModel;
using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ui.Controllers.Api
{
    public class BaseApiDataAccessController<T> : Controller where T : XPBaseObject
    {
        protected UnitOfWork unitOfWork;

        [HttpGet]
        public virtual object Get(DataSourceLoadOptions loadOptions)
        {
            return DataSourceLoader.Load(unitOfWork.Query<T>(), loadOptions);
        }

        
        protected async virtual Task<IActionResult> Remove(Guid id)
        {
            XPBaseObject data = await unitOfWork.GetObjectByKeyAsync<T>(id);

            if (data == null)
            {
                return NotFound();
            }

            try
            {
                unitOfWork.Delete(data);
                await unitOfWork.CommitChangesAsync();
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, $"Ошибка записи {data.GetType().Name} {ex.Message}");
                //ModelState.AddModelError("", "Ошибка записи данных. Попробуйте еще раз, если проблема останется, обратитесь к администратору!");
                //return RedirectToAction("Index");
                return BadRequest(ex.Message);

            }

            return Ok();
        }
    }
}