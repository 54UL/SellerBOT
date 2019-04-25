




namespace StaticAppStringDefines
{


    static class GenericAnwsers 
    {

        static GenericAnwsers()
        {

            
        }

        private static string Search_Message = "Which piece are you interested for?";
        public static string SearchMessage
        {
            get{ return Search_Message; }
            set{ Search_Message = value; }
        }

        private static string Help_Message = "You typed help for some help but there is not help yet, see comands";
        public static string HelpMessage 
        {
            get{ return Help_Message; }
            set{ Help_Message = value;}

        }
        
        private static string Login_Message = "Introduce tu usuario y contrasenia";
        public static string LoginMessage 
        {
            get{ return Login_Message; }
            set{ Login_Message = value; }

        }

        private static string None_Message = "I don't understand you";
        public static string NoneMessage
        {
            get{ return None_Message; }
            set{ None_Message = value; }
        }

        private static string Greetings_Message = "hi, I´m a sales assistant bot";
        public static string GreetingsMessage
        {
            get{ return Greetings_Message; }
            set{ Greetings_Message = value; }
        }


    }



}