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
    public class DepartmentService : IDepartmentService
    {
        private readonly IDepartmentRepository _departmentRepository;

        public DepartmentService(IDepartmentRepository departmentRepository)
        {
            _departmentRepository = departmentRepository;
        }

        public async Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync()
        {
            var departments = await _departmentRepository.GetAllAsync();
            return departments.Select(MapToDto);
        }

        public async Task<DepartmentDto?> GetByIdAsync(int id)
        {
            var department = await _departmentRepository.GetByIdAsync(id);
            return department == null ? null : MapToDto(department);
        }

        public async Task<DepartmentDto> AddDepartmentAsync(DepartmentDto departmentDto)
        {
            var department = new Department
            {
                Name = departmentDto.Name,
                Description = departmentDto.Description,
                IsActive = departmentDto.IsActive,
                CreatedAt = System.DateTime.UtcNow
            };
            await _departmentRepository.AddAsync(department);
            await _departmentRepository.SaveChangesAsync();
            return MapToDto(department);
        }

        public async Task UpdateDepartmentAsync(DepartmentDto departmentDto)
        {
            var department = await _departmentRepository.GetByIdAsync(departmentDto.Id);
            if (department != null)
            {
                department.Name = departmentDto.Name;
                department.Description = departmentDto.Description;
                department.IsActive = departmentDto.IsActive;
                _departmentRepository.Update(department);
                await _departmentRepository.SaveChangesAsync();
            }
        }

        public async Task DeleteDepartmentAsync(int id)
        {
            var department = await _departmentRepository.GetByIdAsync(id);
            if (department != null)
            {
                _departmentRepository.Remove(department);
                await _departmentRepository.SaveChangesAsync();
            }
        }

        private DepartmentDto MapToDto(Department dept)
        {
            return new DepartmentDto
            {
                Id = dept.Id,
                Name = dept.Name,
                Description = dept.Description,
                IsActive = dept.IsActive,
                CreatedAt = dept.CreatedAt
            };
        }
    }
}
