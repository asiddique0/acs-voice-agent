using Microsoft.EntityFrameworkCore.Migrations;

namespace LumenicBackend.Utils
{
    public static class TestTools
    {
        public static ToolDefinition GetTestToolOneDefinition()
        {
            var name = "TestToolOne";
            var description = "This is a test tool that is always called regardless of the conversation history or prompt. It is used for debugging purposes.";
            var toolDefinition = new ToolDefinition()
            {
                Name = name,
                Description = description,
                Parameters = new()
                {
                    Type = "object",
                    Properties = new()
                    {
                        Properties = new()
                        {
                            ["parameterOne"] = JToken.FromObject(
                            new Property()
                            {
                                Type = "string",
                                Description = "Pass a \"TEST\" into this parameter.",
                                Enum = ["TEST"],
                            })
                        }
                    },
                    Required = ["parameterOne"],
                }
            };
            return toolDefinition;
        }

        public static Tool GetTestToolOne()
        {
            var testToolOne = GetTestToolOneDefinition();

            return new Tool()
            {
                Id = Guid.Parse("04c87cfe-d241-459b-81fd-0e50ed34889f"),
                ExecutionFrequency = ToolTypes.AfterCall,
                Name = testToolOne.Name,
                Description = testToolOne.Description,
                Url = "http://localhost:3001/api/testToolOne",
                Structure = JsonConvert.SerializeObject(testToolOne),
                OrganizationId = Guid.Parse("b0b8ff1b-1ecd-41f2-9037-547ce075df66"),
            };
        }
    }
    public partial class SeedData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var createdAt = DateTime.Now;
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
            var testToolOne = TestTools.GetTestToolOne();

            migrationBuilder.InsertData(
                table: "Organizations",
                columns: ["Id", "Name", "Email", "Active", "CreatedAt"],
                values: [Guid.Parse("b0b8ff1b-1ecd-41f2-9037-547ce075df66"), "Nikola Solar", "test@gmail.com", true, createdAt]);

            migrationBuilder.InsertData(
                table: "Numbers",
                columns: ["Id", "NumberValue", "TransferNumber", "TransferWeight", "Active", "StartHour", "EndHour", "TimeZone","CreatedAt", "OrganizationId"],
                values: [Guid.Parse("c0e707d1-9179-4597-b675-495530404c17"), "+18552387235", "+11291355474", 0, true, 0, 24, timeZone.Id, createdAt, Guid.Parse("b0b8ff1b-1ecd-41f2-9037-547ce075df66")]);

            migrationBuilder.InsertData(
                table: "Agents",
                columns: ["Id", "Name", "PhoneNumber", "Greeting", "VoiceModel", "Description", "Instructions", "SearchIndex", "Recorded", "CreatedAt", "OrganizationId"],
                values: [Guid.Parse("60405beb-f3c5-4d35-ab06-a36b5e57671d"), "Alyssa Shannon", "+18552387235", "Hello, you're speaking to Alyssa", "en-US-AvaNeural", "placeholder description", File.ReadAllText("D:\\Documents\\Development\\voice-agent-backend\\Static\\SystemTemplate.txt"), "index", true, createdAt, Guid.Parse("b0b8ff1b-1ecd-41f2-9037-547ce075df66")]);

            migrationBuilder.InsertData(
                table: "Tools",
                columns: ["Id", "ExecutionFrequency", "Name", "Description", "Url", "Structure", "OrganizationId"],
                values: [testToolOne.Id, testToolOne.ExecutionFrequency, testToolOne.Name, testToolOne.Description, testToolOne.Url, testToolOne.Structure, testToolOne.OrganizationId]);

            migrationBuilder.InsertData(
                table: "KnowledgeBases",
                columns: ["Id", "Name", "Content", "IndexName", "OrganizationId"],
                values: [Guid.Parse("9b8eb764-3088-461e-955a-e39ecc9d871b"), "Solar Knowledge Base", File.ReadAllText("D:\\Documents\\Development\\voice-agent-backend\\Static\\KnowledgeBase.txt"), "solar-index", Guid.Parse("b0b8ff1b-1ecd-41f2-9037-547ce075df66")]);

            migrationBuilder.InsertData(
                table: "AgentTools",
                columns: ["Id", "AgentId", "ToolId"],
                values: [Guid.Parse("06577065-b736-42c0-9f15-60f6b4dfaba5"), Guid.Parse("60405beb-f3c5-4d35-ab06-a36b5e57671d"), testToolOne.Id]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AgentTools",
                keyColumn: "Id",
                keyValue: Guid.Parse("06577065-b736-42c0-9f15-60f6b4dfaba5"));

            migrationBuilder.DeleteData(
                table: "Tools",
                keyColumn: "Id",
                keyValue: Guid.Parse("04c87cfe-d241-459b-81fd-0e50ed34889f"));

            migrationBuilder.DeleteData(
                table: "Agents",
                keyColumn: "Id",
                keyValue: Guid.Parse("60405beb-f3c5-4d35-ab06-a36b5e57671d"));

            migrationBuilder.DeleteData(
                table: "Numbers",
                keyColumn: "Id",
                keyValue: Guid.Parse("c0e707d1-9179-4597-b675-495530404c17"));

            migrationBuilder.DeleteData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: Guid.Parse("b0b8ff1b-1ecd-41f2-9037-547ce075df66"));
        }
    }
}