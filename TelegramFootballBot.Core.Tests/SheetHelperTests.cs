using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using TelegramFootballBot.Core.Exceptions;
using TelegramFootballBot.Core.Helpers;

namespace TelegramFootballBot.Core.Tests
{
    [TestClass]
    public class SheetHelperTests
    {
        readonly IList<IList<object>> _cells;
        readonly IList<IList<object>> _playersCells;    

        public SheetHelperTests()
        {
            _cells = CreateCells();
            _playersCells = CreatePlayersCells();

            foreach (var cell in _playersCells)
                _cells.Add(new List<object>(cell));

            _cells.Add(new List<object> { "Всего", $"=SUM(B3:B22)" });
        }

        [TestMethod]
        public void GetAllUsersRange_ReturnsCorrectRange()
        {
            // Arrange
            // Act
            var range = SheetHelper.GetAllPlayersRange();

            // Assert
            Assert.AreEqual("Участие в играх!A:B", range);
        }

        [TestMethod]
        public void GetApproveColumnRange_ReturnsCorrectRange()
        {
            // Arrange
            // Act
            var range = SheetHelper.GetApproveColumnRange(_cells, _playersCells.Count);
            
            // Assert
            Assert.AreEqual("B2:B22", range);
        }

        [TestMethod]
        public void GetApprovedPlayersString_ReturnsCorrectString()
        {
            // Arrange
            var headerMessage = $"{DateHelper.GetNearestGameDateMoscowTime(DateTime.UtcNow).ToRussianDayMonthString()}. Отметились: 7.";
            var dashedString = MarkupHelper.DashedString;
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

            // Act
            var approvedPlayersString = SheetHelper.BuildPlayersListMessage(_playersCells);

            // Assert
            Assert.AreEqual(expectedString, approvedPlayersString);
        }
        
        [TestMethod]
        public void GetOrderedPlayers_ReturnsCorrectCollection()
        {
            // Arrange
            // Act
            var expectedPlayers = _playersCells.OrderBy(p => p[0]).ToList();            
            var orderedPlayers = SheetHelper.GetOrderedPlayers(_cells);
            
            // Assert
            Assert.AreEqual(expectedPlayers.Count, orderedPlayers.Count);

            for (var i = 0; i < expectedPlayers.Count; i++)
            {
                if (expectedPlayers[i].Count < 2) expectedPlayers[i].Add(string.Empty);
                if (expectedPlayers[i][1] == null) expectedPlayers[i][1] = string.Empty;

                Assert.AreEqual(expectedPlayers[i].Count, orderedPlayers[i].Count);
                Assert.AreEqual(expectedPlayers[i][0], orderedPlayers[i][0]);
                Assert.AreEqual(expectedPlayers[i][1], orderedPlayers[i][1]);
            }
        }

        [TestMethod]
        public void GetStartRows_ReturnsCorrectRows()
        {
            // Arrange
            // Act
            var startRows = SheetHelper.GetHeaderRows(_cells);

            // Assert
            Assert.AreEqual(2, startRows.Count());
            Assert.AreEqual(0, startRows.First().Count);
            Assert.AreEqual(4, startRows.Skip(1).First().Count);
            Assert.IsNull(startRows.Skip(1).First()[0]);
            Assert.AreEqual("5 февраля", startRows.Skip(1).First()[1]);
            Assert.IsNull(startRows.Skip(1).First()[2]);
            Assert.AreEqual("Поле:", startRows.Skip(1).First()[3]);
        }

        [TestMethod]
        public void GetTotalsRow_ReturnsCorrectRow()
        {
            // Arrange
            // Act
            var totalsRow = SheetHelper.GetTotalsRow(_cells);

            // Assert
            Assert.AreEqual(2, totalsRow.Count);
            Assert.AreEqual("Всего", totalsRow[0]);
            Assert.AreEqual("=SUM(B3:B22)", totalsRow[1]);
        }

        [TestMethod]
        public void GetUserRange_ReturnsCorrectRange()
        {
            // Arrange
            // Act
            var range1 = SheetHelper.GetPlayerRange(3);
            var range2 = SheetHelper.GetPlayerRange(4);
            var range3 = SheetHelper.GetPlayerRange(5);

            // Assert
            Assert.AreEqual("Участие в играх!B3", range1);
            Assert.AreEqual("Участие в играх!B4", range2);
            Assert.AreEqual("Участие в играх!B5", range3);
        }

        [TestMethod]
        public void GetUserRowNumber_ReturnsCorrectRowNumber()
        {
            // Arrange
            // Act
            var row1 = SheetHelper.GetPlayerRowNumber(_cells, "User1");
            var row2 = SheetHelper.GetPlayerRowNumber(_cells, "User5");
            var row3 = SheetHelper.GetPlayerRowNumber(_cells, "User9");

            // Assert
            Assert.AreEqual(3, row1);
            Assert.AreEqual(7, row2);
            Assert.AreEqual(11, row3);
        }

        [TestMethod]
        public void NewValues_AddPlayer_ReturnsCorrectCollection()
        {
            // Arrange
            var newPlayers = new List<IList<object>>(_playersCells)
            {
                new List<object> { "User21" }
            };

            // Act
            var newValues = SheetHelper.ApplyPlayers(_cells, newPlayers);
            
            // Assert
            Assert.AreEqual(_cells.Count + 1, newValues.Count);
            Assert.AreEqual("Всего", newValues.LastOrDefault()?[0]);
            Assert.AreEqual("=SUM(B3:B23)", newValues.LastOrDefault()?[1]);
            Assert.AreEqual(1, newValues.Count(v => v?.Count > 0 && v[0]?.ToString() == "User21"));
        }

        [TestMethod]
        public void NewValues_Get_ReturnsCorrectCollection()
        {
            // Arrange
            // Act
            var newValues = SheetHelper.ApplyPlayers(_cells, _playersCells);

            // Assert
            Assert.AreEqual(_cells.Count, newValues.Count);
            Assert.AreEqual("Всего", newValues.LastOrDefault()?[0]);
            Assert.AreEqual("=SUM(B3:B22)", newValues.LastOrDefault()?[1]);
        }

        [TestMethod]
        public void NewValues_Get_ThrowsTotalsRowNotFound()
        {
            // Arrange
            void actual() { SheetHelper.ApplyPlayers(_cells.Take(_cells.Count - 1).ToList(), _playersCells); }

            // Act
            // Assert
            Assert.ThrowsException<TotalsRowNotFoundExeption>(actual);
        }

        private static List<IList<object>> CreateCells()
        {
            return new List<IList<object>>
            {
                new List<object>(),
                new List<object> { null, "5 февраля", null, "Поле:" }
            };
        }

        private static List<IList<object>> CreatePlayersCells()
        {
            return new List<IList<object>>
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
        }
    }
}
