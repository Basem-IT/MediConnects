using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MediConnectMVC.Filters
{
    public class SessionAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.Session.GetString("UserName");

            if (string.IsNullOrEmpty(user))
            {
                context.Result = new RedirectToActionResult(
                    "Login",
                    "Account",
                    null);
            }

            base.OnActionExecuting(context);
        }
    }
}