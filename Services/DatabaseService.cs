using LumenicBackend.Models.Database;
using Pipelines.Sockets.Unofficial.Arenas;

namespace LumenicBackend.Services
{
    public class DatabaseService
    {
        private readonly ILogger logger;
        private readonly LumenicDbContext Context;
        public DatabaseService(LumenicDbContext context, ILogger<DatabaseService> logger)
        {
            this.Context = context;
            this.logger = logger;
        }

        public async Task<Organization?> GetOrganization(string organizationId)
        {
            return await Context.Organizations.FindAsync(organizationId);
        }

        public async Task<List<Organization>> GetAllOrganizations()
        {
            return Context.Organizations.ToList();
        }

        public async Task<List<Organization>> GetAllOrganizationsByName(string organizationName)
        {
            return Context.Organizations.Where(x => x.Name == organizationName).ToList();
        }

        public async Task<List<Organization>> GetAllOrganizationsByEmail(string email)
        {
            return Context.Organizations.Where(x => x.Email == email).ToList();
        }

        public async Task<List<Organization>> GetAllActiveOrganizationsByName(string organizationName)
        {
            return Context.Organizations.Where(x => x.Name == organizationName && x.Active).ToList();
        }

        public async Task AddOrganization(Organization organization)
        {
            await Context.Organizations.AddAsync(organization);
            await Context.SaveChangesAsync();
        }

        public async Task UpdateOrganization(Organization organization)
        {
            Context.Organizations.Update(organization);
            await Context.SaveChangesAsync();
        }

        public async Task DeleteOrganization(string organizationId)
        {
            try
            {
                var organization = new Organization { Id = Guid.Parse(organizationId) };
                Context.Organizations.Attach(organization);
                Context.Organizations.Remove(organization);
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // This exception is thrown if the entity doesn't exist in the database.
                // Handle it here if you need to.
            }
            catch (Exception)
            {
                // This will catch any other exceptions.
                // Log the exception, rethrow it, or handle it as appropriate.
                throw;
            }
        }

        public async Task<Number?> GetNumber(string numberId)
        {
            return await Context.Numbers.FindAsync(numberId);
        }

        public async Task<List<Number>> GetAllNumbers()
        {
            return Context.Numbers.ToList();
        }

        public async Task AddNumber(Number number)
        {
            await Context.Numbers.AddAsync(number);
            await Context.SaveChangesAsync();
        }

        public async Task UpdateNumber(Number number)
        {
            Context.Numbers.Update(number);
            await Context.SaveChangesAsync();
        }

        public async Task DeleteNumberById(string numberId)
        {
            try
            {
                var number = new Number { Id = Guid.Parse(numberId) };
                Context.Numbers.Attach(number);
                Context.Numbers.Remove(number);
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // This exception is thrown if the entity doesn't exist in the database.
                // Handle it here if you need to.
            }
            catch (Exception)
            {
                // This will catch any other exceptions.
                // Log the exception, rethrow it, or handle it as appropriate.
                throw;
            }
        }

        public async Task DeleteNumber(string number)
        {
            await Context.Numbers.Where(n => n.NumberValue == number).ExecuteDeleteAsync();
        }

        public List<Number> GetNumbersByOrganizationId(string organizationId)
        {
            return Context.Numbers.Where(n => n.OrganizationId == Guid.Parse(organizationId)).ToList();
        }

        public async Task<Agent?> GetAgent(string agentId)
        {
            return await Context.Agents.FindAsync(agentId);
        }

        // Create the rest of the methods here
        public async Task AddAgent(Agent agent)
        {
            await Context.Agents.AddAsync(agent);
            await Context.SaveChangesAsync();
        }

        public async Task UpdateAgent(Agent agent)
        {
            Context.Agents.Update(agent);
            await Context.SaveChangesAsync();
        }

        public async Task DeleteAgent(string agentId)
        {
            try
            {
                var agent = new Agent { Id = Guid.Parse(agentId) };
                Context.Agents.Attach(agent);
                Context.Agents.Remove(agent);
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // This exception is thrown if the entity doesn't exist in the database.
                // Handle it here if you need to.
            }
            catch (Exception)
            {
                // This will catch any other exceptions.
                // Log the exception, rethrow it, or handle it as appropriate.
                throw;
            }
        }

        public List<Agent> GetAgentsByOrganizationId(string organizationId)
        {
            return Context.Agents.Where(a => a.OrganizationId == Guid.Parse(organizationId)).ToList();
        }

        public (Agent?, List<Tool>?) GetAgentWithTools(string agentId)
        {
            var agent = Context.Agents.Include(a => a.AgentTools)
                            .ThenInclude(at => at.Tool)
                            .FirstOrDefault(a => a.Id == Guid.Parse(agentId));
            var tools = agent?.AgentTools.Select(at => at.Tool).ToList();
            return (agent, tools);
        }
        
        public async Task<Tool?> GetTool(string toolId)
        {
            return await Context.Tools.FindAsync(toolId);
        }

        public async Task AddTool(Tool tool)
        {
            await Context.Tools.AddAsync(tool);
            await Context.SaveChangesAsync();
        }

        public async Task UpdateTool(Tool tool)
        {
            Context.Tools.Update(tool);
            await Context.SaveChangesAsync();
        }

        public async Task DeleteTool(string toolId)
        {
            try
            {
                var tool = new Tool { Id = Guid.Parse(toolId) };
                Context.Tools.Attach(tool);
                Context.Tools.Remove(tool);
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // This exception is thrown if the entity doesn't exist in the database.
                // Handle it here if you need to.
            }
            catch (Exception)
            {
                // This will catch any other exceptions.
                // Log the exception, rethrow it, or handle it as appropriate.
                throw;
            }
        }

        public async Task<List<Tool>> GetToolsByOrganizationId(string organizationId)
        {
            return Context.Tools.Where(t => t.OrganizationId == Guid.Parse(organizationId)).ToList();
        }

        public async Task<List<Tool>> GetToolsByAgentId(string agentId)
        {
            var tools = Context.AgentTools.Where(at => at.AgentId == Guid.Parse(agentId)).Select(at => at.Tool).ToList();
            return tools;
        }

        public async Task AddToolToAgent(string agentId, string toolId)
        {
            var agentTool = new AgentTool
            {
                AgentId = Guid.Parse(agentId),
                ToolId = Guid.Parse(toolId)
            };
            Context.AgentTools.Add(agentTool);
            await Context.SaveChangesAsync();
        }

        public async Task AddToolsToAgent(string agentId, List<string> toolIds)
        {
            var agentTools = new List<AgentTool>();

            foreach (var toolId in toolIds)
            {
                agentTools.Add(new AgentTool()
                {
                    AgentId = Guid.Parse(agentId),
                    ToolId = Guid.Parse(toolId),
                });
            }

            await Context.AgentTools.AddRangeAsync(agentTools);
        }

        public async Task RemoveToolFromAgent(string agentId, string toolId)
        {
            var agentTool = new AgentTool
            {
                AgentId = Guid.Parse(agentId),
                ToolId = Guid.Parse(toolId)
            };
            Context.AgentTools.Attach(agentTool);
            Context.AgentTools.Remove(agentTool);
            await Context.SaveChangesAsync();
        }

        public async Task<List<KnowledgeBase>> GetKnowledgeBasesByOrganizationId(string organizationId)
        {
            return Context.KnowledgeBases.Where(a => a.OrganizationId == Guid.Parse(organizationId)).ToList();
        }

        public async Task AddKnowledgeBase(KnowledgeBase knowledgeBase)
        {
            await Context.KnowledgeBases.AddAsync(knowledgeBase);
            await Context.SaveChangesAsync();
        }

        public async Task UpdateKnowledgeBase(KnowledgeBase knowledgeBase)
        {
            Context.KnowledgeBases.Update(knowledgeBase);
            await Context.SaveChangesAsync();
        }

        public async Task DeleteKnowledgeBase(string knowledgeBaseId)
        {
            try
            {
                var kb = new KnowledgeBase { Id = Guid.Parse(knowledgeBaseId) };
                Context.KnowledgeBases.Attach(kb);
                Context.KnowledgeBases.Remove(kb);
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // This exception is thrown if the entity doesn't exist in the database.
                // Handle it here if you need to.
            }
            catch (Exception)
            {
                // This will catch any other exceptions.
                // Log the exception, rethrow it, or handle it as appropriate.
                throw;
            }
        }

        public async Task DeleteKnowledgeBaseByIndex(string index, string organizationId)
        {
            await Context.KnowledgeBases.Where(kb => kb.IndexName == index && kb.OrganizationId == Guid.Parse(organizationId)).ExecuteDeleteAsync();
        }

        public async Task<CallLedger?> GetCallLedger(int callId)
        {
            return await Context.CallLedgers.FindAsync(callId);
        }

        public async Task AddCallLedger(CallLedger callLedger)
        {
            Context.CallLedgers.Add(callLedger);
            await Context.SaveChangesAsync();
        }

        public async Task UpdateCallLedger(CallLedger callLedger)
        {
            Context.CallLedgers.Update(callLedger);
            await Context.SaveChangesAsync();
        }

        public async Task CreateOrUpdateCallLedger(CallLedger callLedger)
        {
            var existingCallLedger = await Context.CallLedgers.FindAsync(callLedger.Id);

            if (existingCallLedger != null)
            {
                // Update existing call ledger
                Context.Entry(existingCallLedger).CurrentValues.SetValues(callLedger);
            }
            else
            {
                // Add new call ledger
                Context.CallLedgers.Add(callLedger);
            }

            await Context.SaveChangesAsync();
        }

        public async Task DeleteCallLedger(string callId)
        {
            try
            {
                var callLedger = new CallLedger { Id = Guid.Parse(callId) };
                Context.CallLedgers.Attach(callLedger);
                Context.CallLedgers.Remove(callLedger);
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // This exception is thrown if the entity doesn't exist in the database.
                // Handle it here if you need to.
            }
            catch (Exception)
            {
                // This will catch any other exceptions.
                // Log the exception, rethrow it, or handle it as appropriate.
                throw;
            }
        }

        public async Task<List<CallLedger>> GetCallLedgersByOrganizationId(string organizationId)
        {
            return Context.CallLedgers.Where(cl => cl.OrganizationId == Guid.Parse(organizationId)).ToList();
        }

        public CallLedger? GetCallLedgerByThreadId(string threadId)
        {
            return Context.CallLedgers.FirstOrDefault(cl => cl.ThreadId == threadId);
        }

        public CustomOperationContext? InitializeCustomOperationContext(
            string threadId,
            string botNumber,
            string botUserId,
            string botToken,
            string userNumber,
            string userId,
            string userToken,
            string direction)
        {
            var numberValue = Context.Numbers.FirstOrDefault(n => n.NumberValue == botNumber);

            if (numberValue == null || !numberValue.Active) {
                logger.LogError("Number {} not found or not active", botNumber);
                return null;
            }

            var agent = Context.Agents.FirstOrDefault(a => a.PhoneNumber == botNumber);

            if (agent == null) {
                logger.LogError("Agent with phone number {} not found", botNumber);
                return null;
            }

            var tools = Context.AgentTools.Where(at => at.AgentId == agent.Id).Select(at => at.Tool).ToList();

            var utcNow = DateTimeOffset.UtcNow;

            return new CustomOperationContext
            {
                CallConnectionId = string.Empty,
                ServerCallId = string.Empty,
                CallLedgerId = Guid.NewGuid().ToString(),
                ThreadId = threadId,
                OrganizationId = agent.OrganizationId.ToString(),
                AgentId = agent.Id.ToString(),
                BotUserId = botUserId,
                BotNumber = botNumber,
                BotToken = botToken,
                UserId = userId,
                UserNumber = userNumber,
                UserToken = userToken,
                Greeting = agent.Greeting,
                VoiceModel = agent.VoiceModel,
                Name = agent.Name,
                Description = agent.Description,
                Topic = "Bot Conversation",
                SystemTemplate = agent.Instructions,
                SearchIndex = agent.SearchIndex,
                Recorded = true,
                RecordingId = string.Empty,
                Direction = direction,
                CallEndReason = CallEndReason.Unknown.ToString().ToLowerInvariant(),
                TransferNumber = numberValue.TransferNumber,
                TransferWeight = numberValue.TransferWeight,
                StartHour = numberValue.StartHour,
                EndHour = numberValue.EndHour,
                TimeZone = numberValue.TimeZone,
                StartDateTime = utcNow.ToUnixTimeSeconds(),
                SpeechToTextStartTime = utcNow.ToUnixTimeSeconds(),
                ResourceUsage = ResourceUsageHelper.InitializeEmpty(),
                Tools = tools,
            };
        }
    }
}
