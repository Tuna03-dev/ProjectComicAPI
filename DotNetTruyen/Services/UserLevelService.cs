using DotNetTruyen.Data;
using DotNetTruyen.Models;
using DotNetTruyen.ViewModels.Management;
using Microsoft.EntityFrameworkCore;

namespace DotNetTruyen.Services
{
    public class UserLevelService
    {
        private readonly ILogger<UserService> _logger;
        private readonly DotNetTruyenDbContext _context;

        public UserLevelService(ILogger<UserService> logger, DotNetTruyenDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<UserLevelViewModel> GetUserLevelInfoAsync( Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Level)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found in GetUserLevelInfoAsync.", userId);
                throw new Exception("User not found.");
            }

            if (user.Level == null)
            {
                _logger.LogInformation("User {UserId} has no level in GetUserLevelInfoAsync, creating Level 0.", userId);
                user.Level = await GetLevel0Async();
                user.LevelId = user.Level.Id;
                await _context.SaveChangesAsync();
            }

            var levels = await _context.Levels
                .AsNoTracking()
                .Where(l => l.DeletedAt == null)
                .OrderBy(l => l.LevelNumber)
                .Select(l => new Level
                {
                    Id = l.Id,
                    LevelNumber = l.LevelNumber,
                    Name = l.Name,
                    ExpRequired = l.ExpRequired
                })
                .ToListAsync();

            var nextLevel = levels
                .Where(l => l.LevelNumber > user.Level.LevelNumber)
                .OrderBy(l => l.LevelNumber)
                .FirstOrDefault();

            int expRequiredForNextLevel = nextLevel?.ExpRequired ?? user.Level.ExpRequired;
            int expToNextLevel = expRequiredForNextLevel - user.Exp;
            double progressPercentage = nextLevel != null
                ? (double)(user.Exp - user.Level.ExpRequired) / (nextLevel.ExpRequired - user.Level.ExpRequired) * 100
                : 100;

            return new UserLevelViewModel
            {
                LevelNumber = user.Level.LevelNumber,
                LevelName = user.Level.Name ?? "Unknown Level",
                CurrentExp = user.Exp,
                ExpRequiredForNextLevel = expRequiredForNextLevel,
                ExpToNextLevel = expToNextLevel > 0 ? expToNextLevel : 0,
                ProgressPercentage = progressPercentage > 0 ? progressPercentage : 0
            };
        }

        private async Task<Level> GetLevel0Async()
        {
            var level = await _context.Levels
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LevelNumber == 0 && l.DeletedAt == null);

            if (level == null)
            {
                throw new Exception("Level 0 not found in the database. Please ensure it exists.");
            }

            return level;
        }
    }
}
