using System;
using System.Collections.Generic;
using System.Linq;

namespace TelegramFootballBot.Services
{
    public static class NamesGenerator
    {
        
        private static Random _random = new Random();

        public static string[] Generate(int numberOfNames)
        {
            var names = new string[numberOfNames];
            for (int i = 0; i < names.Length; i++)
                names[i] = Generate(names);
            return names;
        }

        private static string Generate(IEnumerable<string> namesToIgnore)
        {
            var remainingNames = _names.Where(n => !namesToIgnore.Contains(n)).ToArray();
            return remainingNames[_random.Next(remainingNames.Count())];
        }
        
        private static readonly string[] _names = new[] {
            "☠️Смертоносные гадюки",
            "🎩Джентльмены",
            "🧐Судари",
            "🕶Уверенные",
            "🦏Носороги",
            "👨‍💻Сборная айтишников",
            "🚀Космические",
            "🧠Тактики",
            "🎓Академики футбола",
            "🔥Перцы",
            "🏃🏻‍♂️Любители побегать",
            "👑Короли камбэков",
            "⚽️Полупрофессионалы",
            "👨‍🎨Свободные художники",
            "❗️Опасные ребята",
            "🥊Бойцы",
            "🦅Орлы",
            "👽Инопланетные парни",
            "🥋Спортивная банда",
            "🏆Чемпионы по жизни",
            "⛔️Непроходимые",
            "🔋Заряженные на борьбу",
            "🎯Бомбардиры",
            "🏎Красная машина"
        };
    }
}
