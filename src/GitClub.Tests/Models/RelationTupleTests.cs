// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using GitClub.Database.Models;
using GitClub.Models;

namespace GitClub.Tests.Models
{
    [TestClass]
    public class RelationTupleTests
    {
        [TestMethod]
        public void Create_ObjectAndSubjectAndRelation_CreatesCorrectRelationTuple()
        {
            // Arrange

            // Act
            var tuple = RelationTuple.Create<User, Organization>(4, 2, "reader");

            // Assert
            Assert.AreEqual("reader", tuple.Relation);
            Assert.AreEqual("User:4", tuple.Object);
            Assert.AreEqual("Organization:2", tuple.Subject);
        }

        [TestMethod]
        public void Create_ObjectAndSubjectAndRelationAndSubjectRelation_CreatesCorrectRelationTuple()
        {
            // Arrange

            // Act
            var tuple = RelationTuple.Create<User, Organization>(4, 2, "reader", "member");

            // Assert
            Assert.AreEqual("reader", tuple.Relation);
            Assert.AreEqual("User:4", tuple.Object);
            Assert.AreEqual("Organization:2#member", tuple.Subject);
        }
    }
}
