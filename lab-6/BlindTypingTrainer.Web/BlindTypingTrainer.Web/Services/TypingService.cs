﻿using BlindTypingTrainer.Web.Models;
using BlindTypingTrainer.Web.Repositories;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BlindTypingTrainer.Web.Services
{
    public class TypingService
    {
        private readonly IReadRepository<Lesson> _lessonRead;
        private readonly IReadRepository<TypingSession> _sessionRead;
        private readonly IWriteRepository<TypingSession> _sessionWrite;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AchievementService _achievementService;

        public TypingService(
            IReadRepository<Lesson> lessonRead,
            IReadRepository<TypingSession> sessionRead,
            IWriteRepository<TypingSession> sessionWrite,
            IHttpContextAccessor httpContextAccessor,
            AchievementService achievementService)
        {
            _lessonRead = lessonRead;
            _sessionRead = sessionRead;
            _sessionWrite = sessionWrite;
            _httpContextAccessor = httpContextAccessor;
            _achievementService = achievementService;
        }

        public async Task<TypingSession> StartSessionAsync(int lessonId)
        {
            // 1) Завантаження уроку
            var lesson = await _lessonRead.GetByIdAsync(lessonId)
                      ?? throw new ArgumentException("Урок не знайдено");

            // 2) Отримання поточного користувача
            var user = _httpContextAccessor.HttpContext?.User;
            var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? throw new InvalidOperationException("Користувач не аутентифікований");

            // 3) Створення нової сесії
            var session = new TypingSession
            {
                LessonId = lessonId,
                UserId = userId,
                StartTime = DateTime.Now
            };

            // 4) Збереження
            await _sessionWrite.AddAsync(session);
            return session;
        }

        public async Task EndSessionAsync(int sessionId, int correctChars, int errors)
        {
            // 1) Завантаження існуючого сеансу
            var session = await _sessionRead.GetByIdAsync(sessionId)
                          ?? throw new ArgumentException("Сесію не знайдено");

            // 2) Оновлення
            session.EndTime = DateTime.Now;
            session.CorrectChars = correctChars;
            session.Errors = errors;

            // 3) Збереження змін
            await _sessionWrite.UpdateAsync(session);

            await _achievementService.CheckAndAwardAsync(session);
        }
    }
}
