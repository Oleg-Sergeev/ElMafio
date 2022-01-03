using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trivia4NET;
using Trivia4NET.Payloads;

namespace Services.Modules;

public interface IQuizService
{
    CategoryStatisticsResponse GetStatisticsAsync(int categoryId, CancellationToken token = default);
    CategoryResponse GetCategoriesAsync(CancellationToken token = default);
    QuestionsResponse GetQuestionsAsync(int categoryId, CancellationToken token = default);
}