namespace LumenicBackend.Utils
{
    public static class CostCalculator
    {
        public const double ONE_MILLION = 1000000.0;
        public const double ONE_MINUTE_IN_SECONDS = 60.0;
        public const double ONE_HOUR_IN_SECONDS = 3600.0;
        public const double NUM_USERS = 2.0;

        public const double VoipCostPerMinutePerUser = 0.004;
        public const double PstnInboundCostPerMinutePerUser = 0.022;
        public const double PstnOutboundCostPerMinutePerUser = 0.013;
        public const double SpeechToTextCostPerHour = 1.0;
        public const double TextToSpeechCostPerOneMillionCharacters = 15.0;
        public const double RecordingCostUnmixedPerMinute = 0.002;

        public const double VoipCostPerSecondPerUser = VoipCostPerMinutePerUser / ONE_MINUTE_IN_SECONDS;
        public const double PstnInboundCostPerSecondPerUser = PstnInboundCostPerMinutePerUser / ONE_MINUTE_IN_SECONDS;
        public const double PstnOutboundCostPerSecondPerUser = PstnOutboundCostPerMinutePerUser / ONE_MINUTE_IN_SECONDS;
        public const double SpeechToTextCostPerSecond = SpeechToTextCostPerHour / ONE_HOUR_IN_SECONDS;
        public const double TextToSpeechCostPerCharacter = TextToSpeechCostPerOneMillionCharacters / ONE_MILLION;
        public const double RecordingCostUnmixedPerSecond = RecordingCostUnmixedPerMinute / ONE_MINUTE_IN_SECONDS;
        public const double ChatCostPerMessage = 0.0008;
        public const double SmsInboundCostPerMessage = 0.0075;
        public const double SmsOutboundCostPerMessage = 0.0075;
        public const double SmsCarrierInboundCostPerMessage = 0.0025;
        public const double SmsCarrierOutboundCostPerMessage = 0.0025;

        public static readonly IDictionary<string, LlmCost> llmCostPerToken = new Dictionary<string, LlmCost>()
        {
            [StartupConstants.CallClassificationModelName] = new LlmCost(0.07 / ONE_MILLION, 0.07 / ONE_MILLION),
            [StartupConstants.CallDetermineActionNeededModelName] = new LlmCost(0.07 / ONE_MILLION, 0.07 / ONE_MILLION),
            [StartupConstants.CallToolsInvokerModelName] = new LlmCost(0.24 / ONE_MILLION, 0.24 / ONE_MILLION),
            [StartupConstants.CallConversationModelName] = new LlmCost(0.65 / ONE_MILLION, 0.65 / ONE_MILLION),
            [StartupConstants.EmbeddingModelName] = new LlmCost(0.02 / ONE_MILLION, 0.0),
            [StartupConstants.GeneralPurposeModelName] = new LlmCost(0.50 / ONE_MILLION, 1.5 / ONE_MILLION),
        };

        public static double CalculateTelephonyCost(double durationInSeconds, string direction)
        {
            double pstnCost = PstnInboundCostPerSecondPerUser;
            if (direction.Equals(CallDirection.Outbound.ToString(), StringComparison.InvariantCultureIgnoreCase))
            {
                pstnCost = PstnOutboundCostPerSecondPerUser;
            }

            double userCost = pstnCost * durationInSeconds;
            double botCost = VoipCostPerSecondPerUser * durationInSeconds;

            return userCost + botCost;
        }

        public static double CalculateRecordingCost(double durationInSeconds)
        {
            return durationInSeconds * RecordingCostUnmixedPerSecond;
        }

        public static double CalculateMessagingCost(int numMessages)
        {
            return numMessages * ChatCostPerMessage;
        }

        public static double CalculateSmsInboundCost(int numMessages)
        {
            return numMessages * (SmsInboundCostPerMessage + SmsCarrierInboundCostPerMessage);
        }

        public static double CalculateSmsOutboundCost(int numMessages)
        {
            return numMessages * (SmsOutboundCostPerMessage + SmsCarrierOutboundCostPerMessage);
        }

        public static double CalculateSpeechToTextCost(int durationInSeconds)
        {
            return durationInSeconds * SpeechToTextCostPerSecond;
        }

        public static double CalculateTextToSpeechCost(int characterCount)
        {
            return characterCount * TextToSpeechCostPerCharacter;
        }

        public static double CalculateLlmInCost(double tokenCount, string modelName)
        {
            var inputCost = llmCostPerToken[modelName].Input;
            return tokenCount * inputCost;
        }

        public static double CalculateLlmOutCost(double tokenCount, string modelName)
        {
            var outputCost = llmCostPerToken[modelName].Output;
            return tokenCount * outputCost;
        }

        public static void IncrementLlmUsageTokens(this Dictionary<string, UsageResult> usageDictionary, string key, int inputTokens, int outputTokens)
        {
            if (usageDictionary != null)
            {
                if (usageDictionary.ContainsKey(key))
                {
                    usageDictionary[key].PromptTokens += inputTokens;
                    usageDictionary[key].CompletionTokens += outputTokens;
                }
                else
                {
                    usageDictionary[key] = UsageResultHelper.GetEmpty();
                }
            }
        }

        public static (double, double) CalculateTotalInputAndOutputTokenCost(ResourceUsage usage)
        {
            double inputCost = 0;
            double outputCost = 0;

            foreach(var kvp in usage.LlmUsageResults)
            {
                var inputUsage = kvp.Value.PromptTokens;
                var outputUsage = kvp.Value.CompletionTokens;

                var inputUnitCost = llmCostPerToken[kvp.Key].Input;
                var outputUnitCost = llmCostPerToken[kvp.Key].Output;

                inputCost += inputUsage * inputUnitCost;
                outputCost += outputUsage * outputUnitCost;
            }
            return (inputCost, outputCost);
        }
    }
}
