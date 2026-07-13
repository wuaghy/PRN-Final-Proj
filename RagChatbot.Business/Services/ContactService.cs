using Microsoft.EntityFrameworkCore;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RagChatbot.Business.Services
{
    public class ContactService : IContactService
    {
        private readonly IContactMessageRepository _contactMessageRepository;

        public ContactService(IContactMessageRepository contactMessageRepository)
        {
            _contactMessageRepository = contactMessageRepository;
        }

        public async Task<IEnumerable<ContactMessageDto>> GetAllContactMessagesAsync()
        {
            var messages = await _contactMessageRepository.Query()
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
            return messages.Select(MapToDto);
        }

        public async Task<int> GetPendingCountAsync()
        {
            return await _contactMessageRepository.Query().CountAsync(c => c.Status == ContactStatus.Pending);
        }

        public async Task<ContactMessageDto?> GetByIdAsync(int id)
        {
            var message = await _contactMessageRepository.Query()
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == id);
            return message == null ? null : MapToDto(message);
        }

        public async Task AddContactMessageAsync(ContactMessageDto contactDto)
        {
            var message = new ContactMessage
            {
                UserId = contactDto.UserId,
                Content = contactDto.Content,
                Type = System.Enum.Parse<ContactType>(contactDto.Type),
                Status = ContactStatus.Pending,
                RelatedId = contactDto.RelatedId,
                CreatedAt = System.DateTime.UtcNow
            };
            await _contactMessageRepository.AddAsync(message);
            await _contactMessageRepository.SaveChangesAsync();
        }

        public async Task UpdateContactStatusAsync(int id, string status)
        {
            var message = await _contactMessageRepository.GetByIdAsync(id);
            if (message != null)
            {
                message.Status = System.Enum.Parse<ContactStatus>(status);
                _contactMessageRepository.Update(message);
                await _contactMessageRepository.SaveChangesAsync();
            }
        }

        public async Task DeleteContactMessageAsync(int id)
        {
            var message = await _contactMessageRepository.GetByIdAsync(id);
            if (message != null)
            {
                _contactMessageRepository.Remove(message);
                await _contactMessageRepository.SaveChangesAsync();
            }
        }

        private ContactMessageDto MapToDto(ContactMessage msg)
        {
            return new ContactMessageDto
            {
                Id = msg.Id,
                UserId = msg.UserId,
                Content = msg.Content,
                Type = msg.Type.ToString(),
                Status = msg.Status.ToString(),
                RelatedId = msg.RelatedId,
                CreatedAt = msg.CreatedAt,
                User = msg.User == null ? null : new AppUserDto
                {
                    Id = msg.User.Id,
                    Email = msg.User.Email,
                    FirstName = msg.User.FirstName,
                    LastName = msg.User.LastName
                }
            };
        }
    }
}
