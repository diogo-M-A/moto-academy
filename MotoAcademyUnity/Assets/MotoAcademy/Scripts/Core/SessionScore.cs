using System;
using System.Collections.Generic;

namespace MotoAcademy.Core
{
    [Serializable]
    public sealed class ScoreEvent
    {
        public string type;
        public string detail;
        public int penalty;
        public float time;
    }

    [Serializable]
    public sealed class SessionScore
    {
        public int score = 100;
        public float seconds;
        public float distance;
        public float topSpeed;
        public int completedLessons;
        public int checkpoints;
        public bool examComplete;
        public string savedAt;
        public List<ScoreEvent> events = new List<ScoreEvent>();
        public void Penalize(string type, string detail, int penalty)
        {
            score = Math.Max(0, score - Math.Max(0, penalty));
            events.Add(new ScoreEvent { type = type, detail = detail, penalty = penalty, time = seconds });
            if (events.Count > 200) events.RemoveAt(0);
        }
    }
}
