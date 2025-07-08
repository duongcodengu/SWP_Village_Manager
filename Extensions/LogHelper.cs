using System;
using Village_Manager.Data;
using Village_Manager.Models;

namespace Village_Manager.Extensions
{
    public static class LogHelper
    {
        public static void SaveLog(AppDbContext context, int? userId, string action, DateTime? createdAt = null)
        {
            var log = new Log
            {
                UserId = userId,
                Action = action,
                CreatedAt = createdAt ?? DateTime.Now
            };
            context.Logs.Add(log);
            context.SaveChanges();
        }
    }
} 