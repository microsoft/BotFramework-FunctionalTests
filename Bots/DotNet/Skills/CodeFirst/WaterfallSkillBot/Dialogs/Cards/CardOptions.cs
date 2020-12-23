﻿namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Cards
{
    public enum CardOptions
    {
        /// <summary>
        /// Adaptive card - Bot action
        /// </summary>
        BotAction,
        
        /// <summary>
        /// Adaptive card - Task module
        /// </summary>
        TaskModule,

        /// <summary>
        /// Adaptive card - Submit action
        /// </summary>
        SumbitAction,

        /// <summary>
        /// Hero cards
        /// </summary>
        Hero,
        
        /// <summary>
        /// Thumbnail cards
        /// </summary>
        Thumbnail,

        /// <summary>
        /// Receipt cards
        /// </summary>
        Receipt,

        /// <summary>
        /// Signin cards
        /// </summary>
        Signin,

        /// <summary>
        /// Carousel cards
        /// </summary>
        Carousel,

        /// <summary>
        /// List cards
        /// </summary>
        List,

        /// <summary>
        /// O365 cards
        /// </summary>
        O365,

        /// <summary>
        /// File cards
        /// </summary>
        File,

        /// <summary>
        /// Animation cards
        /// </summary>
        Animation,

        /// <summary>
        /// Audio cards
        /// </summary>
        Audio,

        /// <summary>
        /// Video cards
        /// </summary>
        Video,

        /// <summary>
        /// UploadFile cards
        /// </summary>
        UploadFile,

        /// <summary>
        /// Ends the card selection dialog
        /// </summary>
        End
    }
}
