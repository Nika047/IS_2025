using data;
using DevExpress.Xpo;

namespace ui.Helper
{
    public class SessionState
    {
        public HashSet<Guid> AskedSymptomIds { get; set; } = new();
        public Dictionary<Guid, bool> Answers { get; set; } = new();
        public Dictionary<Guid, double> CurrentPosteriors { get; set; } = new();
        public bool Finished { get; set; }
    }

    public sealed class ExpertEngine
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly List<DbDiagnose> _diagnoses;
        private readonly List<DbSymptom> _symptoms;
        private readonly ILogger<ExpertEngine> _log;

        private readonly double _thresholdAccept = 0.99;
        private readonly double _thresholdReject = 0.01;

        public ExpertEngine(UnitOfWork unitOfWork, ILogger<ExpertEngine> log)
        {
            _unitOfWork = unitOfWork;
            _log = log;
            _diagnoses = _unitOfWork.Query<DbDiagnose>().ToList();
            _symptoms = _unitOfWork.Query<DbSymptom>().ToList();
        }

        public void Initialize(SessionState state)
        {
            state.AskedSymptomIds.Clear();
            state.Answers.Clear();
            state.Finished = false;
            state.CurrentPosteriors = _diagnoses.ToDictionary(d => d.OID, d => (double)d.PriorP);

            var total = _diagnoses.Sum(d => d.PriorP);
            state.CurrentPosteriors = _diagnoses.ToDictionary(
                d => d.OID,
                d => total == 0 ? 1.0 / _diagnoses.Count : d.PriorP / total
            );
        }

        /// <summary>Выбирает самый информативный ещё не заданный симптом.</summary>
        public DbSymptom? PickNextQuestion(SessionState state)
        {
            var remainingSymptoms = _symptoms.Where(s => !state.AskedSymptomIds.Contains(s.OID));

            var symptomDiagnosesCount = _diagnoses
                .SelectMany(d => d.DiagnoseSymptoms)
                .GroupBy(s => s.Symptom.OID)
                .ToDictionary(g => g.Key, g => g.Count());

            var best = remainingSymptoms
                .Select(sym => new
                {
                    Symptom = sym,
                    Count = symptomDiagnosesCount.TryGetValue(sym.OID, out var count) ? count : 0
                })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Symptom.OID)
                .FirstOrDefault();

            return best.Symptom;
        }

        /// <summary>Обновляет постериоры по ответу пользователя.</summary>
        public void Update(SessionState st, Guid symptomId, bool answerYes)
        {
            st.AskedSymptomIds.Add(symptomId);
            st.Answers[symptomId] = answerYes;

            var priors = new Dictionary<Guid, double>(st.CurrentPosteriors);

            foreach (var d in _diagnoses.Where(diag => priors.ContainsKey(diag.OID)))
            {
                var cond = d.DiagnoseSymptoms.FirstOrDefault(c => c.Symptom.OID == symptomId);
                if (cond is null)
                    continue;

                double pX_W = answerYes ? cond.SymptomGivenDiagnoseP : 1 - cond.SymptomGivenDiagnoseP;
                double pX_not_W = answerYes ? cond.Symptom.SymptomGivenNotDiagnoseP : 1 - cond.Symptom.SymptomGivenNotDiagnoseP;

                double numerator = st.CurrentPosteriors[d.OID] * pX_W;
                double denominator = numerator + (1 - st.CurrentPosteriors[d.OID]) * pX_not_W;

                st.CurrentPosteriors[d.OID] = denominator == 0 ? 0 : numerator / denominator;
            }

            st.CurrentPosteriors = st.CurrentPosteriors
                .Where(kvp => kvp.Value >= _thresholdReject)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            bool noDiagnosesLeft = !st.CurrentPosteriors.Any();

            if (noDiagnosesLeft ||
                st.CurrentPosteriors.Any(p => p.Value >= _thresholdAccept) ||
                st.AskedSymptomIds.Count == _symptoms.Count)
            {
                st.Finished = true;
            }

            if (st.CurrentPosteriors.Any(p => p.Value >= _thresholdAccept) ||
                st.AskedSymptomIds.Count == _symptoms.Count)
            {
                st.Finished = true;
            }

            Console.WriteLine($"Q{symptomId}:{(answerYes ? "Yes" : "No")}. Priors: " +
                $"{string.Join(", ", st.CurrentPosteriors.Select(p => $"{DiagnosisName(p.Key)}={p.Value:F3}"))}");
        }

        public int TotalSymptoms => _symptoms.Count;

        public string DiagnosisName(Guid id) =>
            _diagnoses.FirstOrDefault(d => d.OID == id)?.Country ?? $"Diagnosis {id}";
    }
}