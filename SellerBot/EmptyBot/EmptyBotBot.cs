// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using StaticAppStringDefines;
using Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System.Collections.Generic;
using EmptyBot.Dialogs.Search;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace EmptyBot
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service. Transient lifetime services are created
    /// each time they're requested. Objects that are expensive to construct, or have a lifetime
    /// beyond a single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class EmptyBotBot : IBot
    {
        private DateTime logTime;
        private BotService _services;
        private PartsTechAPI _API_parts;
        private DialogSet _set;
        private SearchAccesors Accesor { get; set; }

        private Dictionary<string, object> part_info_g = new Dictionary<string, object>();

        private async Task<DialogTurnResult> PromptForVid(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Prompt for the location.
            return await stepContext.PromptAsync(
                "TextPrompt",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Which PIECE id?")
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForKeyword(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["piece"] = stepContext.Result;

            return await stepContext.PromptAsync(
                "TextPrompt",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please, insert a keyword")
                },
                cancellationToken);
        }


        private async Task<DialogTurnResult> PromptForQuoute(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // stepContext.Values["piece"] = stepContext.Result;
            return await stepContext.PromptAsync(
                "choicePrompt",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Would you like to quote this item?"),
                    Choices = new List<Choice> { new Choice("Yes"), new Choice("No") }
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmQuote(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var lastResponse = (FoundChoice)stepContext.Result;
            switch (lastResponse.Value)
            {
                case "Yes":

                    dynamic results = await _API_parts.GetQuote((string)Accesor.Parameters["partNumber"]);
                    var name = results.parts[0]?.store?.name;
                    var price = results.parts[0]?.price.list;
                    await stepContext.Context.SendActivityAsync($"We have this part in this store " +
                   $"{name} and costs ${price} ");

                    break;

                case "No":

                    break;
            }

            await stepContext.EndDialogAsync();
            // stepContext.Values["piece"] = stepContext.Result;
            return new DialogTurnResult(DialogTurnStatus.Complete);
        }


        private async Task<DialogTurnResult> PromptForUser(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            return await stepContext.PromptAsync(
                "TextPrompt",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("What's your username?")
                },
                cancellationToken);
        }





        private async Task<DialogTurnResult> ShowPieceInformation(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            stepContext.Values["keyword"] = stepContext.Result;


            await stepContext.Context.SendActivityAsync("Piece info:");
            await stepContext.EndDialogAsync();

            return new DialogTurnResult(DialogTurnStatus.Empty);
        }

        private async Task<DialogTurnResult> PromptForPassword(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["user"] = stepContext.Result;
            return await stepContext.PromptAsync(
                "TextPrompt",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("What's your password?")
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> DoLogin(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["password"] = stepContext.Result;

            // TODO aquí
            // usuario: stepContext.Values["user"]
            // contraseña: stepContext.Values["password"]
            var result = await PartsTechAPI.AutorizeLogin(new
            {
                user = new
                {
                    id = stepContext.Values["user"],
                    key = stepContext.Values["password"]
                },
                partner = new
                {
                    id = "beta_bosch",
                    key = "4700fc1c26dd4e54ab26a0bc1c9dd40d"
                }
            });
            await stepContext.Context.SendActivityAsync(result == null ? "Could not login" : "Login succesful");
            await stepContext.EndDialogAsync();
            return new DialogTurnResult(DialogTurnStatus.Empty);
        }

        private PartsTechAPI PartsTechAPI { get; set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>                        
        public EmptyBotBot(IConfiguration configuration, BotService service, SearchAccesors searchData, PartsTechAPI parts)
        {
            PartsTechAPI = parts;
            Accesor = searchData;
            var conversationState = searchData.ConversationState;
            _set = new DialogSet(conversationState.CreateProperty<DialogState>("dialogState"));

            _set.Add(new WaterfallDialog("searchDialog")
                        .AddStep(PromptForVid)
                        .AddStep(PromptForKeyword)
                        .AddStep(ShowPieceInformation));


            _set.Add(new TextPrompt("TextPrompt"));

            _set.Add(new WaterfallDialog("loginDialog")
                .AddStep(PromptForUser)
                .AddStep(PromptForPassword)
                .AddStep(DoLogin));

            _set.Add(new ChoicePrompt("choicePrompt"));
            _set.Add(new ConfirmPrompt("confirmPrompt"));


            _set.Add(new WaterfallDialog("choiceDialog")
                .AddStep(PromptForQuoute)
                .AddStep(ConfirmQuote));

            _services = service;
            _API_parts = parts;
        }

        /// <summary>
        /// Every conversation turn calls this method.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var dc = await _set.CreateContextAsync(turnContext, cancellationToken);

                var recognizerResult = await _services.LuisServices["SellerBot"].RecognizeAsync(turnContext, cancellationToken);
                var topIntent = recognizerResult?.GetTopScoringIntent();
                var context = await _set.CreateContextAsync(turnContext, cancellationToken);
                if (context.ActiveDialog != null)
                {
                    await context.ContinueDialogAsync(cancellationToken);
                    await Accesor.ConversationState.SaveChangesAsync(
                turnContext, false, cancellationToken);
                    return;
                }


                switch (Enum.Parse<TextRecognizer.Intent>(topIntent.Value.intent))
                {
                    case TextRecognizer.Intent.Search:
                        await turnContext.SendActivityAsync("Searching...");
                        var tempResult = await _API_parts.GetItemByID(recognizerResult.Entities["PId"].First.Value<string>().ToUpper());
                        var result_pieces = tempResult?.partName;
                        var blabla = tempResult?.partNumber as JToken;
                        Accesor.Parameters["partNumber"] = blabla.Value<string>();
                        JArray image = tempResult?.images;
                        dynamic obj = image[image.Count > 2 ? 1 : image.Count - 1];
                        var attachment = new Attachment
                        {
                            ContentUrl = obj?.medium,
                            ContentType = "image/png",
                            Name = result_pieces
                        };
                        var reply = turnContext.Activity.CreateReply();
                        // Add the attachment to our reply.
                        reply.Attachments = new List<Attachment>() { attachment };


                        await turnContext.SendActivityAsync($"Sure, this is the name:\n {result_pieces}, and this is an image", cancellationToken: cancellationToken);
                        await turnContext.SendActivityAsync(reply, cancellationToken: cancellationToken);

                        await context.BeginDialogAsync("choiceDialog", null, cancellationToken);
                        break;
                    case TextRecognizer.Intent.StartSearch:
                        // await turnContext.SendActivityAsync(StaticAppStringDefines.GenericAnwsers.SearchMessage);
                        var result = await context.BeginDialogAsync("searchDialog", null, cancellationToken);

                        // obtenemos el texto del bot
                        //  await 
                        //   _API_parts.GetItemByID(stepContext.Values["keyword"]).Result.vehicleName
                        //llamamos la funcion de la api

                        //regresamos los valores que retorno la api



                        break;
                    case TextRecognizer.Intent.AddCart:
                        break;
                    case TextRecognizer.Intent.Shop:
                        var ShopResult = await _API_parts.GetShop();
                        var phone = ShopResult?.phone;
                        var cellphone = ShopResult?.cellphone;
                        dynamic address = ShopResult?.address;
                        string stringAddress;

                        try
                        {
                            stringAddress = string.Format("{0}, {1}\n {2}, {3}, {4}\n{5}",
                                                    address.address1, address.address2 ?? "",
                                                    address.city, address.state, address.zipCode,
                                                    address.country);
                        }
                        catch (Exception)
                        {

                            throw;
                        }

                        await turnContext.SendActivityAsync("Sure!");
                        await turnContext.SendActivityAsync($"Here's the contact info tel:{phone}, cel:{cellphone}");
                        await turnContext.SendActivityAsync($"Or visit the store at {stringAddress}");
                        break;
                    case TextRecognizer.Intent.GetQuote:

                        //check input

                        break;
                    case TextRecognizer.Intent.Greetings:
                        await turnContext.SendActivityAsync(StaticAppStringDefines.GenericAnwsers.GreetingsMessage,
                            cancellationToken: cancellationToken,
                            speak: StaticAppStringDefines.GenericAnwsers.GreetingsMessage);
                        break;
                    case TextRecognizer.Intent.Help:
                        await turnContext.SendActivityAsync(StaticAppStringDefines.GenericAnwsers.HelpMessage, cancellationToken: cancellationToken);
                        break;
                    case TextRecognizer.Intent.Login:
                        await context.BeginDialogAsync("loginDialog", null, cancellationToken);
                        break;
                    case TextRecognizer.Intent.None:
                        await turnContext.SendActivityAsync(StaticAppStringDefines.GenericAnwsers.NoneMessage, cancellationToken: cancellationToken);
                        break;
                    case TextRecognizer.Intent.RemoveCart:
                        break;
                    case TextRecognizer.Intent.RequestQuote:
                        // string message_quote_info = await _API_parts.getQuoteInfo().Result;
                        // await turnContext.SendActivityAsync("quote of " + message_quote_info, cancellationToken: cancellationToken);
                        break;
                }
                await Accesor.ConversationState.SaveChangesAsync(
                turnContext, false, cancellationToken);
            }
        }
    }
}
