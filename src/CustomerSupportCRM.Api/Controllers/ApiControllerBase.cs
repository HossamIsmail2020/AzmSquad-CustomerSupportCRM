using CustomerSupportCRM.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupportCRM.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult FromError(ServiceError error) => error.Type switch
    {
        ServiceErrorType.NotFound => NotFound(new { error.Message }),
        ServiceErrorType.Conflict => Conflict(new { error.Message }),
        ServiceErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error.Message }),
        ServiceErrorType.Validation => BadRequest(new { error.Message, errors = error.Fields }),
        _ => StatusCode(StatusCodes.Status500InternalServerError, new { error.Message })
    };
}
