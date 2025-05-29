using System;
using System.Collections.Generic;
using System.Linq;
using data;
using DevExpress.Xpo;
using Microsoft.Extensions.Logging;

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

            var totalPrior = _diagnoses.Sum(d => d.PriorP);
            state.CurrentPosteriors = _diagnoses.ToDictionary(
                d => d.OID,
                d => totalPrior == 0 ? 1.0 / _diagnoses.Count : d.PriorP / totalPrior
            );
        }

        /// <summary>Выбирает самый информативный симптом на основе текущих вероятностей диагнозов.</summary>
        public DbSymptom? PickNextQuestion(SessionState state)
        {
            var remainingSymptoms = _symptoms
                .Where(s => !state.AskedSymptomIds.Contains(s.OID))
                .ToList();

            if (!remainingSymptoms.Any())
                return null;

            var symptomScores = new List<(DbSymptom Symptom, double InformationGain)>();

            foreach (var symptom in remainingSymptoms)
            {
                try
                {
                    var gain = CalculateInformationGain(symptom, state.CurrentPosteriors);
                    symptomScores.Add((symptom, gain));
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, $"Error calculating information gain for symptom {symptom.OID}");
                    continue;
                }
            }

            return symptomScores
                .OrderByDescending(x => x.InformationGain)
                .ThenBy(x => x.Symptom.OID)
                .FirstOrDefault()
                .Symptom;
        }

        /// <summary>Вычисляет информационную ценность симптома (ожидаемое уменьшение энтропии).</summary>
        private double CalculateInformationGain(DbSymptom symptom, Dictionary<Guid, double> currentPosteriors)
        {
            double entropyBefore = CalculateEntropy(currentPosteriors.Values);

            double pYes = 0;
            foreach (var diag in _diagnoses)
            {
                if (!currentPosteriors.TryGetValue(diag.OID, out var posterior))
                    continue;

                var cond = diag.DiagnoseSymptoms?.FirstOrDefault(c => c.Symptom?.OID == symptom.OID);
                if (cond == null)
                    continue;

                pYes += posterior * cond.SymptomGivenDiagnoseP;
            }

            double pNo = 1 - pYes;

            var posteriorsIfYes = new Dictionary<Guid, double>();
            var posteriorsIfNo = new Dictionary<Guid, double>();

            foreach (var diag in _diagnoses)
            {
                if (!currentPosteriors.TryGetValue(diag.OID, out var currentP))
                    continue;

                var cond = diag.DiagnoseSymptoms?.FirstOrDefault(c => c.Symptom?.OID == symptom.OID);

                if (cond == null)
                {
                    posteriorsIfYes[diag.OID] = currentP;
                    posteriorsIfNo[diag.OID] = currentP;
                    continue;
                }

                double pX_W = cond.SymptomGivenDiagnoseP;
                double pX_notW = cond.Symptom?.SymptomGivenNotDiagnoseP ?? 0.5;

                double numeratorYes = currentP * pX_W;
                double denominatorYes = numeratorYes + (1 - currentP) * pX_notW;
                posteriorsIfYes[diag.OID] = denominatorYes == 0 ? 0 : numeratorYes / denominatorYes;

                double numeratorNo = currentP * (1 - pX_W);
                double denominatorNo = numeratorNo + (1 - currentP) * (1 - pX_notW);
                posteriorsIfNo[diag.OID] = denominatorNo == 0 ? 0 : numeratorNo / denominatorNo;
            }

            double entropyIfYes = CalculateEntropy(posteriorsIfYes.Values);
            double entropyIfNo = CalculateEntropy(posteriorsIfNo.Values);

            return entropyBefore - (pYes * entropyIfYes + pNo * entropyIfNo);
        }

        /// <summary>Вычисляет энтропию распределения вероятностей.</summary>
        private double CalculateEntropy(IEnumerable<double> probabilities)
        {
            double entropy = 0;
            foreach (var p in probabilities)
            {
                if (p > 0)
                {
                    entropy -= p * Math.Log(p, 2);
                }
            }
            return entropy;
        }

        /// <summary>Обновляет постериоры по ответу пользователя.</summary>
        public void Update(SessionState st, Guid symptomId, bool answerYes)
        {
            st.AskedSymptomIds.Add(symptomId);
            st.Answers[symptomId] = answerYes;

            var newPosteriors = new Dictionary<Guid, double>();

            foreach (var d in _diagnoses)
            {
                if (!st.CurrentPosteriors.TryGetValue(d.OID, out var currentPosterior))
                    continue;

                var cond = d.DiagnoseSymptoms.FirstOrDefault(c => c.Symptom?.OID == symptomId);
                if (cond is null)
                {
                    newPosteriors[d.OID] = currentPosterior;
                    continue;
                }

                double pX_W = answerYes ? cond.SymptomGivenDiagnoseP : 1 - cond.SymptomGivenDiagnoseP;
                double pX_notW = answerYes ? cond.Symptom.SymptomGivenNotDiagnoseP : 1 - cond.Symptom.SymptomGivenNotDiagnoseP;

                double numerator = currentPosterior * pX_W;
                double denominator = numerator + (1 - currentPosterior) * pX_notW;

                newPosteriors[d.OID] = denominator == 0 ? 0 : numerator / denominator;
            }

            double total = newPosteriors.Sum(p => p.Value);
            if (total > 0)
            {
                foreach (var key in newPosteriors.Keys.ToList())
                {
                    newPosteriors[key] /= total;
                }
            }

            st.CurrentPosteriors = newPosteriors
                .Where(kvp => kvp.Value >= _thresholdReject)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            st.Finished = !st.CurrentPosteriors.Any() ||
                         st.CurrentPosteriors.Any(p => p.Value >= _thresholdAccept) ||
                         st.AskedSymptomIds.Count == _symptoms.Count;

            Console.WriteLine($"Q{symptomId}:{(answerYes ? "Yes" : "No")}. Priors: " +
                $"{string.Join(", ", st.CurrentPosteriors.Select(p => $"{DiagnosisName(p.Key)}={p.Value:F3}"))}");
        }

        public int TotalSymptoms => _symptoms.Count;

        public string DiagnosisName(Guid id) =>
            _diagnoses.FirstOrDefault(d => d.OID == id)?.Country ?? $"Diagnosis {id}";
    }
}