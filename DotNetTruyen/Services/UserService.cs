using DotNetTruyen.Data;
using DotNetTruyen.Models;
using DotNetTruyen.ViewModels.Management;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using DotNetTruyen.Hubs;

namespace DotNetTruyen.Services
{
    public class UserService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<UserService> _logger;

        public UserService(IHubContext<NotificationHub> hubContext, ILogger<UserService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task IncreaseExpAsync(DotNetTruyenDbContext context, Guid userId, int expToAdd = 10)
        {
            _logger.LogInformation("Starting IncreaseExpAsync for User {UserId} with expToAdd {ExpToAdd}.", userId, expToAdd);

            var user = await context.Users
                .Include(u => u.Level)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found.", userId);
                throw new Exception("User not found.");
            }

            if (user.Level == null)
            {
                _logger.LogInformation("User {UserId} has no level, creating Level 0.", userId);
                user.Level = await GetLevel0Async(context);
                user.LevelId = user.Level.Id;
                await context.SaveChangesAsync();
                _logger.LogInformation("Level 0 created and saved for User {UserId}.", userId);
            }

            var oldLevelId = user.LevelId;
            var oldExp = user.Exp;

            user.Exp += expToAdd;
            _logger.LogInformation("User {UserId}: Exp increased from {OldExp} to {NewExp}.", userId, oldExp, user.Exp);

            try
            {
                _logger.LogDebug("Updating level for User {UserId} with Exp {Exp}.", userId, user.Exp);
                var level = await context.Levels
                    .AsNoTracking()
                    .Where(l => l.DeletedAt == null && l.ExpRequired <= user.Exp)
                    .OrderByDescending(l => l.LevelNumber)
                    .Select(l => new Level
                    {
                        Id = l.Id,
                        LevelNumber = l.LevelNumber,
                        Name = l.Name,
                        ExpRequired = l.ExpRequired
                    })
                    .FirstOrDefaultAsync();

                if (level == null)
                {
                    _logger.LogWarning("No level found for User {UserId} with Exp {Exp}, falling back to Level 0.", userId, user.Exp);
                    level = await GetLevel0Async(context);
                }

                user.LevelId = level.Id;
                _logger.LogInformation("User {UserId}: LevelId updated from {OldLevelId} to {NewLevelId}.", userId, oldLevelId, user.LevelId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User {UserId}: Error occurred while updating level.", userId);
                throw;
            }

            try
            {
                _logger.LogDebug("Calling SaveChangesAsync for User {UserId}.", userId);
                await context.SaveChangesAsync();
                _logger.LogInformation("User {UserId}: Changes saved successfully. New Exp: {NewExp}", userId, user.Exp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User {UserId}: Failed to save changes.", userId);
                throw;
            }

            if (user.LevelId != oldLevelId)
            {
                _logger.LogInformation("User {UserId} leveled up. Processing notifications.", userId);
                var newLevel = await context.Levels
                    .AsNoTracking()
                    .Where(l => l.Id == user.LevelId)
                    .FirstOrDefaultAsync();

                var levels = await context.Levels
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
                    .Where(l => l.LevelNumber > newLevel.LevelNumber)
                    .OrderBy(l => l.LevelNumber)
                    .FirstOrDefault();

                int expRequiredForNextLevel = nextLevel?.ExpRequired ?? newLevel.ExpRequired;

                var userNotification = new Notification
                {
                    Id = Guid.NewGuid(),
                    Title = "Chúc mừng lên cấp!",
                    Message = $"Bạn đã lên cấp {newLevel.LevelNumber} - {newLevel.Name}!",
                    Type = "success",
                    Icon = "star",
                    Link = "/userProfile",
                    IsRead = false,
                    UserId = userId
                };
                context.Notifications.Add(userNotification);
                await context.SaveChangesAsync();
                _logger.LogInformation("User {UserId}: Notification added for level up to {LevelNumber}.", userId, newLevel.LevelNumber);

                await _hubContext.Clients.Group("Admins").SendAsync("ReceiveNotification", new
                {
                    id = userNotification.Id.ToString(),
                    title = userNotification.Title,
                    message = userNotification.Message,
                    type = userNotification.Type,
                    icon = userNotification.Icon,
                    link = userNotification.Link
                });

                
                int userUnreadCount = await context.Notifications
                    .CountAsync(n => n.DeletedAt == null && !n.IsRead && n.UserId == userId);
                await _hubContext.Clients.Group(userId.ToString()).SendAsync("UpdateUnreadCount", userUnreadCount);
                _logger.LogInformation("User {UserId}: Sent level up notification and updated unread count to {UnreadCount}.", userId, userUnreadCount);
            }
            else
            {
                _logger.LogInformation("User {UserId}: No level change detected.", userId);
            }
        }

        private async Task<Level> GetLevel0Async(DotNetTruyenDbContext context)
        {
            var level = await context.Levels
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LevelNumber == 0 && l.DeletedAt == null);

            if (level == null)
            {
                throw new Exception("Level 0 not found in the database. Please ensure it exists.");
            }

            return level;
        }



        public async Task AddLevelAsync(DotNetTruyenDbContext context, int levelNumber, string name, int expRequired)
        {
            var existingLevel = await context.Levels
                .AsNoTracking()
                .Where(l => l.LevelNumber == levelNumber && l.DeletedAt == null)
                .FirstOrDefaultAsync();

            if (existingLevel != null)
            {
                _logger.LogWarning("Level {LevelNumber} already exists.", levelNumber);
                throw new Exception($"Level {levelNumber} already exists.");
            }

            var newLevel = new Level
            {
                Id = Guid.NewGuid(),
                LevelNumber = levelNumber,
                Name = name,
                ExpRequired = expRequired,
                UpdatedAt = DateTime.UtcNow
            };

            context.Levels.Add(newLevel);
            await context.SaveChangesAsync();
            _logger.LogInformation("Added new level {LevelNumber} - {Name} with ExpRequired {ExpRequired}.", levelNumber, name, expRequired);
        }

		public async Task<string> GetUserLevelNameAsync(DotNetTruyenDbContext _context, Guid userId)
		{
			try
			{
				var user = await _context.Users
					.Include(u => u.Level)
					.FirstOrDefaultAsync(u => u.Id == userId);

				if (user == null)
				{
					_logger.LogWarning("User with ID {UserId} not found in GetUserLevelNameAsync.", userId);
					throw new Exception("User not found.");
				}

				if (user.Level == null)
				{
					_logger.LogInformation("User {UserId} has no level in GetUserLevelNameAsync, assigning Level 0.", userId);
					user.Level = await GetLevel0Async(_context);
					user.LevelId = user.Level.Id;
					await _context.SaveChangesAsync();
				}

				return user.Level.Name ?? "Unknown Level";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error occurred while getting level name for user {UserId}", userId);
				throw;
			}
		}
	}
}