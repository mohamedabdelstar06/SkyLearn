using SkyLearnApi.DTOs.Quizzes;

namespace SkyLearnApi.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class QuestionsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IGeminiService _geminiService;
        private int? UserId => int.TryParse(User.FindFirst("UserId")?.Value, out var id) ? id : null;

        public QuestionsController(AppDbContext context, IGeminiService geminiService)
        {
            _context = context;
            _geminiService = geminiService;
        }

        [HttpPost("quizzes/{quizId}/questions")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> Create(int quizId, [FromBody] CreateQuestionDto dto)
        {
            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz == null) return NotFound(new { message = "Quiz not found." });

            var question = new Question
            {
                QuizId = quizId,
                QuestionText = dto.QuestionText,
                QuestionType = dto.QuestionType,
                Marks = dto.Marks,
                DifficultyLevel = dto.DifficultyLevel,
                Explanation = dto.Explanation,
                SourceReference = dto.SourceReference,
                SortOrder = dto.SortOrder
            };

            if (dto.Options != null)
            {
                foreach (var o in dto.Options)
                {
                    question.Options.Add(new QuestionOption
                    {
                        OptionText = o.OptionText,
                        IsCorrect = o.IsCorrect,
                        SortOrder = o.SortOrder
                    });
                }
            }

            quiz.TotalMarks += dto.Marks;
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            return Ok(new { id = question.Id, message = "Question added successfully." });
        }

        [HttpPut("questions/{id}")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateQuestionDto dto)
        {
            var question = await _context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);
            if (question == null) return NotFound(new { message = "Question not found." });

            var quiz = await _context.Quizzes.FindAsync(question.QuizId);

            if (dto.QuestionText != null) question.QuestionText = dto.QuestionText;
            if (dto.QuestionType != null) question.QuestionType = dto.QuestionType;
            if (dto.DifficultyLevel != null) question.DifficultyLevel = dto.DifficultyLevel;
            if (dto.Explanation != null) question.Explanation = dto.Explanation;
            if (dto.SourceReference != null) question.SourceReference = dto.SourceReference;
            if (dto.SortOrder.HasValue) question.SortOrder = dto.SortOrder.Value;

            if (dto.Marks.HasValue)
            {
                if (quiz != null)
                    quiz.TotalMarks = quiz.TotalMarks - question.Marks + dto.Marks.Value;
                question.Marks = dto.Marks.Value;
            }

            if (dto.Options != null)
            {
                _context.QuestionOptions.RemoveRange(question.Options);
                foreach (var o in dto.Options)
                {
                    question.Options.Add(new QuestionOption
                    {
                        OptionText = o.OptionText,
                        IsCorrect = o.IsCorrect,
                        SortOrder = o.SortOrder
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Question updated successfully." });
        }

        [HttpDelete("questions/{id}")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> Delete(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null) return NotFound(new { message = "Question not found." });

            var quiz = await _context.Quizzes.FindAsync(question.QuizId);
            if (quiz != null) quiz.TotalMarks -= question.Marks;

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Question deleted successfully." });
        }

        [HttpPut("questions/{id}/reorder")]
        [Authorize(Roles = Roles.AdminOrInstructor)]
        public async Task<IActionResult> Reorder(int id, [FromBody] int newSortOrder)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question == null) return NotFound(new { message = "Question not found." });

            question.SortOrder = newSortOrder;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Question reordered." });
        }

        [HttpPost("questions/{id}/translate")]
        [Authorize]
        public async Task<IActionResult> Translate(int id)
        {
            var question = await _context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);
            if (question == null) return NotFound(new { message = "Question not found." });

            if (!string.IsNullOrEmpty(question.QuestionTextAr))
                return Ok(new { questionTextAr = question.QuestionTextAr, message = "Already translated." });

            try
            {
                question.QuestionTextAr = await _geminiService.TranslateToArabicAsync(question.QuestionText);

                if (!string.IsNullOrEmpty(question.Explanation))
                    question.ExplanationAr = await _geminiService.TranslateToArabicAsync(question.Explanation);

                foreach (var option in question.Options)
                {
                    if (string.IsNullOrEmpty(option.OptionTextAr))
                        option.OptionTextAr = await _geminiService.TranslateToArabicAsync(option.OptionText);
                }

                await _context.SaveChangesAsync();
                return Ok(new { questionTextAr = question.QuestionTextAr, message = "Question translated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Translation failed.", error = ex.Message });
            }
        }
    }
}
