using Microsoft.AspNetCore.Mvc;

namespace BuildBazaarCore.Services
{
    public abstract class BaseService
    {
        public virtual JsonResult Json(object? data)
        {
            return new JsonResult(data);
        }
    }
}
