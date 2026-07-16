using RagChatbot.Business.Interfaces;
using RagChatbot.Business.DTOs;
using RagChatbot.Business.Mappings;
using RagChatbot.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace RagChatbot.Business.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly ISubjectRepository _subjectRepository;
        private readonly ISubjectTermRepository _subjectTermRepository;
        private readonly IDocumentRepository _documentRepository;

        public SubjectService(ISubjectRepository subjectRepository, ISubjectTermRepository subjectTermRepository, IDocumentRepository documentRepository)
        {
            _subjectRepository = subjectRepository;
            _subjectTermRepository = subjectTermRepository;
            _documentRepository = documentRepository;
        }

        public async Task<SubjectDto?> GetByIdAsync(int id)
        {
            var entity = await _subjectRepository.Query()
                .Include(s => s.Documents)
                .Include(s => s.Department)
                    .ThenInclude(d => d.Users) // SỬA LỖI: Nạp thêm danh sách User của Bộ môn
                .Include(s => s.Lecturer)
                .FirstOrDefaultAsync(s => s.Id == id);
            return entity.ToDto();
        }

        public async Task<IEnumerable<SubjectDto>> GetAllByUserIdAsync(int userId)
        {
            var entities = await _subjectRepository.Query()
                .Include(s => s.Documents)
                .Include(s => s.Department)
                    .ThenInclude(d => d.Users)
                .Where(s => s.Department != null && s.Department.Users.Any(u => u.Id == userId))
                .ToListAsync();
            return entities.Select(s => s.ToDto()!).ToList();
        }

        public async Task<IEnumerable<SubjectDto>> GetByDepartmentIdAsync(int departmentId)
        {
            var entities = await _subjectRepository.Query()
                .Include(s => s.Documents)
                .Include(s => s.Department).ThenInclude(d => d.Users)
                .Include(s => s.Lecturer)
                .Where(s => s.DepartmentId == departmentId)
                .ToListAsync();
            return entities.Select(s => s.ToDto()!).ToList();
        }

        public async Task<IEnumerable<SubjectDto>> GetAllAsync()
        {
            var entities = await _subjectRepository.Query()
                .Include(s => s.Documents)
                .Include(s => s.Department).ThenInclude(d => d.Users)
                .ToListAsync();
            return entities.Select(s => s.ToDto()!).ToList();
        }

        public async Task ToggleStatusAsync(int id)
        {
            var entity = await _subjectRepository.GetByIdAsync(id);
            if (entity != null)
            {
                entity.IsActive = !entity.IsActive; // Đảo trạng thái true -> false hoặc false -> true
                _subjectRepository.Update(entity);
                await _subjectRepository.SaveChangesAsync();
            }
        }

        public async Task<SubjectDto> AddAsync(CreateSubjectDto dto)
        {
            var entity = dto.ToEntity();
            entity.IsActive = true; // Đảm bảo mặc định khi tạo mới môn học sẽ ở trạng thái "Đang mở"
            await _subjectRepository.AddAsync(entity);
            await _subjectRepository.SaveChangesAsync();
            return entity.ToDto()!;
        }

        public async Task UpdateAsync(SubjectDto subjectDto)
        {
            var entity = await _subjectRepository.GetByIdAsync(subjectDto.Id);
            if (entity != null)
            {
                entity.Name = subjectDto.Name;
                entity.Code = subjectDto.Code;
                _subjectRepository.Update(entity);
                await _subjectRepository.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            var subject = await _subjectRepository.GetByIdAsync(id);
            if (subject != null)
            {
                _subjectRepository.Remove(subject);
                await _subjectRepository.SaveChangesAsync();
            }
        }

        public async Task<bool> AssignLecturerAsync(int subjectId, int? lecturerId)
        {
            var result = await AssignLecturerAsync(subjectId, lecturerId, false);
            return result.Success;
        }

        public async Task<LecturerAssignmentResultDto> AssignLecturerAsync(
            int subjectId,
            int? lecturerId,
            bool removeCurrentSubjectDocuments)
        {
            var entity = await _subjectRepository.GetByIdAsync(subjectId);
            if (entity == null) return new LecturerAssignmentResultDto { Success = false };

            var result = new LecturerAssignmentResultDto
            {
                SubjectId = entity.Id,
                SubjectCode = entity.Code,
                SubjectName = entity.Name,
                Success = true,
                Changed = entity.LecturerId != lecturerId,
                PreviousLecturerId = entity.LecturerId,
                NewLecturerId = lecturerId,
                RemoveCurrentSubjectDocumentsRequested = removeCurrentSubjectDocuments
                    && entity.LecturerId.HasValue
                    && lecturerId.HasValue
                    && entity.LecturerId != lecturerId
            };

            if (!result.Changed) return result;

            var activeTerm = await _subjectTermRepository.Query()
                .FirstOrDefaultAsync(t => t.SubjectId == subjectId && t.EndAt == null);
            if (activeTerm != null)
            {
                activeTerm.EndAt = DateTime.UtcNow;
                _subjectTermRepository.Update(activeTerm);
            }

            if (lecturerId.HasValue)
            {
                await _subjectTermRepository.AddAsync(new RagChatbot.DataAccess.EntityModels.SubjectTerm
                {
                    SubjectId = subjectId,
                    AppUserId = lecturerId.Value,
                    StartAt = DateTime.UtcNow
                });
            }

            if (removeCurrentSubjectDocuments && result.PreviousLecturerId.HasValue && lecturerId.HasValue)
            {
                var documents = (await _documentRepository.FindAsync(d => d.SubjectId == subjectId)).ToList();
                result.DeletedDocumentIds = documents.Select(d => d.Id).ToList();
                if (documents.Count > 0) _documentRepository.RemoveRange(documents);
            }

            entity.LecturerId = lecturerId;
            _subjectRepository.Update(entity);
            await _subjectRepository.SaveChangesAsync();
            return result;
        }

        public async Task<LecturerBatchAssignmentResultDto> AssignLecturersAsync(
            IEnumerable<LecturerAssignmentRequestDto> assignments)
        {
            var requests = assignments?.ToList() ?? new List<LecturerAssignmentRequestDto>();
            var result = new LecturerBatchAssignmentResultDto();

            if (requests.Count == 0)
            {
                result.Success = true;
                return result;
            }

            if (requests.GroupBy(request => request.SubjectId).Any(group => group.Count() > 1))
            {
                result.ErrorMessage = "Danh sách phân công chứa môn học bị trùng.";
                return result;
            }

            var subjectIds = requests.Select(request => request.SubjectId).ToList();
            var subjects = await _subjectRepository.Query()
                .Where(subject => subjectIds.Contains(subject.Id))
                .ToListAsync();

            if (subjects.Count != requests.Count)
            {
                result.ErrorMessage = "Không tìm thấy một hoặc nhiều môn học cần phân công.";
                return result;
            }

            var subjectsById = subjects.ToDictionary(subject => subject.Id);
            if (requests.Any(request => subjectsById[request.SubjectId].LecturerId != request.OriginalLecturerId))
            {
                result.ErrorMessage = "Dữ liệu phân công đã được thay đổi. Vui lòng tải lại trang và thử lại.";
                return result;
            }

            var changedRequests = requests
                .Where(request => request.OriginalLecturerId != request.LecturerId)
                .ToList();
            result.UnchangedCount = requests.Count - changedRequests.Count;

            if (changedRequests.Count == 0)
            {
                result.Success = true;
                return result;
            }

            var changedSubjectIds = changedRequests.Select(request => request.SubjectId).ToList();
            var activeTerms = await _subjectTermRepository.Query()
                .Where(term => changedSubjectIds.Contains(term.SubjectId) && term.EndAt == null)
                .ToListAsync();
            var cleanupSubjectIds = changedRequests
                .Where(request => request.RemoveCurrentSubjectDocuments
                    && request.OriginalLecturerId.HasValue
                    && request.LecturerId.HasValue)
                .Select(request => request.SubjectId)
                .ToHashSet();
            var documents = cleanupSubjectIds.Count == 0
                ? new List<RagChatbot.DataAccess.EntityModels.Document>()
                : await _documentRepository.Query()
                    .Where(document => cleanupSubjectIds.Contains(document.SubjectId))
                    .ToListAsync();
            var documentsBySubjectId = documents
                .GroupBy(document => document.SubjectId)
                .ToDictionary(group => group.Key, group => group.ToList());
            var changedAt = DateTime.UtcNow;

            foreach (var request in changedRequests)
            {
                var subject = subjectsById[request.SubjectId];
                var assignmentResult = new LecturerAssignmentResultDto
                {
                    SubjectId = subject.Id,
                    SubjectCode = subject.Code,
                    SubjectName = subject.Name,
                    Success = true,
                    Changed = true,
                    PreviousLecturerId = subject.LecturerId,
                    NewLecturerId = request.LecturerId,
                    RemoveCurrentSubjectDocumentsRequested = cleanupSubjectIds.Contains(subject.Id)
                };

                var activeTerm = activeTerms.FirstOrDefault(term => term.SubjectId == subject.Id);
                if (activeTerm != null)
                {
                    activeTerm.EndAt = changedAt;
                    _subjectTermRepository.Update(activeTerm);
                }

                if (request.LecturerId.HasValue)
                {
                    await _subjectTermRepository.AddAsync(new RagChatbot.DataAccess.EntityModels.SubjectTerm
                    {
                        SubjectId = subject.Id,
                        AppUserId = request.LecturerId.Value,
                        StartAt = changedAt
                    });
                }

                if (documentsBySubjectId.TryGetValue(subject.Id, out var subjectDocuments))
                {
                    assignmentResult.DeletedDocumentIds = subjectDocuments.Select(document => document.Id).ToList();
                    _documentRepository.RemoveRange(subjectDocuments);
                }

                subject.LecturerId = request.LecturerId;
                _subjectRepository.Update(subject);
                result.Assignments.Add(assignmentResult);
            }

            await _subjectRepository.SaveChangesAsync();
            result.Success = true;
            return result;
        }

        public async Task<IEnumerable<SubjectTermDto>> GetSubjectTermHistoryAsync(int subjectId)
        {
            var terms = await _subjectTermRepository.Query()
                .Include(t => t.AppUser)
                .Where(t => t.SubjectId == subjectId)
                .OrderByDescending(t => t.StartAt)
                .ToListAsync();

            return terms.Select(t => new SubjectTermDto
            {
                LecturerName = t.AppUser != null ? $"{t.AppUser.LastName} {t.AppUser.FirstName}".Trim() : "Không rõ",
                StartAt = t.StartAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                EndAt = t.EndAt.HasValue ? t.EndAt.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : null
            });
        }

        public async Task<bool> ExistsByCodeAsync(string code, int? excludeId = null)
        {
            var query = _subjectRepository.Query().Where(s => s.Code == code);
            if (excludeId.HasValue)
            {
                query = query.Where(s => s.Id != excludeId.Value);
            }
            return await query.AnyAsync();
        }
    }
}
