// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;

namespace Microsoft.BotFrameworkFunctionalTests.CardSkillBot.Controllers
{
    // This ASP Controller is created to handle a request. Dependency Injection will provide the Adapter and IBot
    // implementation at runtime. Multiple different IBot implementations running at different endpoints can be
    // achieved by specifying a more specific type for the bot constructor argument.
    
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly IBot _bot;

        public BotController(IBotFrameworkHttpAdapter adapter, IBot bot)
        {
            _adapter = adapter;
            _bot = bot;
        }

        [Route("api/messages")]
        [HttpPost]
        public async Task PostAsync()
        {
            await _adapter.ProcessAsync(Request, Response, _bot);
        }

        [Route("api/bell")]
        [HttpGet]
        public ActionResult ReturnFile()
        {
            var filename = Constants.BellSound;
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Files", filename);
            byte[] fileData = System.IO.File.ReadAllBytes(filePath);

            /*
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Files", filename);
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var br = new BinaryReader(fs);
            long numBytes = new FileInfo(filePath).Length;
            var buff = br.ReadBytes((int)numBytes);
            
            return File(buff, "audio/mp3", "callrecording.wav");
            */

            return File(fileData, "audio/mp3");
        }
    }
}
