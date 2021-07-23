using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot
{
    internal class ComponentSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether the runtime should use the TokenExchangeSkillHandler.
        /// </summary>
        /// <value>.</value>
        public bool UseTokenExchangeSkillHandler { get; set; } = false;

        /// <summary>
        /// Gets or sets the Connection Name to use for the single token exchange skill handler.
        /// </summary>
        /// <value>..</value>
        public string TokenExchangeConnectionName { get; set; }
    }
}
