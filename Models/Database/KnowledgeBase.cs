namespace LumenicBackend.Models.Database
{
    public class KnowledgeBase
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
        public string IndexName { get; set; }
        public Guid OrganizationId { get; set; }

        [ForeignKey("OrganizationId")]
        public Organization Organization { get; set; }
    }
}
