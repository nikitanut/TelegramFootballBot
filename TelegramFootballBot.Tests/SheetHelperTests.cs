using System;
using System.Collections.Generic;
using System.Linq;
using TelegramFootballBot.Core.Helpers;
using TelegramFootballBot.Core.Models;
using Xunit;

namespace TelegramFootballBot.Tests
{
    public class SheetHelperTests
    {
        readonly IList<IList<object>> _values;
        readonly IList<IList<object>> _players = new List<IList<object>>
        {            
            new List<object> { "User1", string.Empty, "https://User.com", "Every friday" },
            new List<object> { "User2", null },
            new List<object> { "User3", 1 },
            new List<object> { "User4", "0" },
            new List<object> { "User5" },
            new List<object> { "User6", },
            new List<object> { "User7", 0.5 },
            new List<object> { "User8", 2 },
            new List<object> { "User9", "2" },
            new List<object> { "User10", "" },
            new List<object> { "User11" },
            new List<object> { "User12" },
            new List<object> { "User13" },
            new List<object> { "User14", "0.5" },
            new List<object> { "User15", "0,5" },
            new List<object> { "User16", 1 },
            new List<object> { "User17", null },
            new List<object> { "User18", null },
            new List<object> { "User19", "1" },
            new List<object> { "User20", null }            
        };        

        public SheetHelperTests()
        {
            _values = new List<IList<object>>
            {
                new List<object>(),
                new List<object> { null, "5 февраля", null, "Поле:" }
            };

            foreach (var player in _players)
                _values.Add(new List<object>(player));

            _values.Add(new List<object> { "Всего", $"=SUM(B3:B22)" });
        }

        [Fact]
        public void GetAllUsersRange_ReturnsCorrectRange()
        {
            var range = SheetHelper.GetAllUsersRange();
            Assert.Equal("Участие в играх!A:B", range);
        }

        [Fact]
        public void GetApproveColumnRange_ReturnsCorrectRange()
        {
            var range = SheetHelper.GetApproveColumnRange(_values, _players.Count);
            Assert.Equal("B2:B22", range);
        }

        [Fact]
        public void GetApprovedPlayersString_ReturnsCorrectString()
        {
            var approvedPlayersString = SheetHelper.GetApprovedPlayersString(_players);

            var headerMessage = $"{DateHelper.GetNearestGameDateMoscowTime(DateTime.UtcNow).ToRussianDayMonthString()}. Отметились: 7.";
            var dashedString = MarkupHelper.GetDashedString();
            var expectedString = $"{headerMessage}{Environment.NewLine}" +
                $"{dashedString}{Environment.NewLine}" +
                $"User3{Environment.NewLine}" +
                $"User8 x2{Environment.NewLine}" +
                $"User9 x2{Environment.NewLine}" +
                $"User16{Environment.NewLine}" +
                $"User19{Environment.NewLine}" +
                $"{dashedString}{Environment.NewLine}" +
                $"Под вопросом: 3.{Environment.NewLine}" +
                $"User7{Environment.NewLine}" +
                $"User14{Environment.NewLine}" +
                $"User15{Environment.NewLine}";

            Assert.Equal(expectedString, approvedPlayersString);
        }
        
        [Fact]
        public void GetOrderedPlayers_ReturnsCorrectCollection()
        {
            var orderedPlayers = SheetHelper.GetOrderedPlayers(_values);
            var expectedPlayers = _players.OrderBy(p => p[0]).ToList();
            
            Assert.Equal(expectedPlayers.Count, orderedPlayers.Count());

            for (var i = 0; i < expectedPlayers.Count; i++)
            {
                if (expectedPlayers[i].Count < 2) expectedPlayers[i].Add(string.Empty);
                if (expectedPlayers[i][1] == null) expectedPlayers[i][1] = string.Empty;

                Assert.Equal(expectedPlayers[i].Count, orderedPlayers[i].Count);
                Assert.Equal(expectedPlayers[i][0], orderedPlayers[i][0]);
                Assert.Equal(expectedPlayers[i][1], orderedPlayers[i][1]);
            }
        }

        [Fact]
        public void GetStartRows_ReturnsCorrectRows()
        {
            var startRows = SheetHelper.GetStartRows(_values);

            Assert.Equal(2, startRows.Count());
            Assert.Equal(0, startRows.First().Count);
            Assert.Equal(4, startRows.Skip(1).First().Count);
            Assert.Null(startRows.Skip(1).First()[0]);
            Assert.Equal("5 февраля", startRows.Skip(1).First()[1]);
            Assert.Null(startRows.Skip(1).First()[2]);
            Assert.Equal("Поле:", startRows.Skip(1).First()[3]);
        }

        [Fact]
        public void GetTotalsRow_ReturnsCorrectRow()
        {
            var totalsRow = SheetHelper.GetTotalsRow(_values);

            Assert.Equal(2, totalsRow.Count());
            Assert.Equal("Всего", totalsRow[0]);
            Assert.Equal("=SUM(B3:B22)", totalsRow[1]);
        }

        [Fact]
        public void GetUserRange_ReturnsCorrectRange()
        {
            var range1 = SheetHelper.GetUserRange(3);
            var range2 = SheetHelper.GetUserRange(4);
            var range3 = SheetHelper.GetUserRange(5);

            Assert.Equal("Участие в играх!B3", range1);
            Assert.Equal("Участие в играх!B4", range2);
            Assert.Equal("Участие в играх!B5", range3);
        }

        [Fact]
        public void GetUserRowNumber_ReturnsCorrectRowNumber()
        {
            var row1 = SheetHelper.GetUserRowNumber(_values, "User1");
            var row2 = SheetHelper.GetUserRowNumber(_values, "User5");
            var row3 = SheetHelper.GetUserRowNumber(_values, "User9");

            Assert.Equal(3, row1);
            Assert.Equal(7, row2);
            Assert.Equal(11, row3);
        }

        [Fact]
        public void NewValues_AddPlayer_ReturnsCorrectCollection()
        {
            var newPlayers = new List<IList<object>>(_players)
            {
                new List<object> { "User21" }
            };

            var newValues = SheetHelper.GetNewValues(_values, newPlayers);
            
            Assert.Equal(_values.Count + 1, newValues.Count);
            Assert.Equal("Всего", newValues.LastOrDefault()?[0]);
            Assert.Equal("=SUM(B3:B23)", newValues.LastOrDefault()?[1]);
            Assert.True(newValues.Count(v => v?.Count > 0 && v[0]?.ToString() == "User21") == 1);
        }

        [Fact]
        public void NewValues_Get_ReturnsCorrectCollection()
        {
            var newValues = SheetHelper.GetNewValues(_values, _players);

            Assert.Equal(_values.Count, newValues.Count);
            Assert.Equal("Всего", newValues.LastOrDefault()?[0]);
            Assert.Equal("=SUM(B3:B22)", newValues.LastOrDefault()?[1]);
        }

        [Fact]
        public void NewValues_Get_ThrowsTotalsRowNotFound()
        {
            Action actual = () => { SheetHelper.GetNewValues(_values.Take(_values.Count - 1).ToList(), _players); };

            Assert.Throws<TotalsRowNotFoundExeption>(actual);
        }
    }
}
