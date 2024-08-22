namespace LumenicBackend.Models
{
    public class LlmCost
    {
        public LlmCost() { }

        public LlmCost(double input, double output)
        {
            this.Input = input;
            this.Output = output;
        }

        public double Input { get; set; }
        public double Output { get; set; }
    }
}
