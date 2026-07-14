using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RagChatbot.DataAccess.EntityModels
{
    public class SubjectTerm
    {
        [Key]
        public int Id { get; set; }

        public int AppUserId { get; set; }
        public int SubjectId { get; set; }

        public DateTime StartAt { get; set; }
        public DateTime? EndAt { get; set; }

        [ForeignKey("AppUserId")]
        public AppUser? AppUser { get; set; }

        [ForeignKey("SubjectId")]
        public Subject? Subject { get; set; }
    }
}
