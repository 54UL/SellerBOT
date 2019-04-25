using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmptyBot.Dialogs.Search
{
    public class SearchPiece
    {
        public string Vid { get; set; }
        public string Piece { get; set; }
    }

    public class SearchAccesors
    {
        public SearchAccesors(ConversationState conversationState)
        {
            ConversationState = conversationState
                ?? throw new ArgumentNullException(nameof(conversationState));
        }

        public static string DialogStateKey { get; private set; } = "SearchAccessors.DialogStateAccessor";
        public static string SearchPieceKey { get; private set; } = "SearchAccessors.SearchPiecAccessor";

        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        public IStatePropertyAccessor<DialogState> DialogStateAccessor { get; set; }
        public IStatePropertyAccessor<SearchPiece> SearchPieceAccessor { get; set; }

        public ConversationState ConversationState { get; }
    }
}
