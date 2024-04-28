// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using GitClub.Infrastructure.OpenFga;

namespace GitClub.Tests.Infrastructure.OpenFga
{
    [TestClass]
    public class ZanzibarFormattersTests
    {
        [TestMethod]
        public void ToZanzibarNotation_ByEntity_FormatsCorrectly()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "unit-test.local",
                PreferredName = "Unit Test",
                LastEditedBy = 1
            };

            // Act
            var zanzibarNotation = ZanzibarFormatters.ToZanzibarNotation(user);

            // Assert
            Assert.AreEqual("User:1", zanzibarNotation);
        }

        [TestMethod]
        public void ToZanzibarNotation_ByEntityAndRelation_FormatsCorrectly()
        {
            // Arrange
            var user = new User
            {
                Id = 1,
                Email = "unit-test.local",
                PreferredName = "Unit Test",
                LastEditedBy = 1
            };

            // Act
            var zanzibarNotation = ZanzibarFormatters.ToZanzibarNotation(user, "reader");

            // Assert
            Assert.AreEqual("User:1#reader", zanzibarNotation);
        }

        [TestMethod]
        public void ToZanzibarNotation_ByTypeAndId_FormatsCorrectly()
        {
            // Arrange
            (string type, int id) = ("User", 2);

            // Act
            var zanzibarNotation = ZanzibarFormatters.ToZanzibarNotation(type, id);

            // Assert
            Assert.AreEqual("User:2", zanzibarNotation);
        }

        [TestMethod]
        public void ToZanzibarNotation_ByTypeAndIdAndRelation_FormatsCorrectly()
        {
            // Arrange
            (string type, int id, string relation) = ("User", 2, "reader");

            // Act
            var zanzibarNotation = ZanzibarFormatters.ToZanzibarNotation(type, id, relation);

            // Assert
            Assert.AreEqual("User:2#reader", zanzibarNotation);
        }
        
        [TestMethod]
        public void FromZanzibarNotation_WithTypeAndIdAndRelation_ParsesCorrectly()
        {
            // Arrange
            var zanzibarNotation = "User:2#reader";

            // Act
            (var type, var id, var relation) = ZanzibarFormatters.FromZanzibarNotation(zanzibarNotation);

            // Assert
            Assert.AreEqual("User", type);
            Assert.AreEqual(2, id);
            Assert.AreEqual("reader", relation);
        }

        [TestMethod]
        public void FromZanzibarNotation_WithTypeAndId_ParsesCorrectly()
        {
            // Arrange
            var zanzibarNotation = "User:2";

            // Act
            (var type, var id, var relation) = ZanzibarFormatters.FromZanzibarNotation(zanzibarNotation);

            // Assert
            Assert.AreEqual("User", type);
            Assert.AreEqual(2, id);
            Assert.AreEqual(null, relation);
        }
    }
}
