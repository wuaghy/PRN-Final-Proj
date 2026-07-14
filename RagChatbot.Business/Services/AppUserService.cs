using System.Security.Cryptography;
using System.Text;
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
    public class AppUserService : IAppUserService
    {
        private readonly IAppUserRepository _appUserRepository;
        private readonly IHodTermRepository _hodTermRepository;
        private readonly IChatMessageRepository _chatMessageRepository;
        private readonly ISettingService _settingService;

        public AppUserService(
            IAppUserRepository appUserRepository,
            IHodTermRepository hodTermRepository,
            IChatMessageRepository chatMessageRepository,
            ISettingService settingService)
        {
            _appUserRepository = appUserRepository;
            _hodTermRepository = hodTermRepository;
            _chatMessageRepository = chatMessageRepository;
            _settingService = settingService;
        }

        public async Task<AppUserDto?> GetByIdAsync(int id)
        {
            var user = await _appUserRepository.Query()
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == id);
            return user == null ? null : MapToDto(user);
        }

        public async Task<AppUserDto?> GetUserTokenStatsAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            if (user == null) return null;

            var priceConfig = await _settingService.GetPricingConfigAsync();
            decimal defaultInRate = priceConfig.TokenInCostPerMillion;
            decimal defaultOutRate = priceConfig.TokenOutCostPerMillion;
            decimal defaultUsdRate = priceConfig.UsdVndRate;

            var stats = await _chatMessageRepository.Query()
                .Where(m => m.Session != null && m.Session.UserId == userId)
                .GroupBy(m => m.Session.UserId)
                .Select(g => new {
                    MsgCount = g.Count(),
                    TokenIn = g.Sum(m => (long)(m.TokenIn ?? 0)),
                    TokenOut = g.Sum(m => (long)(m.TokenOut ?? 0)),
                    CostVnd = g.Sum(m =>
                        (((m.TokenIn ?? 0) * (m.TokenInCostPerMillion ?? defaultInRate) / 1000000m) +
                         ((m.TokenOut ?? 0) * (m.TokenOutCostPerMillion ?? defaultOutRate) / 1000000m)) *
                        (m.UsdRate ?? defaultUsdRate)
                    )
                })
                .FirstOrDefaultAsync();

            if (stats != null)
            {
                user.TokenIn = stats.TokenIn;
                user.TokenOut = stats.TokenOut;
                user.TodayChatCount = stats.MsgCount; // Reuse field for total message count in wallet view
                user.CostVnd = stats.CostVnd;
            }

            return user;
        }

        public async Task<AppUserDto?> GetByEmailAsync(string email)
        {
            var user = await _appUserRepository.Query()
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Email == email);
            return user == null ? null : MapToDto(user);
        }

        public async Task<IEnumerable<AppUserDto>> GetAllUsersAsync()
        {
            var users = await _appUserRepository.Query()
                .Include(u => u.Department)
                .ToListAsync();
            return users.Select(MapToDto);
        }

        public async Task<IEnumerable<AppUserDto>> GetUsersByRoleAsync(string role)
        {
            var users = await _appUserRepository.Query()
                .Include(u => u.Department)
                .Where(u => u.Role == role)
                .ToListAsync();
            return users.Select(MapToDto);
        }

        public async Task<IEnumerable<AppUserDto>> GetUsersAsync(string role, bool? isActive, string searchEmail)
        {
            var query = _appUserRepository.Query().IgnoreQueryFilters().Include(u => u.Department).Where(u => u.Role == role);
            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }
            if (!string.IsNullOrWhiteSpace(searchEmail))
            {
                query = query.Where(u => u.Email.Contains(searchEmail));
            }
            var users = await query.ToListAsync();
            
            var userIds = users.Select(u => u.Id).ToList();
            var priceConfig = await _settingService.GetPricingConfigAsync();
            decimal defaultInRate = priceConfig.TokenInCostPerMillion;
            decimal defaultOutRate = priceConfig.TokenOutCostPerMillion;
            decimal defaultUsdRate = priceConfig.UsdVndRate;

            var stats = await _chatMessageRepository.Query()
                .Where(m => m.Session != null && userIds.Contains(m.Session.UserId))
                .GroupBy(m => m.Session.UserId)
                .Select(g => new {
                    UserId = g.Key,
                    TokenIn = g.Sum(m => (long)(m.TokenIn ?? 0)),
                    TokenOut = g.Sum(m => (long)(m.TokenOut ?? 0)),
                    CostVnd = g.Sum(m =>
                        (((m.TokenIn ?? 0) * (m.TokenInCostPerMillion ?? defaultInRate) / 1000000m) +
                         ((m.TokenOut ?? 0) * (m.TokenOutCostPerMillion ?? defaultOutRate) / 1000000m)) *
                        (m.UsdRate ?? defaultUsdRate)
                    )
                })
                .ToDictionaryAsync(x => x.UserId, x => x);

            return users.Select(u => {
                var dto = MapToDto(u);
                if (stats.TryGetValue(u.Id, out var s))
                {
                    dto.TokenIn = s.TokenIn;
                    dto.TokenOut = s.TokenOut;
                    dto.CostVnd = s.CostVnd;
                }
                return dto;
            });
        }

        public async Task<int> GetPremiumUsersCountAsync()
        {
            return await _appUserRepository.Query().CountAsync(u => u.Subscription == AppUser.SubscriptionType.Premium);
        }

        public async Task AddUserAsync(AppUserDto userDto, string plainPassword)
        {
            var user = new AppUser
            {
                Email = userDto.Email,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Role = userDto.Role,
                IsActive = userDto.IsActive,
                DepartmentId = userDto.DepartmentId,
                PasswordHash = HashPassword(plainPassword)
            };
            await _appUserRepository.AddAsync(user);
            await _appUserRepository.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(AppUserDto userDto)
        {
            var user = await _appUserRepository.GetByIdAsync(userDto.Id);
            if (user != null)
            {
                user.FirstName = userDto.FirstName;
                user.LastName = userDto.LastName;
                user.Role = userDto.Role;
                user.IsActive = userDto.IsActive;
                user.DepartmentId = userDto.DepartmentId;
                user.Subscription = userDto.Subscription == "Premium" ? AppUser.SubscriptionType.Premium : AppUser.SubscriptionType.Free;
                user.TodayChatCount = userDto.TodayChatCount;
                user.DailyQueryCount = userDto.DailyQueryCount;
                user.LastActiveDate = userDto.LastActiveDate;
                user.LastQueryDate = userDto.LastQueryDate;

                _appUserRepository.Update(user);
                await _appUserRepository.SaveChangesAsync();
            }
        }

        public async Task<bool> VerifyAndChangePasswordAsync(string email, string oldPassword, string newPassword)
        {
            var user = await _appUserRepository.Query().FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return false;

            var hashedOld = HashPassword(oldPassword);
            if (user.PasswordHash != hashedOld) return false;

            user.PasswordHash = HashPassword(newPassword);
            _appUserRepository.Update(user);
            await _appUserRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SoftDeleteUserAsync(int id)
        {
            var user = await _appUserRepository.Query().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
            if (user != null && user.Role != "Admin")
            {
                if (user.Role == "HeadOfDepartment" && user.DepartmentId.HasValue)
                {
                    var activeTerm = await _hodTermRepository.Query().FirstOrDefaultAsync(t => t.AppUserId == user.Id && t.EndAt == null);
                    if (activeTerm != null)
                    {
                        activeTerm.EndAt = System.DateTime.UtcNow;
                        _hodTermRepository.Update(activeTerm);
                        await _hodTermRepository.SaveChangesAsync();
                    }
                    user.DepartmentId = null;
                }

                user.IsActive = false;
                _appUserRepository.Update(user);
                await _appUserRepository.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> RestoreUserAsync(int id)
        {
            var user = await _appUserRepository.Query().IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
            if (user != null)
            {
                user.IsActive = true;
                _appUserRepository.Update(user);
                await _appUserRepository.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> ResetPasswordAsync(int id, string newPassword)
        {
            var user = await _appUserRepository.GetByIdAsync(id);
            if (user != null && user.Role != "Admin")
            {
                user.PasswordHash = HashPassword(newPassword);
                _appUserRepository.Update(user);
                await _appUserRepository.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = await _appUserRepository.GetByIdAsync(id);
            if (user != null)
            {
                _appUserRepository.Remove(user);
                await _appUserRepository.SaveChangesAsync();
            }
        }

        public async Task IncrementQueryCountAsync(int id)
        {
            var user = await _appUserRepository.GetByIdAsync(id);
            if (user != null)
            {
                user.DailyQueryCount++;
                user.LastQueryDate = System.DateTime.UtcNow;
                _appUserRepository.Update(user);
                await _appUserRepository.SaveChangesAsync();
            }
        }

        public async Task ResetDailyQueryCountAsync(int id)
        {
            var user = await _appUserRepository.GetByIdAsync(id);
            if (user != null)
            {
                user.DailyQueryCount = 0;
                _appUserRepository.Update(user);
                await _appUserRepository.SaveChangesAsync();
            }
        }

        public async Task UpdateHodDepartmentAsync(int userId, int? departmentId)
        {
            var user = await _appUserRepository.GetByIdAsync(userId);
            if (user == null || user.Role != "HeadOfDepartment") return;

            if (user.DepartmentId != departmentId)
            {
                var activeTerm = await _hodTermRepository.Query().FirstOrDefaultAsync(t => t.AppUserId == user.Id && t.EndAt == null);
                if (activeTerm != null)
                {
                    activeTerm.EndAt = System.DateTime.UtcNow;
                    _hodTermRepository.Update(activeTerm);
                }

                if (departmentId.HasValue)
                {
                    await _hodTermRepository.AddAsync(new HodTerm
                    {
                        AppUserId = user.Id,
                        DepartmentId = departmentId.Value,
                        StartAt = System.DateTime.UtcNow
                    });
                }

                user.DepartmentId = departmentId;
                _appUserRepository.Update(user);
                
                await _hodTermRepository.SaveChangesAsync();
                await _appUserRepository.SaveChangesAsync();
            }
        }

        public async Task EndHodTermAsync(int userId)
        {
            var user = await _appUserRepository.GetByIdAsync(userId);
            if (user != null && user.Role == "HeadOfDepartment" && user.DepartmentId.HasValue)
            {
                var activeTerm = await _hodTermRepository.Query().FirstOrDefaultAsync(t => t.AppUserId == user.Id && t.EndAt == null);
                if (activeTerm != null)
                {
                    activeTerm.EndAt = System.DateTime.UtcNow;
                    _hodTermRepository.Update(activeTerm);
                    await _hodTermRepository.SaveChangesAsync();
                }

                user.DepartmentId = null;
                _appUserRepository.Update(user);
                await _appUserRepository.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<HodTermDto>> GetHodTermHistoryAsync(int userId)
        {
            var terms = await _hodTermRepository.Query()
                .Include(t => t.Department)
                .Where(t => t.AppUserId == userId)
                .OrderByDescending(t => t.StartAt)
                .ToListAsync();

            return terms.Select(t => new HodTermDto
            {
                DepartmentName = t.Department != null ? t.Department.Name : "Không rõ",
                StartAt = t.StartAt.ToString("dd/MM/yyyy"),
                EndAt = t.EndAt.HasValue ? t.EndAt.Value.ToString("dd/MM/yyyy") : null
            });
        }

        public async Task<IEnumerable<HodTermDto>> GetDepartmentTermHistoryAsync(int deptId)
        {
            var terms = await _hodTermRepository.Query()
                .Include(t => t.AppUser)
                .Where(t => t.DepartmentId == deptId)
                .OrderByDescending(t => t.StartAt)
                .ToListAsync();

            return terms.Select(t => new HodTermDto
            {
                HodName = t.AppUser != null ? (t.AppUser.LastName + " " + t.AppUser.FirstName).Trim() : "Không rõ",
                StartAt = t.StartAt.ToString("dd/MM/yyyy"),
                EndAt = t.EndAt.HasValue ? t.EndAt.Value.ToString("dd/MM/yyyy") : null
            });
        }

        public async Task<bool> HasDepartmentHodAsync(int departmentId, int? excludeUserId = null)
        {
            var query = _appUserRepository.Query().Where(u => u.Role == "HeadOfDepartment" && u.DepartmentId == departmentId);
            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }
            return await query.AnyAsync();
        }

        private AppUserDto MapToDto(AppUser user)
        {
            return new AppUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                IsActive = user.IsActive,
                DepartmentId = user.DepartmentId,
                DailyQueryCount = user.DailyQueryCount,
                LastQueryDate = user.LastQueryDate,
                Subscription = user.Subscription.ToString(),
                TodayChatCount = user.TodayChatCount,
                LastActiveDate = user.LastActiveDate,
                Department = user.Department == null ? null : new DepartmentDto
                {
                    Id = user.Department.Id,
                    Name = user.Department.Name,
                    Description = user.Department.Description
                }
            };
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return System.Convert.ToBase64String(hash);
        }
    }
}
