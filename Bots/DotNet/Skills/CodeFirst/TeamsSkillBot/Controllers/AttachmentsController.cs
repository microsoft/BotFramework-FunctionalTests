// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Controllers
{
    // This ASP Controller is created to handle a request. Dependency Injection will provide the Adapter and IBot
    // implementation at runtime. Multiple different IBot implementations running at different endpoints can be
    // achieved by specifying a more specific type for the bot constructor argument.
    [ApiController]
    public class AttachmentsController
    {
        private static readonly string Attachment = "architecture-resize.png";

        [Route("api/attachments")]
        [HttpGet]
        public FileResult Get()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Dialogs", "Attachments", "Files", Attachment);

            return new FileStreamResult(new FileStream(path, FileMode.Open), "image/png");
        }
    }
}
