namespace LumenicBackend.Utils
{
    public static class IntermediateResponseGenerator
    {
        public static IList<string> Responses => new List<string>()
        {
            "Give me a sec, let me take a look.",
            "Uhh, give me one second.",
            "Sure, let me get back to you real quick.",
            "Just a moment.",
            "Please hold for a second.",
            "Bear with me for a second.",
        };
        public static Random Random = new Random();

        public static string Generate()
        {
            var randomIndex = Random.Next(Responses.Count);
            return Responses[randomIndex];
        }
    }
}
