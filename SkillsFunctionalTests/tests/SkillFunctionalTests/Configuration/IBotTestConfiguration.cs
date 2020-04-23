using System;
using System.Collections.Generic;
using System.Text;

namespace SkillFunctionalTests.Configuration
{
    public interface IBotTestConfiguration
    {
        string BotId { get; }

        string DirectLineSecret { get; }
    }
}
