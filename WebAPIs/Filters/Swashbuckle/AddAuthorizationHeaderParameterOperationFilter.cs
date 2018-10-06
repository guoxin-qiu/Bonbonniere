using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;

namespace WebAPIs.Filters.Swashbuckle
{
    public class AddAuthorizationHeaderParameterOperationFilter : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
            {
                operation.Parameters = new List<IParameter>();
            }

            var filterPipeline = context.ApiDescription.ActionDescriptor.FilterDescriptors;

            var allowAnonymous = false;

            if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                allowAnonymous = descriptor.MethodInfo.GetCustomAttributes(inherit: true).Any(a => a is AllowAnonymousAttribute);
            }

            if (!allowAnonymous)
            {
                operation.Parameters.Add(new HeaderParameter()
                {
                    Name = "Authorization",
                    In = "header",
                    Type = "string",
                    Required = true
                });
            }
        }
    }

    class HeaderParameter : NonBodyParameter
    {
    }
}
