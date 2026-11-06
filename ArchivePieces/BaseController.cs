using ArchivePieces.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ArchivePieces
{
    // Base controller that injects shared view data from HttpContext.Items
    // (populated by the middleware in Program.cs)
    public class BaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            if (HttpContext.Items.TryGetValue("NavCategories", out var cats))
                ViewBag.NavCategories = cats as List<Category>;

            if (HttpContext.Items.TryGetValue("CartCount", out var count))
                ViewBag.CartCount = count;
        }
    }
}
